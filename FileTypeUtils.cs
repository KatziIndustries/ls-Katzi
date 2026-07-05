public static class FileTypeUtils
{
    public static string DefaultApplicationsPath => Path.Combine(App.ConfigDirPath, "DefaultApplications.katzi");
    public static string FileTypeExtensionsPath => Path.Combine(App.ConfigDirPath, "FileTypeExtensions.katzi");

    public static readonly Dictionary<FileType, string> DefaultApplications = new();
    public static readonly Dictionary<string, FileType> FileTypeExtensions = new();

    public static readonly Dictionary<FileType, List<string>> ExtensionsFromFileType = new();
    
    public static void Init()
    {
        InitFileTypeExtensions();
        InitDefaultApplications();
    }

    private static void InitDefaultApplications()
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

                int firstSpace = entry.IndexOf(' ');
                string fileTypeString = entry.Substring(0, firstSpace);

                string defaultApplication = entry.Substring(firstSpace + 1, entry.Length - firstSpace - 1);

                FileType fileType = (FileType)Enum.Parse(typeof(FileType), fileTypeString);
                defaultApplications.Add(fileType, defaultApplication);
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
            RebuildDefaultApplications();
    }

    private static void InitFileTypeExtensions()
    {
        if (!File.Exists(FileTypeExtensionsPath))
            File.Copy(Path.Combine(App.AssetDirPath, "FileTypeExtensions.katzi"), FileTypeExtensionsPath);

        string[] lines = File.ReadAllText(FileTypeExtensionsPath).Split('\n');

        FileType currentFileType = FileType.Unknown;
        ExtensionsFromFileType.Add(FileType.Unknown, new());

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith('-'))
            {
                string extension = lines[i][1..lines[i].Length];
                FileTypeExtensions.Add(extension, currentFileType);

                ExtensionsFromFileType[currentFileType].Add(extension);
            }
            else
            {
                if (Enum.TryParse(lines[i], false, out FileType fileType))
                {
                    currentFileType = fileType;
                    ExtensionsFromFileType.Add(fileType, new());
                }
            }
        }
    }

    public static FileType FromExtension(string extension)
    {
        if (FileTypeExtensions.TryGetValue(extension, out FileType fileType))
        {
            return fileType;
        }
        else
        {
            return FileType.Unknown;
        }
    }

    private static void RebuildDefaultApplications()
    {
        using (var fs = File.Create(DefaultApplicationsPath)) { }
            
        foreach (var kvp in DefaultApplications)
        {
            File.AppendAllText(DefaultApplicationsPath, $"{kvp.Key} {kvp.Value}\n");
        }
    }
}