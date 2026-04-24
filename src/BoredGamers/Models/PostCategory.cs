using System.ComponentModel.DataAnnotations;

namespace BoredGamers.Models;

public enum PostCategory
{
    None = 0,

    [Display(Name = "Currently Playing")]
    CurrentlyPlaying = 1,

    [Display(Name = "Want to Play")]
    WantToPlay = 2,

    [Display(Name = "Completed")]
    Completed = 3,

    [Display(Name = "Top 10")]
    Top10 = 4
}
