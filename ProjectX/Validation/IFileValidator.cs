namespace ProjectX.Validation;

public interface IFileValidator
{
    Task<bool> IsValidFileContentAsync(IFormFile file, string extension);
}