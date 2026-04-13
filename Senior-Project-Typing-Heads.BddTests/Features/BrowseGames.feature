@browsegames
Feature: Browse games

  As a user
  I want to browse games without knowing exactly what I’m looking for
  So that I can discover games more easily

  Scenario: User opens the browse games page from the navbar
    Given I am on the home page
    When I click the "Games" link
    Then I should be taken to the browse games page
    And I should see a list of games from the database

  Scenario: User filters browse games by minimum rating
    Given I am on the home page
    When I click the "Games" link
    And I enter a minimum rating of "8.0"
    And I apply the browse filters
    Then I should be taken to the browse games page
    And I should see filtered browse game results