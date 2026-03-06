using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoredGamers.Models
{
    public class DirectMessage
    {
        [Key]
        public int Id {get;set;}

        //Sender
        [Required]
        public int SenderProfileId {get;set;}
        [ForeignKey("SenderProfileId")]
        public UserProfile SenderProfile {get;set;} = null!;
        //Recipient
        [Required]
        public int RecipientProfileId {get;set;}
        [ForeignKey("RecipientProfileId")]
        public UserProfile RecipientProfile {get;set;} = null!;
        //Message content
        [Required]
        [MaxLength(1000)]
        public string Content {get;set;} = string.Empty;
        //Delivery status
        public MessageStatus Status {get;set;} = MessageStatus.Sent;
        //When the recipient reads the message
        public DateTime? ReadAt {get;set;}
        //Soft delete (hide from sender/recipietn without removing from DB)
        public bool DeletedBySender {get;set;} = false;
        public bool DeletedByRecipient {get;set;} = false;

        public DateTime SentAt {get;set;}
    }

    public enum MessageStatus
    {
        Sent,
        Delivered,
        Read
    }
}