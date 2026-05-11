using System.Threading;
using System.Threading.Tasks;

namespace BoredGamers.Services.Flags;

public interface IPostFlagService
{
    Task<ServiceResult> FlagPostAsync(string userId, int postId,
        CancellationToken ct = default);
}
