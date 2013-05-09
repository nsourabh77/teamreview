using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using TeamReview.Web.Models;

namespace TeamReview.Web.Filters {
	public class AccessControlAttribute : ActionFilterAttribute {
		private readonly string[] _actionNamesToIgnore;

		public AccessControlAttribute(params string[] actionNamesToIgnore) {
			_actionNamesToIgnore = actionNamesToIgnore;
		}

		public override void OnActionExecuting(ActionExecutingContext filterContext) {
			if (!_actionNamesToIgnore.Contains(filterContext.ActionDescriptor.ActionName)) {
				object reviewIdObj;
				if (!filterContext.ActionParameters.TryGetValue("id", out reviewIdObj)) {
					throw new ArgumentNullException(
						"id", string.Format(
							"Could not find 'id' parameter in action method '{0}' but need it to control access!",
							filterContext.ActionDescriptor.ActionName));
				}

				int reviewId;
				try {
					reviewId = Convert.ToInt32(reviewIdObj);
				}
				catch (SystemException se) {
					throw new ArgumentException(
						string.Format("The 'id' parameter must be of type 'int' but was of type '{0}'!", reviewIdObj.GetType().Name),
						"id", se);
				}

				var db = new DatabaseContext();
				if (db.ReviewConfigurations.Count(r => r.ReviewId == reviewId) == 1) {
					var loggedInUserEmailAddress = filterContext.HttpContext.User.Identity.Name;
					if (db.ReviewConfigurations.Where(r => r.ReviewId == reviewId)
					    	.Count(r => r.Peers.Any(p => p.EmailAddress == loggedInUserEmailAddress)) < 1) {
						// for something more fancy, see: http://stackoverflow.com/a/13905859/177710
						filterContext.Result = new HttpStatusCodeResult(
							HttpStatusCode.Forbidden, "You don't have permission to access this page.");
					}
				}
				else {
					base.OnActionExecuting(filterContext);
				}
			}
		}
	}
}