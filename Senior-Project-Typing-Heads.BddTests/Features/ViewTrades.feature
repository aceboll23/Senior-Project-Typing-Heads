@view-trades
Feature: View Friend's Available Trades
  As a registered user
  I want to browse games my friends have available for trade
  So that I can see which games they may be willing to trade

  @view-trades-button
  Scenario: View Available Trades button appears on a friend's profile
    Given I am logged in as the view-trades viewer
    When I navigate to my trade friend's profile
    Then I should see a View Available Trades button

  @view-trades-navigate
  Scenario: Clicking View Available Trades navigates to the trade page
    Given I am logged in as the view-trades viewer
    And I navigate to my trade friend's profile
    When I click the View Available Trades button
    Then I should be on my trade friend's available trades page

  @view-trades-only-tradeable
  Scenario: Only games marked as available for trade are shown
    Given I am logged in as the view-trades viewer
    When I navigate to my trade friend's available trades page
    Then I should see the tradeable game on the page
    And I should not see the non-tradeable game on the page

  @view-trades-empty
  Scenario: Friendly message when friend has no tradeable games
    Given I am logged in as the view-trades viewer
    When I navigate to the no-trades friend's available trades page
    Then I should see a message that no games are available for trade

  @view-trades-non-friend
  Scenario: Non-friends cannot access another user's trade page
    Given I am logged in as the view-trades viewer
    When I attempt to view a stranger's available trades
    Then access to the trade page should be denied

  @view-trades-unauthenticated
  Scenario: Unauthenticated users cannot access trade pages
    Given I am not logged in as any user
    When I attempt to view a user's trades without logging in
    Then I should be redirected to the login page
