using System.Diagnostics;

public static class Utils
{
    private static readonly string[] _sizeUnits = { "B", "KB", "MB", "GB", "TB" };

    public static string GetParentDirectory(string current)
    {
        if (File.Exists(current))
            return Path.GetDirectoryName(current)!;

        string dirName = Path.GetDirectoryName(current) ?? "/";
        return (Directory.GetParent(dirName) ?? new DirectoryInfo("/")).FullName;
    }

    public static string JumpBack(string current)
    {
        if (current[current.Length - 1] == Path.DirectorySeparatorChar)
        {
            current = current.Remove(current.Length - 1, 1);
        }

        int index = current.LastIndexOf(Path.DirectorySeparatorChar);
        return current.Substring(0, index + 1);
    }

    public static void OpenTerminal(string path)
    {
        string defaultTerminal = ConfigScene.DefaultTerminal;
        ProcessStartInfo processStartInfo = new()
        {
            FileName = defaultTerminal,
            Arguments = $"--directory {path}",
            UseShellExecute = false
        };

        Process.Start(processStartInfo);
    }

    public static string GetFileLengthReadable(long length)
    {
        double size = length;
        int order = 0;

        while (size >= 1024 && order < _sizeUnits.Length - 1)
        {
            order++;
            size /= 1024;
        }

        if (_sizeUnits[order] == "B")
            return $"{size:0} {_sizeUnits[order]}";
        else
            return $"{size:0.0} {_sizeUnits[order]}";
    }

    public static FileInfo? GetFileInfo(string path)
    {
        if (!File.Exists(path))
            return null;

        return new FileInfo(path);
    }
}