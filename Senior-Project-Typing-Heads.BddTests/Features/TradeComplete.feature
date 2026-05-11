Feature: Trade Complete — Transfer Game Ownership
  As a registered user
  I want to transfer a game I traded to another user
  So that both collections stay accurate after a trade

@transfer-button-visible
Scenario: Transfer button appears on tradeable games in collection
  Given I am logged in as the trade-complete sender
  When I navigate to my collection page
  Then I should see a Transfer Game button for the tradeable game

@transfer-initiates
Scenario: Sender initiates transfer and game is removed from their collection
  Given I am logged in as the trade-complete sender
  When I initiate a transfer of the tradeable game to the receiver
  Then the tradeable game should no longer appear in my collection
  And I should see a transfer initiated success message

@transfer-receiver-sees-pending
Scenario: Receiver sees pending transfer section on their collection page
  Given I am logged in as the trade-complete receiver
  When I navigate to my collection page
  Then I should see the pending transfers section
  And I should see the pending game in the pending transfers section

@transfer-receiver-accepts
Scenario: Receiver accepts transfer and game is added to their collection
  Given I am logged in as the trade-complete receiver
  When I accept the pending game transfer
  Then the pending game should appear in my collection

@transfer-receiver-declines
Scenario: Receiver declines transfer and game is gone from both collections
  Given I am logged in as the trade-complete receiver
  When I decline the pending game transfer
  Then the pending game should not appear in my collection

@transfer-cancel-confirmation
Scenario: Sender cancels at the confirmation screen and game stays in their collection
  Given I am logged in as the trade-complete sender
  When I navigate to the transfer confirmation page for the tradeable game and the receiver
  And I cancel the transfer
  Then the tradeable game should still appear in my collection

@transfer-duplicate-rejected
Scenario: Transfer to user who already owns the game is rejected
  Given I am logged in as the trade-complete sender
  When I try to transfer the tradeable game to the user who already owns it
  Then I should see an error that the receiving user already has the game
  And the tradeable game should still appear in my collection
