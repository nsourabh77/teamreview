using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web.Mvc;
using AutoMapper;
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
		public ActionResult Create(ReviewCreateEditModel reviewCreateModel) {
			var action = Request.Form["reviewAction"];
			if (action != null) {
				if (action == "AddCategory") {
					reviewCreateModel.AddedCategories.Add(new CategoryAddModel());
					return View(reviewCreateModel);
				}
				if (action == "AddPeer") {
					reviewCreateModel.AddedPeers.Add(new PeerAddModel());
					return View(reviewCreateModel);
				}
			}

			if (!ModelState.IsValid) {
				return View(reviewCreateModel);
			}

			// TODO: raise model state errors for duplicate email addresses

			var newReview = Mapper.Map<ReviewConfiguration>(reviewCreateModel);
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

			if (action != null && action == "Save and Start the Review") {
				return RedirectToAction("StartReview", new {id = newReview.ReviewId});
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

		[HttpPost]
		public ActionResult Edit(int id, ReviewCreateEditModel reviewEditModel) {
			var reviewFromDb = _db.ReviewConfigurations
				.Include("Categories")
				.Include("Peers")
				.Single(rev => rev.ReviewId == id);

			if (reviewFromDb == null) {
				return new HttpNotFoundResult("The review could not be found.");
			}

			var newModel = Mapper.Map<ReviewCreateEditModel>(reviewFromDb);
			var action = Request.Form["reviewAction"];
			if (action != null) {
				if (action == "AddCategory") {
					newModel.AddedCategories.Add(new CategoryAddModel());
				}
				else if (action == "AddPeer") {
					newModel.AddedPeers.Add(new PeerAddModel());
				}
				return View("Create", newModel);
			}

			if (!ModelState.IsValid) {
				return View("Create", newModel);
			}

			reviewFromDb.Name = reviewEditModel.Name;
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
			var numberOfPeersWithoutMe = reviewconfiguration.Peers.Count - 1;

			// my results
			foreach (var category in reviewconfiguration.Categories) {
				var categoryWithResults = new CategoryWithResults
				                          	{
				                          		CategoryName = category.Name,
				                          		CategoryDescription = category.Description
				                          	};
				categoryWithResults.MyRating =
					allAssessments.Where(
						a => a.ReviewCategory.CatId == category.CatId && a.ReviewedPeer.UserId == myId && a.Reviewer.UserId == myId).
						Single().Rating;

				if (numberOfPeersWithoutMe > 0) {
					categoryWithResults.PeerRating =
						allAssessments.Where(
							a => a.ReviewCategory.CatId == category.CatId && a.ReviewedPeer.UserId == myId && a.Reviewer.UserId != myId).
							Select(a => a.Rating).Sum()/numberOfPeersWithoutMe;
				}
				results.CategoriesWithMyResults.Add(categoryWithResults);
			}

			// my stacked results
			results.MyStackedRating = results.CategoriesWithMyResults.Select(c => c.MyRating).Sum();
			results.PeerStackedRating = results.CategoriesWithMyResults.Select(c => c.PeerRating).Sum();
			// everybodys results
			if (numberOfPeersWithoutMe > 0) {
				foreach (var category in reviewconfiguration.Categories) {
					var categoryWithPeersWithResults = new CategoryWithPeersWithResults
					                                   	{
					                                   		CategoryName = category.Name,
					                                   		CategoryDescription = category.Description
					                                   	};

					foreach (var peer in reviewconfiguration.Peers) {
						var peerWithResult = new PeerWithResult { PeerName = peer.UserName };
						peerWithResult.PeerRating =
							allAssessments.Where(
								a =>
								a.ReviewCategory.CatId == category.CatId && a.ReviewedPeer.UserId == peer.UserId &&
								a.Reviewer.UserId != peer.UserId).Select(a => a.Rating).Sum()/numberOfPeersWithoutMe;
						categoryWithPeersWithResults.PeersWithResult.Add(peerWithResult);
					}
					results.CategoriesWithPeersWithResults.Add(categoryWithPeersWithResults);
				}

				// everybodys stacked results
				foreach (var peer in reviewconfiguration.Peers) {
					var peerWithStackedRating = new PeerWithStackedRating { PeerName = peer.UserName };
					peerWithStackedRating.PeerStackedRating =
						results.CategoriesWithPeersWithResults.SelectMany(c => c.PeersWithResult).Where(p => p.PeerName == peer.UserName).
							Select(p => p.PeerRating).Sum();
					results.PeersWithStackedRatings.Add(peerWithStackedRating);
				}
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
			SendMailToPeers(reviewconfiguration.Peers, id);

			TempData["Message"] = "Review has been started and mails have been sent to peers";
			return RedirectToAction("Index");
		}

		private void SendMailToPeers(IEnumerable<UserProfile> peers, int id) {
			var credentials = new NetworkCredential("teamreview@teamaton.com", "TGqDYzt0ZnnbPMgzn9Hl");
			var smtpClient = new SmtpClient("smtp.teamaton.com")
			                 	{
			                 		UseDefaultCredentials = false,
			                 		Credentials = credentials
			                 	};

			foreach (var peer in peers) {
				var message = new MailMessage("teamreview@teamaton.com", peer.EmailAddress)
				              	{
				              		Subject = "Provide Review",
				              		Body = GetMailBody(peer.UserName, id)
				              	};

				smtpClient.Send(message);
			}
		}

		private string GetMailBody(string userName, int id) {
			return string.Format(
				@"Dear {0},

Please provide a review. Go to the following page:
http://teamreview.teamaton.com/Review/Provide/{1}
--
Your teamaton team",
				userName, id);
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