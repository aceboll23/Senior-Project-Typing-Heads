Feature: Delete from Collection (TYP-243)
  As a logged-in user
  I want to remove a game from my owned collection
  So that my collection accurately reflects games I still have

  Scenario: User removes a game from their collection
    Given I am logged in as a BDD delete collection user
    When I navigate to my collection page
    And I click the Remove from Collection button
    Then the game no longer appears in my collection
