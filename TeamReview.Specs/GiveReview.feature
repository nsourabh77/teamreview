Feature: Give Review
	In order to give feedback
	As a user
	I want to give a review

Scenario: Start the review
	Given I am logged in
		And I own a review with two peers
	When I start the review
	Then an email is sent to me and two peers containing a link to the "Give a review" page of my review

Scenario: Give a review
	Given I am logged in
		And I have a started review
	When I visit the "Give a review" url
	Then I am on the "Give review" page

#show give review button on dashboard

#register for review

#login for review

#fill in review