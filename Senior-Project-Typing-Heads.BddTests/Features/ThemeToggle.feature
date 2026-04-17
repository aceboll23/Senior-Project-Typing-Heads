Feature: Theme Toggle
  As a user
  I want to switch between dark and light themes
  So that I can use the app comfortably in different lighting conditions

  Scenario: User can switch to light theme
    Given I am on the home page
    When I click the theme toggle button
    Then the page is in light theme

  Scenario: User can switch back to dark theme
    Given I am on the home page
    When I click the theme toggle button
    And I click the theme toggle button again
    Then the page is in dark theme

  Scenario: Theme preference persists after page reload
    Given I am on the home page
    When I click the theme toggle button
    And I reload the page
    Then the page is in light theme
