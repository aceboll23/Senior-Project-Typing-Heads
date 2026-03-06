using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BoredGamers.Models;


namespace BoredGamers.Models
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }

        //foreign key to user
        [Required]
        public string UserId { get; set; } = string.Empty;

        //navigation property
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        

        [MaxLength(500)]
        public string? AvatarUrl { get; set; }

        [MaxLength(500)]
        public string? Bio { get; set; }

        

        // Privacy settings
        public bool IsProfilePublic { get; set; } = true;
        public bool ShowEmail { get; set; } = false;
        public bool ShowBirthday { get; set; } = true;
        public bool ShowRealName { get; set; } = true;
        public bool ShowGameCollection { get; set; } = true;
        public bool AllowFriendRequests { get; set; } = true;

        // Friendships where this user is the requester
        public ICollection<Friendship> SentFriendRequests { get; set; } = new List<Friendship>();
        // Friendships where this user is the receiver
        public ICollection<Friendship> ReceivedFriendRequests { get; set; } = new List<Friendship>();
        // Users this user has blocked
        public ICollection<BlockedUser> BlockedUsers { get; set; } = new List<BlockedUser>();
        //Users who have blocked this user
        public ICollection<BlockedUser> BlockedByUsers { get; set; } = new List<BlockedUser>();
        //Notifications for this user
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        //Rate limit tracking
        public ICollection<FriendRequestRateLimit> FriendRequestRateLimits { get; set; } = new List<FriendRequestRateLimit>();
        // Tracks of messages sent and received
        public ICollection<DirectMessage> SentMessages { get; set; } = new List<DirectMessage>();
        public ICollection<DirectMessage> ReceivedMessages { get; set; } = new List<DirectMessage>();

        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}