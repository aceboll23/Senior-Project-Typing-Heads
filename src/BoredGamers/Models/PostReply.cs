using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoredGamers.Models;

public class PostReply
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PostId { get; set; }

    [ForeignKey("PostId")]
    public ProfilePost Post { get; set; } = null!;

    [Required]
    public string AuthorId { get; set; } = string.Empty;

    [ForeignKey("AuthorId")]
    public User Author { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
