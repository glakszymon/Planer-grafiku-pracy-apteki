using Microsoft.Data.Sqlite;

namespace GrafikPlanerData;

public static class DatabasePath
{
    private const string AppFolderName = "GrafikPlaner";
    private const string DatabaseFileName = "apteka.db";

    public static string GetPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(appData) || !Path.IsPathRooted(appData))
            appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");

        return Path.Combine(appData, AppFolderName, DatabaseFileName);
    }

    public static string GetConnectionString()
    {
        var path = GetPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return new SqliteConnectionStringBuilder { DataSource = path }.ToString();
    }

    public static void EnsureMigrated()
    {
        var destination = GetPath();

        if (File.Exists(destination))
            return;

        var source = FindSourceDatabase();
        if (source == null)
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

        var tempPath = destination + ".tmp";
        try
        {
            File.Copy(source, tempPath, overwrite: true);

            if (!IsValidSqliteFile(tempPath))
                throw new InvalidDataException($"Skopiowana baza danych nie jest poprawnym plikiem SQLite: {tempPath}");

            try
            {
                File.Move(tempPath, destination, overwrite: false);
            }
            catch (IOException)
            {
                // Destination został utworzony w międzyczasie (inna instancja) — to nie jest błąd.
            }
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    private static string? FindSourceDatabase()
    {
        var candidates = new List<string> { Path.Combine(AppContext.BaseDirectory, DatabaseFileName) };

        var currentDirectory = Environment.CurrentDirectory;
        if (!string.Equals(currentDirectory, AppContext.BaseDirectory, StringComparison.OrdinalIgnoreCase))
            candidates.Add(Path.Combine(currentDirectory, DatabaseFileName));

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate) && IsValidSqliteFile(candidate))
                return candidate;
        }

        return null;
    }

    private static bool IsValidSqliteFile(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        if (stream.Length < 16)
            return false;

        Span<byte> header = stackalloc byte[16];
        var totalRead = 0;
        while (totalRead < header.Length)
        {
            var read = stream.Read(header[totalRead..]);
            if (read == 0)
                break;
            totalRead += read;
        }

        return totalRead == header.Length && header.SequenceEqual("SQLite format 3\0"u8);
    }
}