namespace ProjectX.Helpers;

public static class PathHelper
{
    /// <summary>
    /// Chuyển đổi một đường dẫn vật lý tuyệt đối (trong wwwroot)
    /// thành đường dẫn URL tương đối có thể dùng bởi client.
    /// </summary>
    public static string GetRelativePathFromAbsolute(string absolutePath, string webRootPath)
    {
        if (string.IsNullOrEmpty(absolutePath) || string.IsNullOrEmpty(webRootPath))
            return string.Empty;

        var normalizedAbsolute = Path.GetFullPath(absolutePath).Replace("\\", "/");
        var normalizedRoot = Path.GetFullPath(webRootPath).Replace("\\", "/");

        if (!normalizedAbsolute.StartsWith(normalizedRoot))
            return string.Empty;

        var relativePath = normalizedAbsolute.Substring(normalizedRoot.Length);
        return "/" + relativePath.TrimStart('/');
    }

    /// <summary>
    /// Loại bỏ các ký tự không hợp lệ khỏi tên file 
    /// </summary>
    public static string GetCleanFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var cleanFileName = string.Join("_",
            fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return cleanFileName;
    }
}