using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BoredGamers.Models
{
  public class GameNightEvent
  {
    [Required]
    public int PlaygroupId { get; set; }

    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public DateTime EventDateTime { get; set; }
    
    [StringLength(1000)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Playgroup? Playgroup { get; set; }

    public User? CreatedByUser { get; set; }

    public ICollection<GameNightEventGame> EventGames { get; set; } = new List<GameNightEventGame>();

  }
}