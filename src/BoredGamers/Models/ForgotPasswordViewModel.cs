using System.ComponentModel.DataAnnotations;

namespace BoredGamers.Models;
public class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}