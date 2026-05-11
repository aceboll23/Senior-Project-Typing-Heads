using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BoredGamers.Models;

public class ProfilePostFlag
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProfilePostId { get; set; }

    [ForeignKey("ProfilePostId")]
    public ProfilePost ProfilePost { get; set; } = null!;

    [Required]
    public string ReporterId { get; set; } = null!;

    public DateTime ReportedAt { get; set; }
}
