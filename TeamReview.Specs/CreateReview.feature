Feature: Create review
	In order to run a review
	As a user
	I want to create a new review

Scenario: Create a review with 2 categories
	Given I am logged in
	When I create a new review
		And I fill in a review name
		And I add a category
		And I fill in a category name
		And I fill in a category description
		And I add another category
		And I fill in a category name
		And I fill in a category description
		And I save the review
	Then my new review was created
		And I am on the "Edit review" page for my review

Scenario: Invite new peer to my review
	Given I am logged in
		And I have a review
	When I edit my review
		And I invite a peer
		And I fill in the peer's name
		And I fill in the peer's email address
		And I invite that peer
		And no account exists for that peer's email address
	Then a new user with the given name and email address was created
		And an email was sent to that user containing a link to the "Give a review" page of my review

Scenario: Invite existing peer to my review
	Given I am logged in
		And I have a review
	When I edit my review
		And I invite a peer
		And I fill in the peer's name
		And I fill in the peer's email address
		And I invite that peer
		And an account exists for that peer's email address
	Then an email was sent to that user containing a link to the "Give a review" page of my review
