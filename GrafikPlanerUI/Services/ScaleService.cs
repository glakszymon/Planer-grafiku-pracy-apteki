using System;
using Avalonia.Controls;
using Avalonia.Media;

namespace GrafikPlanerUI.Services;

public static class ScaleService
{
    public const double DesignWidth = 1920;
    public const double DesignHeight = 1080;
    public const double MinScale = 0.5;
    public const double MaxScale = 1.5;
    public const double BaseRowHeight = 55;
    public const double MinRowHeight = 30;

    public static double Compute(Window window)
    {
        if (window is null)
            return 1.0;

        var size = window.ClientSize;
        if (size.Width <= 0 || size.Height <= 0)
            return 1.0;

        double s = Math.Min(size.Width / DesignWidth, size.Height / DesignHeight);
        s = Math.Clamp(s, MinScale, MaxScale);
        return Math.Round(s, 2);
    }

    public static double Apply(Window window, LayoutTransformControl host)
    {
        double s = Compute(window);

        if (host.LayoutTransform is not ScaleTransform scale)
        {
            host.LayoutTransform = new ScaleTransform(s, s);
        }
        else if (scale.ScaleX != s || scale.ScaleY != s)
        {
            scale.ScaleX = s;
            scale.ScaleY = s;
        }

        return s;
    }
}