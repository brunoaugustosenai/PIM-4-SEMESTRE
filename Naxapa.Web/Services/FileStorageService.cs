namespace Naxapa.Web.Services;

public sealed class FileStorageService(IWebHostEnvironment environment)
{
    private const long MaxImageSize = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    public async Task<string?> SaveImageAsync(IFormFile? file, string folder, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            return null;
        if (file.Length > MaxImageSize)
            throw new InvalidOperationException("A imagem deve possuir no máximo 5 MB.");

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Formato de imagem não permitido. Use JPG, PNG ou WEBP.");

        await using var input = file.OpenReadStream();
        if (!await IsKnownImageAsync(input, extension, cancellationToken))
            throw new InvalidOperationException("O conteúdo do arquivo não corresponde a uma imagem válida.");
        input.Position = 0;

        var safeFolder = string.Concat(folder.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
        if (string.IsNullOrWhiteSpace(safeFolder))
            throw new InvalidOperationException("Pasta de upload inválida.");

        var relativeDirectory = Path.Combine("uploads", safeFolder);
        var physicalDirectory = Path.Combine(environment.WebRootPath, relativeDirectory);
        Directory.CreateDirectory(physicalDirectory);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var physicalPath = Path.Combine(physicalDirectory, fileName);
        await using var output = File.Create(physicalPath);
        await input.CopyToAsync(output, cancellationToken);

        return "/" + Path.Combine(relativeDirectory, fileName).Replace('\\', '/');
    }

    public void DeleteOwnedFile(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl) || !relativeUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return;

        var relative = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var root = Path.GetFullPath(environment.WebRootPath);
        var path = Path.GetFullPath(Path.Combine(root, relative));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            return;
        if (File.Exists(path))
            File.Delete(path);
    }

    private static async Task<bool> IsKnownImageAsync(Stream stream, string extension, CancellationToken cancellationToken)
    {
        var header = new byte[12];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
        if (read < 4)
            return false;

        if (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            return header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
            return read >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
        if (extension.Equals(".webp", StringComparison.OrdinalIgnoreCase))
            return read >= 12
                   && header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F'
                   && header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P';
        return false;
    }
}
