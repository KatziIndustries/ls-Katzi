public static class FileTypeUtils
{
    public static string DefaultApplicationsPath => Path.Combine(App.ConfigDirPath, "DefaultApplications.katzi");

    public static readonly Dictionary<FileType, string> DefaultApplications = new();
    
    public static void InitFileTypes()
    {
        if (!File.Exists(DefaultApplicationsPath))
            File.Copy(Path.Combine(App.AssetDirPath, "DefaultApplications.katzi"), DefaultApplicationsPath);

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
            DefaultApplications.Add(fileType, entries[1]);
        }
    }

    public static FileType FromExtension(string extension)
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
            extension == ".md")
        {
            return FileType.Text;
        }

        return FileType.Unknown;
    }
}