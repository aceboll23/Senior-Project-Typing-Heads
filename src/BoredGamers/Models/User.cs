using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using BoredGamers.Models;


namespace BoredGamers.Models
{
  //Represents a user profile stored by the system
  //Passwords will never be stored in plain text

  public class User : IdentityUser
  {

     // Id, UserName, Email, PasswordHash, etc. are inherited from IdentityUser

    //display info
    [MaxLength(50)]
    public string? FirstName { get; set; }

    [MaxLength(50)]
    public string? LastName { get; set; }
    public DateOnly? Birthday { get; set; }

    //navigation to profile
    public UserProfile? Profile {get; set;}

    // account status, whether a user is banned or their account is deactivated
    public bool IsBanned {get; set;} = false;
    public bool IsDeactivated {get; set;} = false; 
    // For when the user wants to change their email
    [MaxLength(256)]
    public string? PendingEmail {get; set;}
    [MaxLength(256)]
    public string? EmailVerificationToken { get; set; }

    public DateTime? EmailVerificationTokenExpiry { get; set; }

    // password reset fields
    [MaxLength(256)]
    public string? PasswordResetToken {get; set;}
    public DateTime? PasswordResetTokenExpiry {get;set;}

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }    
  }
}