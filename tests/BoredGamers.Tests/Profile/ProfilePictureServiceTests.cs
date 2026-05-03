using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.ProfilePicture;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;

namespace BoredGamers.Tests.Profile;

[TestFixture]
public class ProfilePictureServiceTests
{
    private ApplicationDbContext _db = null!;
    private ProfilePictureService _service = null!;
    private string _tempWebRoot = null!;

    private User _user = null!;
    private UserProfile _profile = null!;

    private static async Task<ApplicationDbContext> CreateSqliteInMemoryDbAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    [SetUp]
    public async Task SetUp()
    {
        _db = await CreateSqliteInMemoryDbAsync();

        _tempWebRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempWebRoot);

        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.WebRootPath).Returns(_tempWebRoot);

        _service = new ProfilePictureService(_db, mockEnv.Object, new Mock<IConfiguration>().Object);

        _user = new User { Id = "user-1", UserName = "TestUser", Email = "test@test.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Users.Add(_user);
        await _db.SaveChangesAsync();

        _profile = new UserProfile { UserId = _user.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Set<UserProfile>().Add(_profile);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
        if (Directory.Exists(_tempWebRoot))
            Directory.Delete(_tempWebRoot, recursive: true);
    }

    private static IFormFile MakeFormFile(string fileName, long sizeBytes, string contentType = "image/jpeg")
    {
        var content = new byte[sizeBytes];
        var stream = new MemoryStream(content);
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.Length).Returns(sizeBytes);
        mock.Setup(f => f.ContentType).Returns(contentType);
        mock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns((Stream target, CancellationToken _) => stream.CopyToAsync(target));
        return mock.Object;
    }

    // --- UploadAvatarAsync ---

    [Test]
    public async Task UploadAvatarAsync_ValidJpeg_ReturnsSuccess()
    {
        var file = MakeFormFile("photo.jpg", 1024);
        var result = await _service.UploadAvatarAsync(_user.Id, file);
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task UploadAvatarAsync_ValidJpeg_UpdatesAvatarUrl()
    {
        var file = MakeFormFile("photo.jpg", 1024);
        await _service.UploadAvatarAsync(_user.Id, file);

        var updated = await _db.Set<UserProfile>().FirstAsync(p => p.UserId == _user.Id);
        Assert.That(updated.AvatarUrl, Does.StartWith("/uploads/avatars/"));
        Assert.That(updated.AvatarUrl, Does.EndWith(".jpg"));
    }

    [Test]
    public async Task UploadAvatarAsync_ValidPng_ReturnsSuccess()
    {
        var file = MakeFormFile("photo.png", 2048, "image/png");
        var result = await _service.UploadAvatarAsync(_user.Id, file);
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task UploadAvatarAsync_InvalidExtension_ReturnsFail()
    {
        var file = MakeFormFile("malware.exe", 1024, "application/octet-stream");
        var result = await _service.UploadAvatarAsync(_user.Id, file);
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("JPG").Or.Contain("PNG").IgnoreCase);
    }

    [Test]
    public async Task UploadAvatarAsync_FileTooLarge_ReturnsFail()
    {
        var file = MakeFormFile("big.jpg", 6 * 1024 * 1024); // 6MB
        var result = await _service.UploadAvatarAsync(_user.Id, file);
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("5MB").IgnoreCase);
    }

    [Test]
    public async Task UploadAvatarAsync_ProfileNotFound_ReturnsFail()
    {
        var file = MakeFormFile("photo.jpg", 1024);
        var result = await _service.UploadAvatarAsync("nonexistent-user", file);
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task UploadAvatarAsync_ReplacesOldAvatar_DeletesOldFile()
    {
        // Seed an existing avatar file
        var avatarsDir = Path.Combine(_tempWebRoot, "uploads", "avatars");
        Directory.CreateDirectory(avatarsDir);
        var oldFileName = "old_avatar.jpg";
        var oldFilePath = Path.Combine(avatarsDir, oldFileName);
        await File.WriteAllBytesAsync(oldFilePath, new byte[100]);

        _profile.AvatarUrl = $"/uploads/avatars/{oldFileName}";
        await _db.SaveChangesAsync();

        var file = MakeFormFile("new.jpg", 1024);
        await _service.UploadAvatarAsync(_user.Id, file);

        Assert.That(File.Exists(oldFilePath), Is.False);
    }

    [Test]
    public async Task UploadAvatarAsync_SavesFileToDisk()
    {
        var file = MakeFormFile("photo.jpg", 512);
        await _service.UploadAvatarAsync(_user.Id, file);

        var profile = await _db.Set<UserProfile>().FirstAsync(p => p.UserId == _user.Id);
        var relativePath = profile.AvatarUrl!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_tempWebRoot, relativePath);

        Assert.That(File.Exists(fullPath), Is.True);
    }
}
