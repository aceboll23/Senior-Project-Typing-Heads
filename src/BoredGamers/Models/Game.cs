using System;
using System.ComponentModel.DataAnnotations;

namespace BoredGamers.Models
{
  //Represents a board game stored locally so the home page can Load quickly
  //without calling BGG on every request.
  public class Game
  {
    public int Id { get; set; }

    //BoardGameGeek Game id (unique)
    [Required]
    public int BggGameId { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public int? YearPublished { get; set; }

    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    //1...100 from BGG's ranked list
    public int? BggRank { get; set; }

    public decimal? AverageRating { get; set; }

    //Game description pulled from BGG
    public string? Description { get; set; }

    //Player count from BGG
    public int? MinPlayers { get; set; }
    public int? MaxPlayers { get; set; }

    //Play time in minutes
    public int? PlayTime { get; set; }

    //When this record was last refreshed from BGG data
    public DateTime LastSyncedAt { get; set;}

  }
}