public static class FileTypeUtils
{
    public static readonly Dictionary<FileType, string> DefaultApplications = new()
    {
        { FileType.Image, "Built-In" }
    };

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

        return FileType.Unknown;
    }
}