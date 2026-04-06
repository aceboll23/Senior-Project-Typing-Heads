Feature: DeleteAccount
  As a registered user
  I want to delete my account
  So that I can remove my data from the platform

  Scenario: Registered user can delete their account
    Given I am logged in as "deletetest" with password "TestPassword1!"
    And I navigate to the settings page
    When I click the delete account link
    And I enter my current password "TestPassword1!" in the delete form
    And I click the permanently delete button
    Then I am redirected to the home page
    And I am no longer logged in