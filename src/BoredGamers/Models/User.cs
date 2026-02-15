using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BoredGamers.Models
{
  //Represents a user profile stored by the system
  //Passwords will never be stored in plain text

  public class User : IdentityUser
  {
    //Primary Key
    /** public int Id { get; set; }

    [MaxLength(50)]
    public string? FirstName { get; set; }

    [MaxLength(50)]
    public string? LastName { get; set; }

    //Public-facing username (must be unique)
    [Required, MaxLength(30)]
    public string Username { get; set; } = string.Empty;

    //Email used for contact and future authentication
    [Required, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    //Hashed password only
    [Required, MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    //Optional user-provided birthday
    public DateOnly? Birthday { get; set; }

    //Audit fields
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; } */

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