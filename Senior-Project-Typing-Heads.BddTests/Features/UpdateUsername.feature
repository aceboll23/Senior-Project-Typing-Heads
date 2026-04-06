Feature: UpdateUsername
  As a registered user
  I want to update my username from the settings page
  So that I can keep my profile information current

  Scenario: Registered user can update their username from settings
    Given I am logged in as "settingstest" with password "TestPassword1!"
    And I navigate to the settings page
    When I clear the username field and type "settingstest_updated"
    And I enter my current password "TestPassword1!" in the settings form
    And I click the save changes button
    Then I see the success message
    And the navbar displays "settingstest_updated"