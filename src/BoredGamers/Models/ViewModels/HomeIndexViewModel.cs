using System.Collections.Generic;

namespace BoredGamers.Models.ViewModels
{
  //Keeps HomeController thin and gives the Index view exactly what it needs:
  //a list of featured games to display (Top N).

  public class HomeIndexViewModel
  {
    public IReadOnlyList<Game> FeaturedGames { get; set; } = new List<Game>();
  }
}