using System.ComponentModel.DataAnnotations;

namespace BoredGamers.Models
{
  public class GameNightEventGame
  {
    [Key]
    public int Id { get; set; }

    [Required]
    public int GameNightEventId { get; set; }

    [Required]
    public int GameId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public GameNightEvent? GameNightEvent { get; set; }

    public Game? Game { get; set; }

    public User? User { get; set; }
  }
}