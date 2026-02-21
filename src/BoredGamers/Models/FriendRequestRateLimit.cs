using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BoredGamers.Models;

namespace BoredGamers.Models
{
    public class FriendRequestRateLimit
    {
        [Key]
        public int Id { get; set; }
        
        // UserProfile who sent the request
        [Required]
        public int UserProfileId { get; set; }
        
        [ForeignKey("UserProfileId")]
        public UserProfile UserProfile { get; set; } = null!;
        
        // When the request was sent
        public DateTime RequestSentAt { get; set; }
        
        // Track the date (for counting requests per day)
        public DateOnly RequestDate { get; set; }
    }
}