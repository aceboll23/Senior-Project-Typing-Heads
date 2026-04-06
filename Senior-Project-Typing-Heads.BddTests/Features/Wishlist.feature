Feature: Wishlist
  As a registered user
  I want to manage my game wishlist
  So that I can track games I want to buy

  Scenario: User can add a game to their wishlist
    Given I am logged in as PersonThree
    When I navigate to the Dune game details page
    And I click the Add to Wishlist button
    Then the Dune game appears in my wishlist

  Scenario: User cannot add the same game to wishlist twice
    Given I am logged in as PersonThree
    When I navigate to the Dune game details page
    Then the Add to Wishlist button is disabled or shows On Wishlist
