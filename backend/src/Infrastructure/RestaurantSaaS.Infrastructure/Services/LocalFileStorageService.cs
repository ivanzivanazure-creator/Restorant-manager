using RestaurantSaaS.Application.Common.Interfaces;

namespace RestaurantSaaS.Infrastructure.Services;

/// <summary>Disk-backed IFileStorageService for local/dev use (product images, recipe photos, invoice
/// PDFs). Swap for an Azure Blob Storage implementation behind the same interface for production —
/// see deploy/azure/main.bicep, which provisions a Storage Account for exactly that purpose.</summary>
public sealed class LocalFileStorageService(string basePath) : IFileStorageService
{
    public async Task<string> UploadAsync(string containerName, string fileName, Stream content, string contentType, CancellationToken ct)
    {
        var containerPath = Path.Combine(basePath, containerName);
        Directory.CreateDirectory(containerPath);

        var safeFileName = $"{Guid.NewGuid():N}-{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(containerPath, safeFileName);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, ct);

        return $"/uploads/{containerName}/{safeFileName}";
    }
}
