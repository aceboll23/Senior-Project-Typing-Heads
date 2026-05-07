Feature: All Friends' Available Trades
  As a logged-in user
  I want to see all of my friends' available trades in one place
  So I can find games I am interested in trading for

@all-friend-trades-nav
Scenario: Friends Trades link appears in the navigation dropdown
  Given I am logged in as the all-friend-trades viewer
  Then I should see a Friends Trades link in the nav

@all-friend-trades-shows-games
Scenario: User sees tradeable games from all accepted friends
  Given I am logged in as the all-friend-trades viewer
  When I navigate to the Friends Trades page
  Then I should see friend1's tradeable game on the page
  And I should see friend2's tradeable game on the page

@all-friend-trades-excludes-non-tradeable
Scenario: Non-tradeable games from friends are not shown
  Given I am logged in as the all-friend-trades viewer
  When I navigate to the Friends Trades page
  Then I should not see the non-tradeable game on the all-trades page

@all-friend-trades-message-button
Scenario: Each game card has a message button for the owning friend
  Given I am logged in as the all-friend-trades viewer
  When I navigate to the Friends Trades page
  Then I should see a message button for friend1's game

@all-friend-trades-unauthenticated
Scenario: Unauthenticated user is redirected to login
  Given I am not logged in as any user
  When I navigate to the all friend trades page directly
  Then I should be redirected to the login page
