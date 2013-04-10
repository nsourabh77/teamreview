﻿Feature: Provide Review
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
		And I have a started review with two categories
	When I visit the Provide review url
	Then I am on the "Provide review" page for my review

Scenario: Show "Provide review" button for started reviews
	Given I am logged in
		And I have a started review with two categories
		And I am on the "Dashboard" page
	When I click on the "Provide review" link of the review
	Then I am on the "Provide review" page for my review

Scenario: Show Provide Review
	Given I am logged in
		And I have a started review with two categories
	When I visit the Provide review url
	Then I see all categories
		And I have input options from 1 to 10 for each category

Scenario: Provide full Review
	Given I am logged in
		And I have a started review with two categories
	When I visit the Provide review url
	When I select 5 for each category
		And I save the review
	Then the feedback is saved with 5 for each category
	Then I am on the "Reviews"
		And I see the message "Review has been completed"

Scenario: Providing partial Review is not allowed
	Given I am logged in
		And I have a started review with two categories
	When I visit the Provide review url
		And I save the review
	Then the feedback is not saved
		And I see the message "Please fill out all categories"

#register for review

#login for review

