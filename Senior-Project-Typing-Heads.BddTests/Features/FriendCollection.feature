Feature: Friend Collection
  As a registered user
  I want to view a friend's game collection
  So that I can see what games they own

  Scenario: Friend's owned games are visible on their collection page
    Given I am logged in as a BDD friend collection viewer
    When I navigate to the friend's collection page
    Then the owned game is shown

  Scenario: Wishlist items are not shown on a friend's collection page
    Given I am logged in as a BDD friend collection viewer
    When I navigate to the friend's collection page
    Then the wishlist game is not shown

  Scenario: Empty state is shown when friend has no owned games
    Given I am logged in as a BDD friend collection viewer
    When I navigate to the empty friend's collection page
    Then an empty state message is shown

  Scenario: Collection page has a back link to the friend's profile
    Given I am logged in as a BDD friend collection viewer
    When I navigate to the friend's collection page
    Then a back link to the friend's profile is present

  Scenario: Viewing own collection via friend route redirects to My Collection
    Given I am logged in as a BDD friend collection viewer
    When I navigate to my own collection via the friend route
    Then I am redirected to my own collection page
