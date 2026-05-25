using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoredGamers.Models;

public class PostLike
{
    [Key] public int Id { get; set; }

    [Required] public int PostId { get; set; }
    [ForeignKey("PostId")] public ProfilePost Post { get; set; } = null!;

    [Required] public string UserId { get; set; } = string.Empty;
    [ForeignKey("UserId")] public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
