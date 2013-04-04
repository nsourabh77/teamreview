using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using AutoMapper;
using TeamReview.Web.Models;
using TeamReview.Web.ViewModels;

namespace TeamReview.Web.Controllers {
	[Authorize]
	public class ReviewController : Controller {
		private readonly DatabaseContext db = new DatabaseContext();

		//
		// GET: /Review/

		public ActionResult Index() {
			return View(db.ReviewConfigurations.ToList());
		}

		//
		// GET: /Review/Details/5

		public ActionResult Details(int id = 0) {
			var reviewconfiguration = db.ReviewConfigurations.Find(id);
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

			var newReview = Mapper.Map<ReviewConfiguration>(reviewCreateModel);
			db.ReviewConfigurations.Add(newReview);

			foreach (var cat in reviewCreateModel.AddedCategories.Select(Mapper.Map<ReviewCategory>)) {
				db.ReviewCategories.Add(cat);
				newReview.Categories.Add(cat);
			}

			foreach (var newPeer in reviewCreateModel.AddedPeers.Select(Mapper.Map<UserProfile>)) {
				var fromDb = db.UserProfiles.SingleOrDefault(user => user.UserName == newPeer.UserName);
				if (fromDb != null) {
					db.UserProfiles.Attach(fromDb);
					newReview.Peers.Add(fromDb);
				}
				else {
					db.UserProfiles.Add(newPeer);
					newReview.Peers.Add(newPeer);
				}
			}
			var loggedInUser = db.UserProfiles.FirstOrDefault(user => user.UserName == User.Identity.Name);
			if (loggedInUser != null) {
				db.UserProfiles.Attach(loggedInUser);
				newReview.Peers.Add(loggedInUser);
			}
			db.SaveChanges();

			TempData["Message"] = "Review has been created";

			return RedirectToAction("Edit", new { id = newReview.ReviewId });
		}

		//
		// GET: /Review/Edit/5

		public ActionResult Edit(int id) {
			object review;
			if (TempData.TryGetValue("review", out review)) {
				return View(review);
			}
			var reviewFromDb = db.ReviewConfigurations
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
			var reviewFromDb = db.ReviewConfigurations
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

			foreach (var cat in reviewEditModel.AddedCategories.Select(Mapper.Map<ReviewCategory>)) {
				db.ReviewCategories.Add(cat);
				reviewFromDb.Categories.Add(cat);
			}

			foreach (var newPeer in reviewEditModel.AddedPeers.Select(Mapper.Map<UserProfile>)) {
				var fromDb = db.UserProfiles.SingleOrDefault(user => user.UserName == newPeer.UserName);
				if (fromDb != null) {
					db.UserProfiles.Attach(fromDb);
					reviewFromDb.Peers.Add(fromDb);
				}
				else {
					db.UserProfiles.Add(newPeer);
					reviewFromDb.Peers.Add(newPeer);
				}
			}
			db.SaveChanges();

			TempData["Message"] = "Review has been saved";

			return RedirectToAction("Edit", new { id = reviewEditModel.Id });
		}

		//
		// POST: /Review/Edit/5

		[HttpPost]
		public ActionResult _Edit(int id, ReviewConfiguration reviewConfiguration) {
			if (reviewConfiguration.ReviewId != id) {
				return new HttpUnauthorizedResult("You must not change the ID of the review you're editing!");
			}
			if (Request.Form["reviewAction"] == "AddCategory") {
				reviewConfiguration.Categories.Add(new ReviewCategory());
			}
			else if (Request.Form["reviewAction"] == "AddPeer") {
				reviewConfiguration.Peers.Add(new UserProfile());
			}
			else if (ModelState.IsValid) {
				// Get original product from DB including category
				var fromDb = db.ReviewConfigurations
					.Include("Categories")
					.Include("Peers")
					.SingleOrDefault(rev => rev.ReviewId == id);

				// Update scalar properties of product
				db.Entry(fromDb).CurrentValues.SetValues(reviewConfiguration);

				// Update the Category reference if the CategoryID has been changed in the from
				//if (productFromForm.Category.CategoryID != fromDb.Category.CategoryID)
				//{
				//    db.Categories.Attach(productFromForm.Category);
				//    fromDb.Category = productFromForm.Category;
				//}

				db.SaveChanges();

				//RemovePeerDuplicates(reviewConfiguration);
				//var dbReviewConfiguration = db.ReviewConfigurations.Find(reviewConfiguration.ReviewId);
				//db.Entry(dbReviewConfiguration).Collection(c => c.Categories).Load();
				//db.Entry(dbReviewConfiguration).Collection(c => c.Peers).Load();
				//dbReviewConfiguration.Peers = reviewConfiguration.Peers;
				//dbReviewConfiguration.Categories = reviewConfiguration.Categories;
				//dbReviewConfiguration.Name = reviewConfiguration.Name;
				//UpdateModel(dbReviewConfiguration, null, null, new[] { "Peers" });
				//db.SaveChanges();
				//db.Entry(reviewConfiguration).State = EntityState.Modified;
				//reviewConfiguration.Categories.ForEach(c => db.Entry(c).State = EntityState.Modified);
				//reviewConfiguration.Peers.ForEach(p => db.Entry(p).State = EntityState.Modified);
				//try {
				//    db.SaveChanges();
				//}
				//catch (OptimisticConcurrencyException) {
				//    var objectContext = ((IObjectContextAdapter) db).ObjectContext;
				//    objectContext.Refresh(RefreshMode.ClientWins, db.ReviewConfigurations);
				//    db.SaveChanges();
				//}
				TempData["Message"] = "Review has been saved";
				return RedirectToAction("Edit", new { id = reviewConfiguration.ReviewId });
			}

			TempData["review"] = reviewConfiguration;
			return RedirectToAction("Edit");
		}

		//
		// GET: /Review/Delete/5

		public ActionResult Delete(int id = 0) {
			var reviewconfiguration = db.ReviewConfigurations.Find(id);
			if (reviewconfiguration == null) {
				return HttpNotFound();
			}
			return View(reviewconfiguration);
		}

		//
		// POST: /Review/Delete/5

		[HttpPost, ActionName("Delete")]
		public ActionResult DeleteConfirmed(int id) {
			var reviewconfiguration = db.ReviewConfigurations.Find(id);
			db.ReviewConfigurations.Remove(reviewconfiguration);
			db.SaveChanges();
			return RedirectToAction("Index");
		}

		protected override void Dispose(bool disposing) {
			db.Dispose();
			base.Dispose(disposing);
		}

		private void RemovePeerDuplicates(ReviewConfiguration reviewConfiguration) {
			for (var i = 0; i < reviewConfiguration.Peers.Count(); i++) {
				var peer = reviewConfiguration.Peers[i];
				var peerFromDb = db.UserProfiles.Where(p => p.EmailAddress == peer.EmailAddress).FirstOrDefault();
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