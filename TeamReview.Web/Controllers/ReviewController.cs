using System.Linq;
using System.Web.Mvc;
using TeamReview.Web.Models;

namespace TeamReview.Web.Controllers {
	[Authorize]
	public class ReviewController : Controller {
		private readonly ReviewsContext db = new ReviewsContext();

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
			object review;
			return View(TempData.TryGetValue("review", out review) ? review : new ReviewConfiguration());
		}

		//
		// POST: /Review/Create

		[HttpPost]
		public ActionResult Create(ReviewConfiguration reviewConfiguration) {
			if (Request.Form["reviewAction"] == "AddCategory") {
				reviewConfiguration.Categories.Add(new ReviewCategory());
			}
			else if (Request.Form["reviewAction"] == "AddPeer") {
				reviewConfiguration.Peers.Add(new UserProfile());
			}
			else if (ModelState.IsValid) {
				// TODO: check for duplicates, maybe user added himself or someone else twice
				reviewConfiguration.Peers.Add(db.UserProfiles.First(user => user.UserName == User.Identity.Name));
				db.ReviewConfigurations.Add(reviewConfiguration);
				db.SaveChanges();
				TempData["Message"] = "Review has been created";
				return RedirectToAction("Edit", new { id = reviewConfiguration.ReviewId });
			}

			TempData["review"] = reviewConfiguration;
			return RedirectToAction("Create");
			//return View(reviewConfiguration);
		}

		//public ActionResult AddCategory(ReviewConfiguration reviewConfiguration) {
		//    reviewConfiguration.Categories.Add(new ReviewCategory());
		//    TempData["review"] = reviewConfiguration;
		//    return RedirectToAction("Create");
		//    //return PartialView("AddCategory", new ReviewCategory());
		//}

		//
		// GET: /Review/Edit/5

		public ActionResult Edit(int id = 0) {
			var reviewconfiguration = db.ReviewConfigurations.Find(id);
			if (reviewconfiguration == null) {
				return HttpNotFound();
			}
			db.Entry(reviewconfiguration).Collection(c => c.Categories).Load();
			return View(reviewconfiguration);
		}

		//
		// POST: /Review/Edit/5

		[HttpPost]
		public ActionResult Edit(ReviewConfiguration reviewconfiguration) {
			if (ModelState.IsValid) {
				var dbReviewConfiguration = db.ReviewConfigurations.Find(reviewconfiguration.ReviewId);
				db.Entry(dbReviewConfiguration).Collection(c => c.Categories).Load();
				UpdateModel(dbReviewConfiguration);
				db.SaveChanges();
				TempData["Message"] = "Review has been saved";
				return RedirectToAction("Edit", new { id = reviewconfiguration.ReviewId });
			}
			return View(reviewconfiguration);
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
	}
}