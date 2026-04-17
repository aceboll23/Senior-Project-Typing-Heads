@social-feed
Feature: Social Feed
  As a registered user
  I want to see a feed of my friends' recent posts
  So that I can stay updated on what they are playing and sharing

  @feed-has-posts
  Scenario: User sees friends' posts in their social feed
    Given I am logged in as the social feed viewer
    When I navigate to the social feed page
    Then I should see the friend's post content in the feed
    And I should see the friend's username in the feed

  @empty-feed
  Scenario: User sees an empty state message when friends have no posts
    Given I am logged in as the social feed friend
    When I navigate to the social feed page
    Then I should see the empty feed message

  @non-friend-excluded
  Scenario: Non-friend posts do not appear in the feed
    Given I am logged in as the social feed viewer
    When I navigate to the social feed page
    Then the stranger's post should not appear in the feed

  @feed-ordering
  Scenario: Posts appear in reverse chronological order
    Given I am logged in as the social feed viewer
    When I navigate to the social feed page
    Then the newer post should appear before the older post in the feed

  @nav-link
  Scenario: Social feed link appears in the authenticated navigation menu
    Given I am logged in as the social feed viewer
    Then I should see a Social link in the navigation
