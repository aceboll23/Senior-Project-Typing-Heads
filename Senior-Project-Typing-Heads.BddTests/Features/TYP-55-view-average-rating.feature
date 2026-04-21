Feature: View Average Rating (TYP-55)
  As a user browsing a game
  I want to see the average of user-submitted ratings for the game
  So that I know how users on this site feel about it

  Scenario: Game details page shows the average of user-submitted ratings
    Given I am logged in as a BDD average rating user
    When I navigate to the game details page
    Then I see the average user rating displayed
