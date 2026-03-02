using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BoredGamers.Models
{
    public class Review
    {
        public int ReviewId { get; set; }

        [Required]
        public int GameId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Range(1, 10)]
        public int Rating { get; set; }

        [Required]
        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public IdentityUser? User { get; set; }
    }
}