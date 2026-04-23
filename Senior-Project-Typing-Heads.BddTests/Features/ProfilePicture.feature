@profile-picture
Feature: Profile Picture
  As a registered user
  I want to upload a profile picture
  So that my profile and account feel more personal

  @upload-valid-picture
  Scenario: User uploads a valid profile picture
    Given I am logged in as the profile picture test user
    And I am on my profile page
    When I upload a valid profile picture
    Then I should see my profile picture displayed on my profile

  @upload-invalid-type
  Scenario: User tries to upload an invalid file type
    Given I am logged in as the profile picture test user
    And I am on my profile page
    When I upload a file with an invalid type
    Then I should see a file type error message

  @upload-too-large
  Scenario: User tries to upload a file that is too large
    Given I am logged in as the profile picture test user
    And I am on my profile page
    When I upload a file that is too large
    Then I should see a file size error message
