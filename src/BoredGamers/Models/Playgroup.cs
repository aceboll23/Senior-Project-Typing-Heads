using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BoredGamers.Models
{
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

        public ICollection<PlaygroupMember> Members { get; set; } = new List<PlaygroupMember>();
    }
}