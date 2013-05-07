﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Mail;
using System.Web.Mvc;
using AutoMapper;
using TeamReview.Web.Filters;
using TeamReview.Web.Models;
using TeamReview.Web.ViewModels;

namespace TeamReview.Web.Controllers {
	[Authorize]
	public class ReviewController : Controller {
		private readonly DatabaseContext _db = new DatabaseContext();

		//
		// GET: /Review/

		public ActionResult Index() {
			var currentUserId = _db.UserProfiles.First(user => user.EmailAddress == User.Identity.Name).UserId;
			var reviewConfigurations = _db.ReviewConfigurations
				.Include("Feedback")
				.Include("Peers")
				.Where(r => r.Peers.Any(p => p.UserId == currentUserId))
				.ToList();
			var reviewViewModels = new List<ReviewViewModel>();
			foreach (var reviewConfiguration in reviewConfigurations) {
				var reviewViewModel = new ReviewViewModel
				                      	{ ReviewId = reviewConfiguration.ReviewId, Name = reviewConfiguration.Name };
				if (!reviewConfiguration.Active) {
					reviewViewModel.ActionStatus = ActionStatus.NotStarted;
				}
				else if (reviewConfiguration.Feedback.Count == reviewConfiguration.Peers.Count) {
					reviewViewModel.ActionStatus = ActionStatus.ShowResults;
				}
				else if (reviewConfiguration.Feedback.Any(f => f.Reviewer.UserId == currentUserId)) {
					reviewViewModel.ActionStatus = ActionStatus.WaitForReviews;
				}
				else {
					reviewViewModel.ActionStatus = ActionStatus.ProvideReview;
				}
				reviewViewModels.Add(reviewViewModel);
			}
			return View(reviewViewModels);
		}

		//
		// GET: /Review/Details/5

		public ActionResult Details(int id = 0) {
			var reviewconfiguration = _db.ReviewConfigurations.Find(id);
			if (reviewconfiguration == null) {
				return HttpNotFound();
			}
			return View(reviewconfiguration);
		}

		//
		// GET: /Review/Create

		[HttpGet]
		public ActionResult Create() {
			return View(new ReviewCreateEditModel());
		}

		//
		// POST: /Review/Create

		[HttpPost]
		[FormValueRequired("submit")]
		public ActionResult Create(ReviewCreateEditModel reviewCreateModel) {
			// manual model validation
			ValidateModel(reviewCreateModel);

			if (!ModelState.IsValid) {
				return View(reviewCreateModel);
			}

			// TODO: raise model state errors for duplicate email addresses

			var newReview = Mapper.Map<ReviewConfiguration>(reviewCreateModel);
			newReview.EnsureName();

			_db.ReviewConfigurations.Add(newReview);

			foreach (var cat in reviewCreateModel.AddedCategories.Select(Mapper.Map<ReviewCategory>)) {
				newReview.Categories.Add(cat);
			}

			foreach (var newPeer in reviewCreateModel.AddedPeers.Select(Mapper.Map<UserProfile>)) {
				var fromDb = _db.UserProfiles.SingleOrDefault(user => user.EmailAddress == newPeer.EmailAddress);
				newReview.Peers.Add(fromDb ?? newPeer);
			}

			var loggedInUser = _db.UserProfiles.FirstOrDefault(user => user.EmailAddress == User.Identity.Name);
			if (loggedInUser != null) {
				newReview.Peers.Add(loggedInUser);
			}

			_db.SaveChanges();

			var action = Request.Form["submit"];
			if (!string.IsNullOrEmpty(action)) {
				return RedirectToAction("StartReview", new { id = newReview.ReviewId });
			}

			TempData["Message"] = "Review has been created";

			return RedirectToAction("Index");
		}

		//
		// GET: /Review/Edit/5

		[HttpGet]
		public ActionResult Edit(int id) {
			object review;
			if (TempData.TryGetValue("review", out review)) {
				return View("Create", review);
			}
			var reviewFromDb = _db.ReviewConfigurations
				.Include("Categories")
				.Include("Peers")
				.SingleOrDefault(rev => rev.ReviewId == id);
			if (reviewFromDb == null) {
				return HttpNotFound("No review found with the given id.");
			}
			return View("Create", Mapper.Map<ReviewCreateEditModel>(reviewFromDb));
		}

		//
		// POST: /Review/Edit/5

