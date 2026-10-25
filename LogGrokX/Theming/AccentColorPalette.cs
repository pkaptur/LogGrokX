using LogGrokX.Platform;
using System;
using System.Collections.Generic;

namespace LogGrokX.Theming;

public static class AccentColorPalette
{
    public static IReadOnlyList<AccentColorDefinition> All { get; } =
    [
        new(AccentColorKind.Blue, "AccentBlue", "#0078D4"),
        new(AccentColorKind.Purple, "AccentPurple", "#8661C5"),
        new(AccentColorKind.Pink, "AccentPink", "#E3008C"),
        new(AccentColorKind.Red, "AccentRed", "#D13438"),
        new(AccentColorKind.Orange, "AccentOrange", "#F7630C"),
        new(AccentColorKind.Green, "AccentGreen", "#107C10"),
        new(AccentColorKind.Teal, "AccentTeal", "#008272")
    ];

    public static AccentColorKind Normalize(AccentColorKind value) =>
        Enum.IsDefined(value) ? value : AccentColorKind.Blue;

    public static string GetColor(AccentColorKind value)
    {
        value = Normalize(value);
        foreach (var definition in All)
        {
            if (definition.Value == value)
                return definition.Color;
        }

        return All[0].Color;
    }
}

public sealed record AccentColorDefinition(
    AccentColorKind Value,
    string TitleResource,
    string Color);
