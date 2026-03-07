using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoredGamers.Models
{
    public enum PlaygroupRole
    {
        Owner,
        Member
    }

    public class PlaygroupMember
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PlaygroupId { get; set; }

        [ForeignKey("PlaygroupId")]
        public Playgroup Playgroup { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public PlaygroupRole Role { get; set; } = PlaygroupRole.Member;

        public DateTime JoinedAt { get; set; }
    }
}