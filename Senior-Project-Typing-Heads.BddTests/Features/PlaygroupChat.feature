Feature: Playgroup Group Chat
  As a playgroup member
  I want to send and receive messages in a group chat
  So that I can communicate with my playgroup

  Scenario: Member can open the group chat page
    Given I am logged in as a BDD chat member
    When I navigate to the group chat page
    Then the chat page is displayed with the playgroup name

  Scenario: Non-member cannot access the group chat
    Given I am logged in as a BDD chat outsider
    When I navigate to the group chat page
    Then I receive a not found response

  Scenario: Member sends a message and it appears in the chat
    Given I am logged in as a BDD chat member
    When I navigate to the group chat page
    And I send the message "Hello from BDD test!"
    Then the message "Hello from BDD test!" is visible in the chat

  Scenario: Member cannot send an empty message
    Given I am logged in as a BDD chat member
    When I navigate to the group chat page
    And I attempt to send an empty message
    Then the send button remains inactive

  Scenario: System message appears when a member leaves the playgroup
    Given I am logged in as a BDD chat member
    When I leave the playgroup
    And I am logged in as the BDD chat owner
    And I navigate to the group chat page
    Then a system message about the member leaving is visible
