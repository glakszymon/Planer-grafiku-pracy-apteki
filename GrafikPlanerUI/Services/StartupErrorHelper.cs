using System;

namespace GrafikPlanerUI.Services;

public static class StartupErrorHelper
{
    public static void ShowFatalError(Exception exception)
    {
        var message =
            "Grafik Planer nie mógł się uruchomić.\n\n" +
            "Przyczyna: " + exception.Message + "\n\n" +
            "Jeśli problem się powtarza, zamknij inne uruchomienia programu " +
            "i skontaktuj się z administratorem.";

        // Linux nie ma user32.dll — logujemy do stderr zamiast okna MessageBox.
        Console.Error.WriteLine(message);
        Console.Error.WriteLine(exception.ToString());
    }
}