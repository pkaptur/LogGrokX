#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using LogGrokX.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogGrokX.Tests;

[TestClass]
public sealed class ViewModelArchitectureTests
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    private static readonly Dictionary<short, OpCode> Codes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(field => (OpCode)field.GetValue(null)!).ToDictionary(code => code.Value);

    [TestMethod]
    public void ViewModelMethodBodiesDoNotCallWpfOrDesktopApis()
    {
        var types = typeof(ViewModelBase).Assembly.GetTypes().Where(type => typeof(ViewModelBase).IsAssignableFrom(type));
        foreach (var type in types.SelectMany(WithNestedTypes))
        foreach (var method in type.GetMethods(Members).Cast<MethodBase>().Concat(type.GetConstructors(Members)))
        foreach (var reference in GetReferences(method))
        {
            var owner = reference is Type referencedType ? referencedType : reference.DeclaringType;
            Assert.IsFalse(owner != null && IsPlatformImplementation(owner),
                $"{type.FullName}.{method.Name} references {reference.DeclaringType?.FullName ?? owner?.FullName}.{reference.Name}.");
        }
    }

    [TestMethod]
    public void BodyInspectionFindsCallsThatAreAbsentFromSignatures()
    {
        var method = typeof(ForbiddenViewModel).GetMethod(nameof(ForbiddenViewModel.ReadClipboard))!;
        Assert.IsTrue(GetReferences(method).Any(reference => reference.DeclaringType == typeof(System.Windows.Clipboard)));
    }

    [TestMethod]
    public void PlatformDependenciesCannotBeOmittedFromViewModelConstructors()
    {
        var contracts = new[] { typeof(IUiDispatcher), typeof(IDesktopService), typeof(IColorValidator), typeof(IAppDialogs), typeof(IThemeService), typeof(IUpdateService) };
        foreach (var type in typeof(ViewModelBase).Assembly.GetTypes().Where(type => typeof(ViewModelBase).IsAssignableFrom(type)))
        foreach (var parameter in type.GetConstructors(Members).SelectMany(constructor => constructor.GetParameters()))
            if (contracts.Contains(parameter.ParameterType))
                Assert.IsFalse(parameter.HasDefaultValue, $"{type.Name}.{parameter.Name} must be supplied explicitly.");
    }

    private static bool IsPlatformImplementation(Type type)
    {
        var assembly = type.Assembly.GetName().Name ?? string.Empty;
        return assembly is "PresentationFramework" or "PresentationCore" or "WindowsBase" or "TextCopy" ||
               assembly.StartsWith("AvalonDock", StringComparison.Ordinal) || assembly.StartsWith("Wpf.Ui", StringComparison.Ordinal) ||
               type == typeof(System.Diagnostics.Process) || type == typeof(UpdateCheckService) || type == typeof(Theming.UiThemeService);
    }

    private static IEnumerable<Type> WithNestedTypes(Type type)
    {
        yield return type;
        foreach (var nested in type.GetNestedTypes(Members).SelectMany(WithNestedTypes)) yield return nested;
    }

    private static IEnumerable<MemberInfo> GetReferences(MethodBase method)
    {
        var bytes = method.GetMethodBody()?.GetILAsByteArray();
        if (bytes == null) yield break;
        var position = 0;
        while (position < bytes.Length)
        {
            short value = bytes[position++];
            if (value == 0xfe) value = unchecked((short)(0xfe00 | bytes[position++]));
            var code = Codes[value];
            switch (code.OperandType)
            {
                case OperandType.InlineMethod:
                case OperandType.InlineField:
                case OperandType.InlineType:
                case OperandType.InlineTok:
                    var token = BitConverter.ToInt32(bytes, position);
                    var member = method.Module.ResolveMember(token, method.DeclaringType?.GetGenericArguments(),
                        method.IsGenericMethod ? method.GetGenericArguments() : null);
                    if (member != null) yield return member;
                    position += 4;
                    break;
                case OperandType.InlineSwitch:
                    position += 4 + BitConverter.ToInt32(bytes, position) * 4;
                    break;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    position += 8;
                    break;
                case OperandType.InlineBrTarget:
                case OperandType.InlineI:
                case OperandType.ShortInlineR:
                case OperandType.InlineSig:
                case OperandType.InlineString:
                    position += 4;
                    break;
                case OperandType.InlineVar:
                    position += 2;
                    break;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    position++;
                    break;
            }
        }
    }

    private sealed class ForbiddenViewModel : ViewModelBase
    {
        public bool ReadClipboard() => System.Windows.Clipboard.ContainsText();
    }
}
