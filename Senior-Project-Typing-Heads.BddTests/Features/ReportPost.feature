@report-post
Feature: Report Post
  As a registered user
  I want to report a post for inappropriate content
  So that admins can review flagged posts

  @flag-post
  Scenario: User reports a post on another user's profile
    Given I am logged in as a friend of the post owner
    When I navigate to the post owner's profile
    And I click the Report button on a post
    And I accept the report confirmation
    Then the Report button should change to "Reported"
    And the Report button should be disabled
