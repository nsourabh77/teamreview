using System;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.Routing;
using TeamReview.Web.Models;

namespace TeamReview.Web.Filters {
	public class DenyDuplicateFeedbackAttribute : ActionFilterAttribute {
		public override void OnActionExecuting(ActionExecutingContext filterContext) {
			var reviewId = this.GetIdValue(filterContext);
			var db = new DatabaseContext();

			Expression<Func<ReviewConfiguration, bool>> userHasAlreadyProvidedFeedback =
				r =>
				r.ReviewId == reviewId &&
				r.Feedback.Any(fb => fb.Reviewer.EmailAddress == filterContext.HttpContext.User.Identity.Name);
			var review = db.ReviewConfigurations.Where(userHasAlreadyProvidedFeedback).SingleOrDefault();
			if (review != null) {
				filterContext.Controller.TempData["Message"] =
					string.Format("You have already completed the review '{0}'. Thank you!", review.Name);
				filterContext.Result =
					new RedirectToRouteResult(new RouteValueDictionary(new { action = "Index", controller = "Review" }));
			}

			base.OnActionExecuting(filterContext);
		}
	}
}