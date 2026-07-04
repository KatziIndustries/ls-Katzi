public static class FileTypeUtils
{
    public static string DefaultApplicationsPath => Path.Combine(App.ConfigDirPath, "DefaultApplications.katzi");

    public static readonly Dictionary<FileType, string> DefaultApplications = new();
    
    public static void InitFileTypes()
    {
        Dictionary<FileType, string> defaultApplications = new();

        if (File.Exists(DefaultApplicationsPath))
        {
            string[] lines = File.ReadAllText(DefaultApplicationsPath).Split('\n', StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length == 0) 
                return;


            for (int i = 0; i < lines.Length; i++)
            {
                string entry = lines[i];
                string[] entries = entry.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (entries.Length != 2)
                    throw new InvalidDataException($"Default applications file wasn't in a correct format; line: {i + 1}");

                FileType fileType = (FileType)Enum.Parse(typeof(FileType), entries[0]);
                defaultApplications.Add(fileType, entries[1]);
            }
        }


        bool rebuildConfig = false;
        foreach (FileType fileType in Enum.GetValues<FileType>())
        {
            if (fileType == FileType.Unknown) 
                continue;
            
            defaultApplications.TryGetValue(fileType, out string? loadedApplication);

            if (loadedApplication == null)
            {
                DefaultApplications.Add(fileType, "Built-In");
                rebuildConfig = true;
            }
            else DefaultApplications.Add(fileType, loadedApplication);
        }

        if (rebuildConfig)
            RebuildConfig();
    }

    public static FileType FromExtension(string extension, string entry)
    {
        if (extension == ".jpeg" ||
            extension == ".jpg"  ||
            extension == ".png"  ||
            extension == ".gif"  ||
            extension == ".webp" ||
            extension == ".avif" ||
            extension == ".tiff" ||
            extension == ".bmp"  ||
            extension == ".ppm"  ||
            extension == ".pgm"  ||
            extension == ".pbm"  ||
            extension == ".pnm")
        {
            return FileType.Image;
        }

        if (extension == ".zip" ||
            extension == ".gz"  ||
            extension == ".bz2" ||
            extension == ".xz"  ||
            extension == ".tar" ||
            extension == ".z"   ||
            extension == ".7z")
        {
            return FileType.CompressedArchive;
        }

        if (extension == ".txt" ||
            extension == ".md"  ||
            extension == ".sh"  ||
            extension == ".bat")
        {
            return FileType.Text;
        }

        if (extension == ".exe" && OperatingSystem.IsWindows())
        {
            return FileType.Executable;
        }

        if (OperatingSystem.IsLinux())
        {
            UnixFileMode fileMode = File.GetUnixFileMode(entry);

            if (fileMode.HasFlag(UnixFileMode.UserExecute))
                return FileType.Executable;
        }

        return FileType.Unknown;
    }

    private static void RebuildConfig()
    {
        using (var fs = File.Create(DefaultApplicationsPath)) { }
            
        foreach (var kvp in DefaultApplications)
        {
            File.AppendAllText(DefaultApplicationsPath, $"{kvp.Key} {kvp.Value}\n");
        }
    }
}