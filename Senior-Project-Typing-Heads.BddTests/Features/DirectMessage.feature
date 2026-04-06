Feature: DirectMessage
  As a registered user
  I want to send direct messages to my friends
  So that I can communicate with other players on BoredGamers

  Scenario: Registered user can send a direct message to a friend
    Given I am logged in as "testuser" with password "TestPassword1!"
    And I navigate to the profile page of "friendtarget"
    When I click the "Message" button link
    And I type "Hello from the BDD test!" into the message input
    And I click the send button
    Then the message "Hello from the BDD test!" appears in the conversation