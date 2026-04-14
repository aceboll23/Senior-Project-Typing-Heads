Feature: Playgroup Combined Collection
  As a playgroup member
  I want to see the combined game collection of all members
  So that I know what games are available to play

  Scenario: Playgroup member can access the combined collection page
    Given I am a logged-in member of the collection playgroup
    When I navigate to the playgroup details page
    Then the View Combined Collection link is visible
    When I click the View Combined Collection link
    Then the combined collection page loads successfully

  Scenario: Combined collection displays games with correct owners
    Given I am a logged-in member of the collection playgroup
    When I navigate to the combined collection page directly
    Then at least one game is listed
    And the owner username is displayed next to the game

  Scenario: Non-member cannot access the combined collection page
    Given I am logged in as a non-member
    When I navigate to the combined collection page directly
    Then I see a not found page

  Scenario: Combined collection shows empty state when no members own games
    Given I am a logged-in member of the empty playgroup
    When I navigate to the empty playgroup collection page directly
    Then I see the empty collection message