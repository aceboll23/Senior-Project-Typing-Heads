Feature: Delete Friend (TYP-234)
  As a logged-in user
  I want to remove a friend from my friends list
  So that I can manage my connections

  Scenario: User removes a friend from their friends list
    Given I am logged in as a BDD delete friend user
    When I navigate to my friends page
    And I click the Remove Friend button
    Then the friend no longer appears in my friends list
