@posts
Feature: Profile Posts
  As a registered user
  I want to post gaming updates on my profile
  So that my friends can see what I have been playing and accomplishing

  @create-post
  Scenario: Owner creates a post on their own profile
    Given I am logged in as the post test owner
    And I am on my own profile page
    When I enter post content "Just beat Catan for the first time!"
    And I click the Post button
    Then I should see "Just beat Catan for the first time!" on the profile page

  @view-post
  Scenario: Friend can view posts on another user's profile
    Given I am logged in as a friend of the post owner
    When I navigate to the post owner's profile
    Then I should see the seeded post content on the profile page

  @delete-post
  Scenario: Owner can delete their own post
    Given I am logged in as the post test owner
    And I navigate to my own profile page
    When I click the delete button on my post
    And I accept the delete confirmation
    Then the deleted post should not appear on the profile

  @empty-post
  Scenario: Owner cannot submit an empty post
    Given I am logged in as the post test owner
    And I am on my own profile page
    When I submit the post form with no content
    Then I should see a post content error on the profile page

  @no-create-form
  Scenario: Friend cannot see the post creation form on another user's profile
    Given I am logged in as a friend of the post owner
    When I navigate to the post owner's profile
    Then I should not see the post creation form

  @post-image
  Scenario: Owner creates a post with an image
    Given I am logged in as the post test owner
    And I am on my own profile page
    When I attach an image to the post
    And I click the Post button
    Then I should see the post image on the profile page

  @post-category
  Scenario: Owner creates a post with a category tag
    Given I am logged in as the post test owner
    And I am on my own profile page
    When I enter post content "Started a new campaign!"
    And I select the "Currently Playing" category
    And I click the Post button
    Then I should see the "Currently Playing" badge on the profile page

  @post-game
  Scenario: Owner creates a post linked to a game
    Given I am logged in as the post test owner
    And I am on my own profile page
    When I enter post content "Great session last night"
    And I select the seeded game from the game dropdown
    And I click the Post button
    Then I should see the seeded game name on the profile page

  @char-counter
  Scenario: Character counter updates as user types
    Given I am logged in as the post test owner
    And I am on my own profile page
    When I enter post content "Hello"
    Then the character counter should show 495
