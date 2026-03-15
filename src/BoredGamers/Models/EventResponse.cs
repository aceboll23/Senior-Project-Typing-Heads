using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoredGamers.Models
{
    public class EventResponse
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GameNightEventId { get; set; }

        [ForeignKey("GameNightEventId")]
        public GameNightEvent GameNightEvent { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public ResponseStatus Status { get; set; }

        public DateTime RespondedAt { get; set; }
    }

    public enum ResponseStatus
    {
        Going,
        Maybe,
        NotGoing
    }
}