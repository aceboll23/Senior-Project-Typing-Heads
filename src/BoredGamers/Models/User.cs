using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BoredGamers.Models
{
  //Represents a user profile stored by the system
  //Passwords will never be stored in plain text

  public class User : IdentityUser
  {

    [MaxLength(50)]
    public string? FirstName { get; set; }
        
    [MaxLength(50)]
    public string? LastName { get; set; }
    public DateOnly? Birthday { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
        
    // Id, UserName, Email, PasswordHash, etc. are inherited from IdentityUser
  }
}