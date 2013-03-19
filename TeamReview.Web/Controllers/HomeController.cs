using System.Web.Mvc;

namespace TeamReview.Controllers {
	public class HomeController : Controller {
		//
		// GET: /Home/

		public ActionResult Index() {
			return View();
		}
	}
}