		[HttpPost, ActionName("Edit")]
		[FormValueRequired("submit")]
		public ActionResult EditPost(int id) {
			var reviewFromDb = _db.ReviewConfigurations
				.Include("Categories")
				.Include("Peers")
				.Single(rev => rev.ReviewId == id);

			if (reviewFromDb == null) {
				return new HttpNotFoundResult("The review could not be found.");
			}

			var reviewEditModel = Mapper.Map<ReviewCreateEditModel>(reviewFromDb);
			// load form data into new model object
			UpdateModel(reviewEditModel);

			// manual model validation
			ValidateModel(reviewEditModel);

			if (!ModelState.IsValid) {
				return View("Create", reviewEditModel);
			}

			reviewFromDb.Name = reviewEditModel.Name;
			reviewFromDb.EnsureName();
			// use this if there are more properties to set
			// _db.Entry(reviewFromDb).CurrentValues.SetValues(reviewEditModel);

			foreach (var cat in reviewEditModel.AddedCategories.Select(Mapper.Map<ReviewCategory>)) {
				reviewFromDb.Categories.Add(cat);
			}

			foreach (var newPeer in reviewEditModel.AddedPeers.Select(Mapper.Map<UserProfile>)) {
				var fromDb = _db.UserProfiles.SingleOrDefault(user => user.EmailAddress == newPeer.EmailAddress);
				reviewFromDb.Peers.Add(fromDb ?? newPeer);
			}
			_db.SaveChanges();

			TempData["Message"] = "Review has been saved";

			return RedirectToAction("Edit", new { id });
		}

		[HttpPost, ActionName("Create")]
		[FormValueRequired("submit.add")]
		public ActionResult CreateExtension(ReviewCreateEditModel reviewCreateModel) {
			var action = Request.Form["submit.add"];
			if (action != null) {
				if (action == "addCategory") {
					reviewCreateModel.AddedCategories.Add(new CategoryAddModel());
					return View("Create", reviewCreateModel);
				}
				if (action == "addPeer") {
					reviewCreateModel.AddedPeers.Add(new PeerAddModel());
					return View("Create", reviewCreateModel);
				}
			}
			throw new ArgumentNullException("reviewAction", "The given form field must not be empty!");
		}

		[HttpPost, ActionName("Edit")]
		[FormValueRequired("submit.add")]
		public ActionResult EditExtension(int id) {
			var reviewFromDb = _db.ReviewConfigurations
				.Include("Categories")
				.Include("Peers")
				.Single(rev => rev.ReviewId == id);

			if (reviewFromDb == null) {
				return new HttpNotFoundResult("The review could not be found.");
			}

			var newModel = Mapper.Map<ReviewCreateEditModel>(reviewFromDb);

			return CreateExtension(newModel);
		}

		//
		// GET: /Review/Delete/5

		public ActionResult Delete(int id = 0) {
			var reviewconfiguration = _db.ReviewConfigurations.Find(id);
			if (reviewconfiguration == null) {
				return HttpNotFound();
			}
			return View(reviewconfiguration);
		}

		//
		// POST: /Review/Delete/5

		[HttpPost, ActionName("Delete")]
		public ActionResult DeleteConfirmed(int id) {
			var reviewconfiguration = _db.ReviewConfigurations.Find(id);
			_db.ReviewConfigurations.Remove(reviewconfiguration);
			_db.SaveChanges();
			return RedirectToAction("Index");
		}

		public ActionResult Provide(int id = 0) {
			var reviewconfiguration = _db.ReviewConfigurations.Find(id);
			if (reviewconfiguration == null) {
				return HttpNotFound();
			}
			_db.Entry(reviewconfiguration).Collection(c => c.Categories).Load();
			_db.Entry(reviewconfiguration).Collection(c => c.Peers).Load();

			var feedback = new FeedbackViewModel { ReviewId = id, ReviewName = reviewconfiguration.Name };

			foreach (var reviewCategory in reviewconfiguration.Categories) {
				var categoryWithPeersAndRatings = new CategoryWithPeersAndRatings();
				categoryWithPeersAndRatings.Category = Mapper.Map<CategoryShowModel>(reviewCategory);
				foreach (var peer in reviewconfiguration.Peers) {
					var peerWithRating = new PeerWithRating { Peer = Mapper.Map<PeerShowModel>(peer), Rating = -1 };
					categoryWithPeersAndRatings.PeersWithRatings.Add(peerWithRating);
				}
				feedback.CategoriesWithPeersAndRatings.Add(categoryWithPeersAndRatings);
			}
			return View(feedback);
		}

