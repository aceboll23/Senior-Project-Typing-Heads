Feature: Create Playgroup
  As a registered user
  I want to create a new playgroup
  So that I can organize game sessions with other players

  Scenario: Create Playgroup form exists for logged in user
    Given I am logged in as PersonThree
    When I navigate to the Create Playgroup page
    Then the Create Playgroup form is displayed

  Scenario: User can create a new playgroup
    Given I am logged in as PersonThree
    When I navigate to the Create Playgroup page
    When I fill in the Create Playgroup form with a unique name
    And I submit the Create Playgroup form
    Then I am redirected to the playgroup detail page
    And the created playgroup name is displayed
