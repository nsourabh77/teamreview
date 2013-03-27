using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TeamReview.Web.Models;

namespace TeamReview.Web.Controllers
{
	[Authorize]
	public class ReviewController : Controller
	{
		private ReviewsContext db = new ReviewsContext();

		//
		// GET: /Review/

		public ActionResult Index()
		{
			return View(db.ReviewConfigurations.ToList());
		}

		//
		// GET: /Review/Details/5

		public ActionResult Details(int id = 0)
		{
			ReviewConfiguration reviewconfiguration = db.ReviewConfigurations.Find(id);
			if (reviewconfiguration == null)
			{
				return HttpNotFound();
			}
			return View(reviewconfiguration);
		}

		//
		// GET: /Review/Create
		public ActionResult Create()
		{
			return View();
		}

		//
		// POST: /Review/Create

		[HttpPost]
		public ActionResult Create(ReviewConfiguration reviewconfiguration)
		{
			if (ModelState.IsValid)
			{
				reviewconfiguration.Peers.Add(db.UserProfiles.First(user => user.UserName == User.Identity.Name));
				db.ReviewConfigurations.Add(reviewconfiguration);
				db.SaveChanges();
				return RedirectToAction("Index");
			}

			return View(reviewconfiguration);
		}
		
		public PartialViewResult AddCategory()
		{
			return PartialView("AddCategory", new ReviewCategory());
		}

		//
		// GET: /Review/Edit/5

		public ActionResult Edit(int id = 0)
		{
			ReviewConfiguration reviewconfiguration = db.ReviewConfigurations.Find(id);
			if (reviewconfiguration == null)
			{
				return HttpNotFound();
			}
			return View(reviewconfiguration);
		}

		//
		// POST: /Review/Edit/5

		[HttpPost]
		public ActionResult Edit(ReviewConfiguration reviewconfiguration)
		{
			if (ModelState.IsValid)
			{
				db.Entry(reviewconfiguration).State = EntityState.Modified;
				db.SaveChanges();
				return RedirectToAction("Index");
			}
			return View(reviewconfiguration);
		}

		//
		// GET: /Review/Delete/5

		public ActionResult Delete(int id = 0)
		{
			ReviewConfiguration reviewconfiguration = db.ReviewConfigurations.Find(id);
			if (reviewconfiguration == null)
			{
				return HttpNotFound();
			}
			return View(reviewconfiguration);
		}

		//
		// POST: /Review/Delete/5

		[HttpPost, ActionName("Delete")]
		public ActionResult DeleteConfirmed(int id)
		{
			ReviewConfiguration reviewconfiguration = db.ReviewConfigurations.Find(id);
			db.ReviewConfigurations.Remove(reviewconfiguration);
			db.SaveChanges();
			return RedirectToAction("Index");
		}

		protected override void Dispose(bool disposing)
		{
			db.Dispose();
			base.Dispose(disposing);
		}
	}
}