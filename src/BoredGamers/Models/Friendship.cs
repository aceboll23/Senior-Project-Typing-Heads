using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BoredGamers.Models;

namespace BoredGamers.Models
{
    public class Friendship
    {
        [Key]
        public int Id {get;set;}

        // UserProfile who sent the friend request
        [Required]
        public int RequesterProfileId { get; set; }
        
        [ForeignKey("RequesterProfileId")]
        public UserProfile RequesterProfile { get; set; } = null!;
        
        // UserProfile who received the friend request
        [Required]
        public int ReceiverProfileId { get; set; }
        
        [ForeignKey("ReceiverProfileId")]
        public UserProfile ReceiverProfile { get; set; } = null!;

        //status
        [Required]
        public FriendshipStatus Status {get;set;} = FriendshipStatus.Pending;

        //when the request was sent
        public DateTime RequestedAt {get;set;}

        //When the request was accepted or declined
        public DateTime? RespondedAt {get;set;}
        //When the request was declined
        public DateTime? LastDeclinedAt {get;set;}

        
        public DateTime CreatedAt {get;set;}
        public DateTime UpdatedAt {get;set;}
    }

    public enum FriendshipStatus
    {
        //Pending: request sent, awaiting response
        //Accepted: now friends
        //Declined: request declined
        //Cancelled: requester cancelled the request
        Pending,
        Accepted,
        Declined,
        Cancelled
    }
}