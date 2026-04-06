Feature: Reviews
  As a registered user
  I want to create, edit, and delete my reviews
  So that I can manage my opinions on games

  Scenario: Registered user submits a valid review
    Given I am logged in as a registered user
    And I am on a game details page for a game I have not reviewed
    When I select a rating of 8
    And I enter review text "Great game with lots of replay value."
    And I submit the review form
    Then I should see the message "Review submitted!"
    And I should see my review text "Great game with lots of replay value."

  Scenario: Registered user cannot submit a review with blank text
    Given I am logged in as a registered user
    And I am on a game details page for a game I have not reviewed
    When I select a rating of 7
    And I leave the review text blank
    And I submit the review form
    Then the review form should still be visible
    And I should not see my review text "This review should not be created."

  Scenario: Registered user edits their own review
    Given I am logged in as a registered user
    And I am on a game details page for a game I have already reviewed
    When I click the Edit review action
    And I change the rating to 9
    And I change the review text to "Updated review text."
    And I save my review changes
    Then I should see the message "Review updated!"
    And I should see my review text "Updated review text."

  Scenario: Registered user deletes their own review
    Given I am logged in as a registered user
    And I am on a game details page for a game I have already reviewed
    When I click the Delete review action
    And I accept the delete confirmation
    Then I should see the message "Review deleted!"
    And I should not see my deleted review text