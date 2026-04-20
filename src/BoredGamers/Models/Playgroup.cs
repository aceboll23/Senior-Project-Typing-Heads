using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // hass [Key], [Required], [MaxLength]

namespace BoredGamers.Models
{
    // each property becomes a column, each object a row (in the table)
    public class Playgroup
    {
        [Key]
        public int Id { get; set; } 

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [Required]
        public string CreatedByUserId { get; set; } = string.Empty;

        public bool IsPrivate { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        //write playgroup.Members in C# to get all the member rows
        public ICollection<PlaygroupMember> Members { get; set; } = new List<PlaygroupMember>();
        
        // Helper methods — move logic out of controller/ViewData into the model
        public bool IsOwner(string userId) => Members.Any(m => m.UserId == userId && m.Role == PlaygroupRole.Owner);
        public bool IsMember(string userId) => Members.Any(m => m.UserId == userId);

        public int MemberCount() => Members.Count;
        
        public ICollection<GameNightEvent> GameNightEvents { get; set; } = new List<GameNightEvent>();
        public ICollection<PlaygroupMessage> Messages { get; set; } = new List<PlaygroupMessage>();
    }
}