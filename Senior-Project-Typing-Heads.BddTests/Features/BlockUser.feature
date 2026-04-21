@block
Feature: Block User
  As a registered user
  I want to block another user
  So that I can prevent unwanted interaction

  @block-from-profile
  Scenario: User blocks another user from their profile page
    Given I am logged in as the block test blocker with a friendship
    And I am on the target user's profile page
    When I click the Block User button
    Then I should see a block success message

  @blocked-profile-hidden
  Scenario: Blocked user cannot view the blocker's profile
    Given I am logged in as the block test target who has been blocked
    When I navigate to the blocker's profile
    Then I should see a profile not available message

  @blocked-posts-excluded
  Scenario: Blocked user's posts do not appear in the blocker's social feed
    Given I am logged in as the block test blocker with a pre-existing block
    When I navigate to the social feed page
    Then I should not see the blocked user's post in the feed

  @block-removes-friendship
  Scenario: Blocking a friend removes the friendship
    Given I am logged in as the block test blocker with a friendship
    And I am on the target user's profile page
    When I click the Block User button
    Then the target user should no longer appear in my friends list

  @view-blocked-list
  Scenario: Blocker can view their blocked users list in settings
    Given I am logged in as the block test blocker with a pre-existing block
    When I navigate to the blocked users page
    Then I should see the blocked user in the list

  @unblock-user
  Scenario: Blocker can unblock a user from the blocked users page
    Given I am logged in as the block test blocker with a pre-existing block
    When I navigate to the blocked users page
    And I click the Unblock button for the target user
    Then the target user should no longer appear in the blocked list
