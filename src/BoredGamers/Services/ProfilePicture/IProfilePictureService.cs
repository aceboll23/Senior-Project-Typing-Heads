using BoredGamers.Services;
using Microsoft.AspNetCore.Http;

namespace BoredGamers.Services.ProfilePicture;

public interface IProfilePictureService
{
    Task<ServiceResult> UploadAvatarAsync(string userId, IFormFile file);
}
