Feature: ShowReviewResults
	In order to get feedback
	As a peer
	I want to see the results of the review

Scenario: Show "Waiting for reviews" for started, provided review if not all peers have provided review
	Given I am logged in
		And I have a started review with 2 categories and 1 additional peers
		And I have provided review
		And I am on the "Dashboard" page
	Then I see "Waiting for reviews" for my review

Scenario: Show "Show results" button for finished reviews
	Given I am logged in
		And I have a started review with 2 categories and 2 additional peers
		And all peers have provided the review
		And I am on the "Dashboard" page
	When I click on the "Show results" link of the review
	Then I am on the "Results" page for my review

Scenario: Show individual results
	Given I am logged in
		And I have a started review with 2 categories and 2 additional peers
		And all peers have provided the review
		And I am on the "Results" page
	Then for each category I see the peer rating of me (average rating of all peers except mine) and my rating of me
		And I see the stacked rating of me (sum of ratings of all categories)

Scenario: Show team results
	Given I am logged in
		And I have a started review with 2 categories and 2 additional peers
		And all peers have provided the review
		And I am on the "Results" page
	Then for each category and each peer I see their peer rating (average rating of all peers except his/hers)
		And I see the stacked rating of each peer (sum of ratings of all categories)

#done! :)	