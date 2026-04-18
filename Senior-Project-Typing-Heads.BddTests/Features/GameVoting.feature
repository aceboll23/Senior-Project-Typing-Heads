Feature: Game Voting
  As a playgroup member
  I want to vote on which game to play at a game night
  So that the group can collectively decide

  Scenario: Event creator can open voting
    Given I am a logged-in event creator for voting
    When I navigate to the voting event details page
    And I click the Open Voting button
    Then voting is shown as open

  Scenario: Playgroup member can submit rankings
    Given I am a logged-in event member for voting
    And voting is already open for the event
    When I navigate to the voting event details page
    And I enter a ranking of 1 for the first game
    And I submit my rankings
    Then my rankings are saved successfully

  Scenario: Creator can close voting and see results
    Given I am a logged-in event creator for voting
    And voting is already open for the event
    When I navigate to the voting event details page
    And I click the Close Voting button
    Then voting results are displayed
    And the winning game is highlighted