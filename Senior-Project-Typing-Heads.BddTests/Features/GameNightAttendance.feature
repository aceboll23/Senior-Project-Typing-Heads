@gamenightattendance
Feature: Game night attendance

    As a playgroup member
    I want to mark myself as going to a game night
    So that other members know I plan to attend

    Scenario: Playgroup member marks themselves as going
        Given I am a logged-in member of the playgroup for the event
        And a game night event exists for my playgroup
        When I mark my attendance as "Going"
        Then my attendance for that event should be saved as "Going"