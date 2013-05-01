Feature: Register and login
	In order to create a review
	As a user
	I want to register an account

Scenario: Register using Google account
	Given I own a Google account
		And I am not logged into TeamReview
	When I register a new account
		And I use my Google account
		And I fill in a user name
		And I finish registering
	Then a new account was created with my Google address
		And I am logged in
		And I am on the "Dashboard"

Scenario: Login as existing user using Google account
	Given I have an account at TeamReview
		And I am not logged into TeamReview
	When I log in using my Google account
	Then I am logged in
		And I am on the "Dashboard"

Scenario: Login as non-existing user using Google account
	Given I own a Google account
		And I don't have an account at TeamReview
	When I register using my Google account
		And I finish registering
	Then a new account was created with my Google address
		And I am logged in
		And I am on the "Dashboard"

Scenario: Log out of TeamReview
	Given I am logged into TeamReview
	When I log out
	Then I am not logged into TeamReview
		And I am on the login page