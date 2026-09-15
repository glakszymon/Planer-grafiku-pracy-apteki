using System;
using System.Runtime.InteropServices;

namespace GrafikPlanerUI.Services;

public static class StartupErrorHelper
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    public static void ShowFatalError(Exception exception)
    {
        var message =
            "Grafik Planer nie mógł się uruchomić.\n\n" +
            "Przyczyna: " + exception.Message + "\n\n" +
            "Jeśli problem się powtarza, zamknij inne uruchomienia programu " +
            "i skontaktuj się z administratorem.";

        MessageBoxW(IntPtr.Zero, message, "Grafik Planer — błąd uruchamiania",
            0x00000010 /* MB_ICONERROR */);
    }
}