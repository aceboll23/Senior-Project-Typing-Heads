@trade
Feature: Trade Status
  As a registered user
  I want to mark games in my collection as available for trade
  So that other users can know which games I am willing to trade

  @mark-as-trade
  Scenario: Collection page shows a Mark as Trade option for each game
    Given I am logged in as the trade test owner with a game in their collection
    When I navigate to my collection page
    Then I should see a Mark as Trade button for my game

  @trade-status-persists
  Scenario: Marking a game as available for trade persists
    Given I am logged in as the trade test owner with a game in their collection
    And I navigate to my collection page
    When I click the Mark as Trade button for my game
    And I navigate to my collection page
    Then I should see the Available for Trade badge for my game

  @remove-trade-status
  Scenario: Removing trade status from a game
    Given I am logged in as the trade test owner with a game marked as available for trade
    And I navigate to my collection page
    When I click the Remove from Trade button for my game
    Then I should not see the Available for Trade badge for my game

  @trade-status-visible
  Scenario: Trade status badge is visible when a game is already marked
    Given I am logged in as the trade test owner with a game marked as available for trade
    When I navigate to my collection page
    Then I should see the Available for Trade badge for my game

  @unauthenticated-cannot-trade
  Scenario: Unauthenticated users are redirected when accessing the collection
    Given I am not logged in as any user
    When I navigate to my collection page
    Then I should be redirected to the login page
