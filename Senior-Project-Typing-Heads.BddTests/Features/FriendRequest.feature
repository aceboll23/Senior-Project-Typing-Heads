Feature: FriendRequest
  As a registered user
  I want to send friend requests to other users
  So that I can connect with other players on BoredGamers

  Scenario: Registered user can send a friend request
    Given I am logged in as "friendsender" with password "TestPassword1!"
    And I navigate to the profile page of "friendreceiver"
    When I click the "Add As Friend" button
    Then the button changes to "Cancel Request"