		[HttpPost]
		public ActionResult Provide(FeedbackViewModel feedback) {
			if (
				feedback.CategoriesWithPeersAndRatings.SelectMany(c => c.PeersWithRatings.Select(p => p.Rating)).Any(
					a => a < 1 || a > 10)) {
				TempData["Message"] = "Please fill out all ratings";
				return View(feedback);
			}

			var reviewFeedback = new ReviewFeedback
			                     	{
			                     		Reviewer = _db.UserProfiles.FirstOrDefault(user => user.EmailAddress == User.Identity.Name)
			                     	};

			var reviewconfiguration = _db.ReviewConfigurations.Find(feedback.ReviewId);
			_db.Entry(reviewconfiguration).Collection(c => c.Feedback).Load();

			foreach (var categoriesWithPeersAndRating in feedback.CategoriesWithPeersAndRatings) {
				foreach (var peersWithRating in categoriesWithPeersAndRating.PeersWithRatings) {
					var assessment = new Assessment
					                 	{
					                 		Rating = peersWithRating.Rating,
					                 		Reviewer = reviewFeedback.Reviewer,
					                 		ReviewCategory = _db.ReviewCategories.Find(categoriesWithPeersAndRating.Category.CatId),
					                 		ReviewedPeer = _db.UserProfiles.Find(peersWithRating.Peer.UserId)
					                 	};
					reviewFeedback.Assessments.Add(assessment);
				}
			}

			reviewconfiguration.Feedback.Add(reviewFeedback);
			_db.SaveChanges();

			TempData["Message"] = "Review has been completed";
			return RedirectToAction("Index");
		}

