using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoredGamers.Models;

public class GameVote
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int GameNightEventId { get; set; }

    [ForeignKey("GameNightEventId")]
    public GameNightEvent GameNightEvent { get; set; } = null!;

    // The game being ranked
    [Required]
    public int GameNightEventGameId { get; set; }

    [ForeignKey("GameNightEventGameId")]
    public GameNightEventGame GameNightEventGame { get; set; } = null!;

    // The user who submitted this ranking
    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public User User { get; set; } = null!;

    // Rank position — 1 = first choice, 2 = second choice, etc.
    [Required]
    public int Rank { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}