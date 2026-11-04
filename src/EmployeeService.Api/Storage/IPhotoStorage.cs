using EmployeeAbsence.Shared;

namespace EmployeeService.Api.Storage;

public interface IPhotoStorage
{
    /// <summary>Saves the uploaded image and returns its relative URL, e.g. <c>/uploads/abc.jpg</c>.</summary>
    Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);
}

public sealed class LocalPhotoStorage(IWebHostEnvironment env) : IPhotoStorage
{
    public const long MaxBytes = 2 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length == 0 || file.Length > MaxBytes)
        {
            throw new BusinessValidationException("Photo must be between 1 byte and 2 MB.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessValidationException("Only JPG, PNG or WEBP images are allowed.");
        }

        var folder = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        await using var stream = File.Create(Path.Combine(folder, fileName));
        await file.CopyToAsync(stream, cancellationToken);
        return $"/uploads/{fileName}";
    }
}
