using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoredGamers.Models
{
    public class PlaygroupMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PlaygroupId { get; set; }

        [ForeignKey("PlaygroupId")]
        public Playgroup Playgroup { get; set; } = null!;

        public int? SenderProfileId { get; set; }

        [ForeignKey("SenderProfileId")]
        public UserProfile? SenderProfile { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        public bool IsSystemMessage { get; set; } = false;

        public DateTime SentAt { get; set; }
    }
}