		public ActionResult Results(int id = 0) {
			var reviewconfiguration = _db.ReviewConfigurations.Find(id);
			if (reviewconfiguration == null) {
				return HttpNotFound();
			}

			var results = new ResultViewModel { ReviewName = reviewconfiguration.Name };
			var myId = _db.UserProfiles.FirstOrDefault(user => user.EmailAddress == User.Identity.Name).UserId;

			_db.Entry(reviewconfiguration).Collection(c => c.Feedback).Load();
			_db.Entry(reviewconfiguration).Collection(c => c.Categories).Load();
			_db.Entry(reviewconfiguration).Collection(c => c.Peers).Load();
			foreach (var reviewFeedback in reviewconfiguration.Feedback) {
				_db.Entry(reviewFeedback).Collection(f => f.Assessments).Load();
			}

			var allAssessments = reviewconfiguration.Feedback.SelectMany(f => f.Assessments);
			var reviewers = reviewconfiguration.Feedback.Select(f => f.Reviewer);
			var numberOfReviewersWithoutMe = reviewers.Where(r => r.UserId != myId).Count();

			// my results
			foreach (var category in reviewconfiguration.Categories) {
				var categoryWithResults = new CategoryWithResults
				                          	{
				                          		CategoryName = category.Name,
				                          		CategoryDescription = category.Description
				                          	};
				var cat = category;
				var myAssessment = allAssessments.Where(
					a => a.ReviewCategory.CatId == cat.CatId && a.ReviewedPeer.UserId == myId && a.Reviewer.UserId == myId).
					SingleOrDefault();
				categoryWithResults.MyRating = myAssessment != null ? myAssessment.Rating : 0;

				if (numberOfReviewersWithoutMe > 0) {
					categoryWithResults.PeerRating =
						allAssessments.Where(
							a => a.ReviewCategory.CatId == cat.CatId && a.ReviewedPeer.UserId == myId && a.Reviewer.UserId != myId).
							Select(a => a.Rating).Sum()/(decimal) numberOfReviewersWithoutMe;
				}
				results.CategoriesWithMyResults.Add(categoryWithResults);
			}

			// my stacked results
			results.MyStackedRating = results.CategoriesWithMyResults.Select(c => c.MyRating).Sum();
			results.PeerStackedRating = results.CategoriesWithMyResults.Select(c => c.PeerRating).Sum();

			// everybodys results
			var categories = reviewconfiguration.Categories.Select(cat => cat.Name);
			results.CategoryPeerRatings = new CategoryPeerRatings
			                              	{
			                              		Categories = categories,
			                              		PeersWithRatings = new List<PeerWithRatings>()
			                              	};
			foreach (var peer in reviewconfiguration.Peers) {
				var pee = peer;
				var numberOfReviewersWithoutPeer = reviewers.Where(r => r.UserId != pee.UserId).Count();
				var peerWithRatings = new PeerWithRatings
				                      	{
				                      		PeerName = peer.UserName,
				                      		Ratings = new List<float>()
				                      	};
				results.CategoryPeerRatings.PeersWithRatings.Add(peerWithRatings);
				foreach (var category in reviewconfiguration.Categories) {
					var cat = category;
					peerWithRatings.Ratings.Add(numberOfReviewersWithoutPeer > 0
					                            	? allAssessments.Where(
					                            		a =>
					                            		a.ReviewCategory.CatId == cat.CatId && a.ReviewedPeer.UserId == pee.UserId &&
					                            		a.Reviewer.UserId != pee.UserId).Sum(a => a.Rating)/
					                            	  (float) numberOfReviewersWithoutPeer
					                            	: 0);
				}
			}

			var peerStackedRatings =
				reviewconfiguration.Peers.Select(peer => new PeerIdWithStackedRating { PeerId = peer.UserId, StackedRating = 0 }).
					ToList();
			foreach (var category in reviewconfiguration.Categories) {
				var categoryWithPeersWithResults = new CategoryWithPeersWithResults
				                                   	{
				                                   		CategoryName = category.Name,
				                                   		CategoryDescription = category.Description
				                                   	};

				foreach (var peer in reviewconfiguration.Peers) {
					var numberOfReviewersWithoutPeer = reviewers.Where(r => r.UserId != peer.UserId).Count();
					var peerWithResult = new PeerWithResult { PeerName = peer.UserName };
					if (numberOfReviewersWithoutPeer > 0) {
						peerWithResult.PeerRating =
							allAssessments.Where(
								a =>
								a.ReviewCategory.CatId == category.CatId && a.ReviewedPeer.UserId == peer.UserId &&
								a.Reviewer.UserId != peer.UserId).Select(a => a.Rating).Sum()/(decimal) numberOfReviewersWithoutPeer;
					}
					else {
						peerWithResult.PeerRating = 0;
					}
					categoryWithPeersWithResults.PeersWithResult.Add(peerWithResult);
					peerStackedRatings.Find(p => p.PeerId == peer.UserId).StackedRating += peerWithResult.PeerRating;
				}
				results.CategoriesWithPeersWithResults.Add(categoryWithPeersWithResults);
			}

			// everybodys stacked results
			foreach (var peer in reviewconfiguration.Peers) {
				var peerWithStackedRating = new PeerWithStackedRating
				                            	{
				                            		PeerName = peer.UserName,
				                            		PeerStackedRating =
				                            			peerStackedRatings.First(p => p.PeerId == peer.UserId).StackedRating
				                            	};
				results.PeersWithStackedRatings.Add(peerWithStackedRating);
			}

			results.RatingsForPeersPerCategory = new List<decimal[]>();
			foreach (var category in reviewconfiguration.Categories) {
				var cat = category;
				results.RatingsForPeersPerCategory.Add(
					reviewconfiguration.Peers
						.Select(peer => reviewconfiguration.Feedback.GetRatingForPeerForCategory(peer, cat))
						.ToArray());
			}

			return View(results);
		}

		public ActionResult StartReview(int id = 0) {
			var reviewconfiguration = _db.ReviewConfigurations.Include("Peers").Single(rc => rc.ReviewId == id);
			if (reviewconfiguration == null) {
				return HttpNotFound();
			}
			reviewconfiguration.Active = true;
			_db.SaveChanges();

			// TODO: send mail asynchronously from background task
			var owner = _db.UserProfiles.Single(user => user.EmailAddress == User.Identity.Name);
			SendMailToPeers(reviewconfiguration.Peers, id, owner);

			TempData["Message"] = "Review has been started and mails have been sent to peers";
			return RedirectToAction("Index");
		}

		private static void SendMailToPeers(IEnumerable<UserProfile> peers, int reviewId, UserProfile owner) {
			var credentials = new NetworkCredential("teamreview@teamaton.com", "TGqDYzt0ZnnbPMgzn9Hl");
			var smtpClient = new SmtpClient("mail.teamaton.com")
			                 	{
			                 		UseDefaultCredentials = false,
			                 		Credentials = credentials
			                 	};

			foreach (var peer in peers) {
				var message = new MailMessage("teamreview@teamaton.com", peer.EmailAddress)
				              	{
				              		Subject = "Provide Review",
				              		Body = GetMailBody(peer.UserName, reviewId, owner)
				              	};

				smtpClient.Send(message);
			}
		}

