using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoredGamers.Models;

public class ProfilePost
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserProfileId { get; set; }

    [ForeignKey("UserProfileId")]
    public UserProfile UserProfile { get; set; } = null!;

    [MaxLength(500)]
    public string? Content { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public PostCategory Category { get; set; } = PostCategory.None;

    public int? GameId { get; set; }

    [ForeignKey("GameId")]
    public Game? Game { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
