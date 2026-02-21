using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BoredGamers.Models;

namespace BoredGamers.Models
{
    public class BlockedUser
    {
        [Key]
        public int Id { get; set; }
        
        // UserProfile who is blocking
        [Required]
        public int BlockerProfileId { get; set; }
        
        [ForeignKey("BlockerProfileId")]
        public UserProfile BlockerProfile { get; set; } = null!;
        
        // UserProfile who is being blocked
        [Required]
        public int BlockedProfileId { get; set; }
        
        [ForeignKey("BlockedProfileId")]
        public UserProfile BlockedProfile { get; set; } = null!;
        // When the block was created
        public DateTime BlockedAt { get; set; }
    }
}