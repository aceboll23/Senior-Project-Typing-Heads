Feature: Remove Wishlist Item (TYP-50)
  As a logged-in user
  I want to remove a game from my wishlist
  So that my wishlist stays up to date

  Scenario: User removes a game from their wishlist
    Given I am logged in as a BDD wishlist user
    And the game is already on my wishlist
    When I navigate to my collection page
    And I click the Remove from Wishlist button
    Then the game no longer appears in my wishlist
