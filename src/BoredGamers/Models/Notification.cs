using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BoredGamers.Models;

namespace BoredGamers.Models
{

    public class Notification
    {
        [Key]
        public int Id { get; set; }
        
        // UserProfile who receives the notification
        [Required]
        public int UserProfileId { get; set; }
        
        [ForeignKey("UserProfileId")]
        public UserProfile UserProfile { get; set; } = null!;
        
        // Type of notification
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;
        
        // Notification title
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        // Notification message
        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;
        
        // Link to relevant page
        [MaxLength(500)]
        public string? ActionUrl { get; set; }
        
        // Related entity ID
        public int? RelatedEntityId { get; set; }
        
        // Whether the notification has been read
        public bool IsRead { get; set; } = false;
        
        // When the notification was read
        public DateTime? ReadAt { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; }
    }
}