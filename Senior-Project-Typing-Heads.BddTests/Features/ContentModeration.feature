Feature: Content moderation
  As a community member
  I want inappropriate content to be blocked from posts and reviews
  So that the community stays welcoming and respectful

  Scenario: User cannot create a profile post with inappropriate content
    Given I am logged in as the post test owner
    And I am on my own profile page
    When I submit a profile post with clearly inappropriate content
    Then I should see a content moderation error message
    And the inappropriate post should not appear on the profile

  Scenario: User can create a profile post with clean content
    Given I am logged in as the post test owner
    And I am on my own profile page
    When I submit a profile post with clean content
    Then the post should appear on the profile