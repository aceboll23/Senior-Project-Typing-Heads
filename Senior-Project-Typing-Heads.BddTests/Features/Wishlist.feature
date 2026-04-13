Feature: Wishlist
  As a registered user
  I want to manage my game wishlist
  So that I can track games I want to buy

  Scenario: User can add a game to their wishlist
    Given I am logged in as a BDD wishlist user
    When I navigate to the game not on my wishlist
    And I click the Add to Wishlist button
    Then the game appears in my wishlist

  Scenario: User cannot add the same game to wishlist twice
    Given I am logged in as a BDD wishlist user
    When I navigate to the game already on my wishlist
    Then the Add to Wishlist button is disabled or shows On Wishlist

  Scenario: Wishlisted games appear separately from owned games on collection page
    Given I am logged in as a BDD wishlist user
    When I navigate to the game not on my wishlist
    And I click the Add to Wishlist button
    Then the wishlisted game appears in the wishlist section but not the owned section

  Scenario: Unauthenticated user does not see the Add to Wishlist button
    Given I am not logged in
    When I navigate to the game not on my wishlist as a guest
    Then the Add to Wishlist button is not visible