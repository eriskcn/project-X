namespace ProjectX.Validation;

public class FileValidator(ILogger<FileValidator> logger) : IFileValidator
{
    public async Task<bool> IsValidFileContentAsync(IFormFile file, string extension)
    {
        try
        {
            await using var stream = file.OpenReadStream();
            var buffer = new byte[8]; // Increased buffer size for more reliable validation
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

            return extension switch
            {
                ".jpg" or ".jpeg" => bytesRead >= 2 && buffer[0] == 0xFF && buffer[1] == 0xD8,
                ".png" => bytesRead >= 8 &&
                          buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47 &&
                          buffer[4] == 0x0D && buffer[5] == 0x0A && buffer[6] == 0x1A && buffer[7] == 0x0A,
                ".pdf" => bytesRead >= 4 &&
                          buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46,
                ".docx" => bytesRead >= 4 &&
                           buffer[0] == 0x50 && buffer[1] == 0x4B && buffer[2] == 0x03 && buffer[3] == 0x04,
                _ => false // Only allow explicitly validated types
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to validate file content for {FileName}", file.FileName);
            return false;
        }
    }
}