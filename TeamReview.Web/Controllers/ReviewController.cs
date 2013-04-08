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
			return View(_db.ReviewConfigurations.ToList());
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

		public ActionResult Create() {
			return View(new ReviewCreateModel());
		}

		//
		// POST: /Review/Create

		[HttpPost]
		public ActionResult Create(ReviewCreateModel reviewCreateModel) {
			var action = Request.Form["reviewAction"];
			if (action != null) {
				if (action == "AddCategory") {
					reviewCreateModel.AddedCategories.Add(new CategoryAddModel());
				}
				else if (action == "AddPeer") {
					reviewCreateModel.AddedPeers.Add(new PeerAddModel());
				}
				return View(reviewCreateModel);
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

			var loggedInUser = _db.UserProfiles.FirstOrDefault(user => user.UserName == User.Identity.Name);
			if (loggedInUser != null) {
				newReview.Peers.Add(loggedInUser);
			}
			_db.SaveChanges();

			TempData["Message"] = "Review has been created";

			return RedirectToAction("Edit", new {id = newReview.ReviewId});
		}

		//
		// GET: /Review/Edit/5

		public ActionResult Edit(int id) {
			object review;
			if (TempData.TryGetValue("review", out review)) {
				return View(review);
			}
			var reviewFromDb = _db.ReviewConfigurations
				.Include("Categories")
				.Include("Peers")
				.SingleOrDefault(rev => rev.ReviewId == id);
			if (reviewFromDb == null) {
				return HttpNotFound("No review found with the given id.");
			}
			return View(Mapper.Map<ReviewEditModel>(reviewFromDb));
		}

		//
		// POST: /Review/Edit/5

		[HttpPost]
		public ActionResult Edit(ReviewEditModel reviewEditModel) {
			var reviewFromDb = _db.ReviewConfigurations
				.Include("Categories")
				.Include("Peers")
				.Single(rev => rev.ReviewId == reviewEditModel.Id);

			if (reviewFromDb == null) {
				return new HttpNotFoundResult("The review could not be found.");
			}

			var newModel = Mapper.Map<ReviewEditModel>(reviewFromDb);
			var action = Request.Form["reviewAction"];
			if (action != null) {
				if (action == "AddCategory") {
					newModel.AddedCategories.Add(new CategoryAddModel());
				}
				else if (action == "AddPeer") {
					newModel.AddedPeers.Add(new PeerAddModel());
				}
				return View(newModel);
			}

			if (!ModelState.IsValid) {
				return View(newModel);
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

			return RedirectToAction("Edit", new {id = reviewEditModel.Id});
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

		public ActionResult StartReview(int id = 0) {
			var reviewconfiguration = _db.ReviewConfigurations.Include("Peers").Single(rc => rc.ReviewId == id);
			if (reviewconfiguration == null) {
				return HttpNotFound();
			}
			reviewconfiguration.Active = true;
			_db.SaveChanges();

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