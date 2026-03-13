using System;
using System.ComponentModel.DataAnnotations;

namespace BoredGamers.Models.ViewModels
{
  public class CreateGameNightEventViewModel
  {
    [Required]
    public int PlaygroupId { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Event Title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Date and Time")]
    public DateTime EventDateTime { get; set; } = DateTime.Now;

    [StringLength(1000)]
    public string? Description { get; set; }

    public bool ConfirmDuplicateDate { get; set; } = false;
    public string? WarningMessage { get; set; } 
  }
}