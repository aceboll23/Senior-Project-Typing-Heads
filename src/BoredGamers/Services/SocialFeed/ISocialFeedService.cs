using BoredGamers.Models.ViewModels;

namespace BoredGamers.Services.SocialFeed;

public interface ISocialFeedService
{
    Task<IReadOnlyList<SocialFeedPostViewModel>> GetFeedForUserAsync(string userId);
}
