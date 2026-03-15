using System.ComponentModel.DataAnnotations;

namespace BoredGamers.Models;

public class DeleteAccountViewModel
{
    [Required]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;
}