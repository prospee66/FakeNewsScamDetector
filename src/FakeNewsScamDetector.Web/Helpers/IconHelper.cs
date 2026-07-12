using FakeNewsScamDetector.Core.Enums;

namespace FakeNewsScamDetector.Web.Helpers;

public static class IconHelper
{
    private const string Attrs = "class=\"icon\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"";

    public static string Svg(string name) => name switch
    {
        "shield" =>
            $"<svg {Attrs}><path d=\"M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z\"/><path d=\"M9 12l2 2 4-4\"/></svg>",

        "cpu" =>
            $"<svg {Attrs}><rect x=\"5\" y=\"5\" width=\"14\" height=\"14\" rx=\"2\"/><rect x=\"9\" y=\"9\" width=\"6\" height=\"6\"/><path d=\"M9 1v3M15 1v3M9 20v3M15 20v3M1 9h3M1 15h3M20 9h3M20 15h3\"/></svg>",

        "search" =>
            $"<svg {Attrs}><circle cx=\"11\" cy=\"11\" r=\"8\"/><path d=\"M21 21l-4.35-4.35\"/></svg>",

        "link" =>
            $"<svg {Attrs}><path d=\"M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71\"/><path d=\"M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71\"/></svg>",

        "check-circle" =>
            $"<svg {Attrs}><circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"M9 12l2 2 4-4\"/></svg>",

        "alert-triangle" =>
            $"<svg {Attrs}><path d=\"M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z\"/><line x1=\"12\" y1=\"9\" x2=\"12\" y2=\"13\"/><line x1=\"12\" y1=\"17\" x2=\"12.01\" y2=\"17\"/></svg>",

        "alert-octagon" =>
            $"<svg {Attrs}><polygon points=\"7.86 2 16.14 2 22 7.86 22 16.14 16.14 22 7.86 22 2 16.14 2 7.86 7.86 2\"/><line x1=\"12\" y1=\"8\" x2=\"12\" y2=\"12\"/><line x1=\"12\" y1=\"16\" x2=\"12.01\" y2=\"16\"/></svg>",

        "arrow-right" =>
            $"<svg {Attrs}><line x1=\"5\" y1=\"12\" x2=\"19\" y2=\"12\"/><polyline points=\"12 5 19 12 12 19\"/></svg>",

        _ => string.Empty
    };

    public static string VerdictIcon(VerdictType verdict) => verdict switch
    {
        VerdictType.Legitimate => Svg("check-circle"),
        VerdictType.Suspicious => Svg("alert-triangle"),
        _ => Svg("alert-octagon")
    };
}
