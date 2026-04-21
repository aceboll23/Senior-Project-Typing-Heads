Feature: Sort Reviews by Rating (TYP-56)
  As a user browsing a game
  I want to sort the reviews by rating
  So that I can quickly see the highest or lowest-rated opinions

  Scenario: Sorting reviews by lowest rating first shows the 2-rated review on top
    Given I am logged in as a BDD sort reviews user
    When I navigate to the sort reviews game details page
    And I select "Rating: Low to High" from the review sort dropdown
    Then the first review shown has rating 2
    And the last review shown has rating 9
