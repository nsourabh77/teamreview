Feature: Provide Review
	In order to give feedback
	As a user
	I want to provide a review

Scenario: Start the review
	Given I am logged in
		And I own a review with two peers
	When I start the review
	Then the review is active
		#And an email is sent to me and two peers containing a link to the "Provide review" page of my review
		And I see the message "Review has been started and mails have been sent to peers"

Scenario: Provide a review
	Given I am logged in
		And I have a started review
	When I visit the "Provide review" url
	Then I am on the "Provide review" page

Scenario: Show "Provide review" button for started reviews
	Given I am logged in
		And I have a started review
	When I visit the "Dashboard"
	And I click on "Provide review"
	Then I am on the "Provide review" page

Scenario: Show Provide Review
	Given I am logged in
		And I have a started review
	When I am on the "Provide review" page
	Then I see all categories
		And I have input options from 1 to 10 for each category

Scenario: Provide full Review
	Given I am logged in
		And I have a started review
	When I am on the "Provide review" page
	When I select 5 for each category
		And I save the review
	Then the feedback is saved
	Then I am on the "Dashboard"
		And I see the message "Review has been completed"

Scenario: Providing partial Review is not allowed
	Given I am logged in
		And I have a started review
	When I am on the "Provide review" page
		And I save the review
	Then the feedback is not saved
		And I see the message "Please fill out all categories"

#register for review

#login for review

