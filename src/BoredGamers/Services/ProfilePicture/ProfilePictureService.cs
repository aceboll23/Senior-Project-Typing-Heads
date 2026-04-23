using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.ProfilePicture;

public class ProfilePictureService : IProfilePictureService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ProfilePictureService(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<ServiceResult> UploadAvatarAsync(string userId, IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(ext))
            return ServiceResult.Fail("Only JPG, PNG, GIF, and WebP files are allowed.");

        if (file.Length > MaxFileSizeBytes)
            return ServiceResult.Fail("File must be 5MB or smaller.");

        var profile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
            return ServiceResult.Fail("Profile not found.");

        // Delete old avatar if it exists
        if (!string.IsNullOrWhiteSpace(profile.AvatarUrl))
        {
            var oldRelative = profile.AvatarUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var oldPath = Path.Combine(_env.WebRootPath, oldRelative);
            if (File.Exists(oldPath))
                File.Delete(oldPath);
        }

        // Save new file
        var avatarsDir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(avatarsDir);

        var fileName = $"{userId}_{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(avatarsDir, fileName);

        using (var stream = File.Create(filePath))
            await file.CopyToAsync(stream);

        profile.AvatarUrl = $"/uploads/avatars/{fileName}";
        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ServiceResult.Ok();
    }
}
