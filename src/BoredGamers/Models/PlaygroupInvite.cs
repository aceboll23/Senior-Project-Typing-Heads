using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoredGamers.Models
{
    public class PlaygroupInvite
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PlaygroupId { get; set; }

        [ForeignKey("PlaygroupId")]
        public Playgroup Playgroup { get; set; } = null!;

        // The user being invited (Identity UserId string)
        [Required]
        public string InvitedUserId { get; set; } = string.Empty;

        // The user who sent the invite (Identity UserId string)
        [Required]
        public string InvitedByUserId { get; set; } = string.Empty;

        [Required]
        public InviteStatus Status { get; set; } = InviteStatus.Pending;

        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }

    public enum InviteStatus
    {
        Pending,
        Accepted,
        Declined
    }
}