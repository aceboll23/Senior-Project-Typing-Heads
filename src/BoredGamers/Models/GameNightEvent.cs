using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BoredGamers.Models
{
  public class GameNightEvent
  {
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int PlaygroupId { get; set; }

    [Required]
    public string CreatedByUserId { get; set; } = string.Empty; //FK to get the value from AspNetUser

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public DateTime EventDateTime { get; set; }
    
    [StringLength(1000)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Playgroup? Playgroup { get; set; }

    public User? CreatedByUser { get; set; } // Brings in the full User Object

    public ICollection<GameNightEventGame> EventGames { get; set; } = new List<GameNightEventGame>();

    // For playgroup game voting
    public VotingStatus VotingStatus { get; set; } = VotingStatus.NotStarted;
    public ICollection<GameVote> GameVotes { get; set; } = new List<GameVote>();
  }
  // For playgroup game voting
  public enum VotingStatus
  {
      NotStarted,
      Open,
      Closed
  }
  
}

