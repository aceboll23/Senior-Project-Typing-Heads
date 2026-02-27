using System;

namespace BoredGamers.Models
{
    public class UserGameCollection
    {
        public int Id { get; set; }

        // FK to AspNetUsers (IdentityUser.Id is string)
        public string UserId { get; set; } = null!;

        // FK to Games table
        public int GameId { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        // Navigation properties (optional but recommended)
        public Game Game { get; set; } = null!;
    }
}