		private static string GetMailBody(string userName, int reviewId, UserProfile owner) {
			return string.Format(
				@"Hi there, {0},

you have been invited by {2} ({3}) to provide a review.

This helps improve your team's and your own performance.                

Please visit the following link to provide the review:

http://teamreview.teamaton.com/Review/Provide/{1}

If you would like to find out more about TeamReview, feel free to visit http://www.teamreview.net/.

In case you have any questions, just reply to this email and we will get in touch with you as soon as possible.


Thank you for your time and cheers,

Andrej - Masterchief Head of Design of TeamReview.net
",
				userName, reviewId, owner.UserName, owner.EmailAddress);
		}

		protected override void Dispose(bool disposing) {
			_db.Dispose();
			base.Dispose(disposing);
		}

		private void RemovePeerDuplicates(ReviewConfiguration reviewConfiguration) {
			for (var i = 0; i < reviewConfiguration.Peers.Count(); i++) {
				var peer = reviewConfiguration.Peers[i];
				var peerFromDb = _db.UserProfiles.Where(p => p.EmailAddress == peer.EmailAddress).FirstOrDefault();
				if (peerFromDb != null) {
					reviewConfiguration.Peers[i] = peerFromDb;
				}
			}
			reviewConfiguration.Peers = reviewConfiguration.Peers.Distinct(new UserProfileComparer()).ToList();
		}

		private void ValidateModel(ReviewCreateEditModel reviewCreateModel) {
			// 1. Remove empty entries from categories and peers
			for (var index = 0; index < reviewCreateModel.AddedCategories.Count; index++) {
				var category = reviewCreateModel.AddedCategories[index];
				if (string.IsNullOrWhiteSpace(category.Name) && string.IsNullOrWhiteSpace(category.Description)) {
					reviewCreateModel.AddedCategories.Remove(category);
				}
			}
			for (var index = 0; index < reviewCreateModel.AddedPeers.Count; index++) {
				var peer = reviewCreateModel.AddedPeers[index];
				if (string.IsNullOrWhiteSpace(peer.UserName) && string.IsNullOrWhiteSpace(peer.EmailAddress)) {
					reviewCreateModel.AddedPeers.Remove(peer);
				}
			}
			// 2. Check for categories with only a description
			for (var i = 0; i < reviewCreateModel.AddedCategories.Count; i++) {
				var index = i;
				var category = reviewCreateModel.AddedCategories[index];
				if (string.IsNullOrWhiteSpace(category.Name)) {
					Expression<Func<ReviewCreateEditModel, string>> expression = x => x.AddedCategories[index].Name;
					var key = ExpressionHelper.GetExpressionText(expression);
					ModelState.AddModelError(key, "Please give your category a name.");
				}
			}
			// 3. Check for peers without one of the fields
			for (var i = 0; i < reviewCreateModel.AddedPeers.Count; i++) {
				var index = i;
				var peer = reviewCreateModel.AddedPeers[index];
				if (string.IsNullOrWhiteSpace(peer.UserName)) {
					Expression<Func<ReviewCreateEditModel, string>> expression = x => x.AddedPeers[index].UserName;
					var key = ExpressionHelper.GetExpressionText(expression);
					ModelState.AddModelError(key, "Please enter your peer's name.");
				}
				else if (string.IsNullOrWhiteSpace(peer.EmailAddress)) {
					Expression<Func<ReviewCreateEditModel, string>> expression = x => x.AddedPeers[index].EmailAddress;
					var key = ExpressionHelper.GetExpressionText(expression);
					ModelState.AddModelError(key, "Please enter your peer's email address.");
				}
			}
		}
	}

	public class PeerIdWithStackedRating {
		public int PeerId;
		public decimal StackedRating;
	}

	public class UserProfileComparer : IEqualityComparer<UserProfile> {
		#region IEqualityComparer<UserProfile> Members

		public bool Equals(UserProfile x, UserProfile y) {
			return x.EmailAddress == y.EmailAddress;
		}

		public int GetHashCode(UserProfile userProfile) {
			return userProfile.EmailAddress.GetHashCode();
		}

		#endregion
	}
}