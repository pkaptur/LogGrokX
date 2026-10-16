using System;
using LogGrokX.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogGrokX.Data.Tests;

[TestClass]
public class LogFormatValidationTests
{
    private const string Pattern = @"^(?<Level>[A-Z]+) (?<Message>.*)$";

    [TestMethod]
    [DataRow("Levl")]
    [DataRow("level")]
    [DataRow("0")]
    [DataRow("3")]
    [DataRow("")]
    public void UnknownIndexedFieldIsInvalid(string field)
    {
        var format = new LogFormat { Regex = Pattern, IndexedFields = [field] };

        Assert.IsFalse(format.IsCorrect());
    }

    [TestMethod]
    [DataRow("Levl")]
    [DataRow("level")]
    [DataRow("0")]
    public void IndexedParserRejectsInvalidFieldsBeforeParsing(string field)
    {
        var format = new LogFormat { Regex = Pattern, IndexedFields = [field] };

        var error = Assert.ThrowsExactly<ArgumentException>(() =>
            new RegexBasedLineParser(new LogMetaInformation(format), true));

        Assert.AreEqual(nameof(LogFormat.IndexedFields), error.ParamName);
        StringAssert.Contains(error.Message, field);
    }

    [TestMethod]
    public void ExistingOptionalFieldCanHaveNoValue()
    {
        var format = new LogFormat
        {
            Regex = @"^(?:(?<Level>[A-Z]+) )?(?<Message>.*)$",
            IndexedFields = ["Level"]
        };

        Assert.IsTrue(format.IsCorrect());
        new RegexBasedLineParser(new LogMetaInformation(format), true).Parse("hello");
    }

    [TestMethod]
    public void EmptyIndexListIsValid()
    {
        var format = new LogFormat { Regex = Pattern };

        Assert.IsTrue(format.IsCorrect());
        new RegexBasedLineParser(new LogMetaInformation(format), true).Parse("INFO hello");
    }

    [TestMethod]
    public void RenamingGroupInvalidatesItsIndexReference()
    {
        var format = new LogFormat { Regex = Pattern, IndexedFields = ["Level"] };
        Assert.IsTrue(format.IsCorrect());

        format.Regex = @"^(?<Severity>[A-Z]+) (?<Message>.*)$";

        Assert.IsFalse(format.IsCorrect());
    }
}
