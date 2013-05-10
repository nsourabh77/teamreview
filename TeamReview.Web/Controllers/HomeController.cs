using System.Net.Mail;
using System.Web.Mvc;
using TeamReview.Web.ViewModels;

namespace TeamReview.Controllers {
	public class HomeController : Controller {
		//
		// GET: /Home/

		public ActionResult Index() {
			return View();
		}

		public ActionResult Features() {
			return View();
		}

		public ActionResult StackRanking() {
			return View();
		}

		public ActionResult PeerReviews() {
			return View();
		}

		public ActionResult PerformanceReview() {
			return View();
		}

		public ActionResult VisualizeData() {
			return View();
		}

		public ActionResult AboutUs() {
			return View();
		}

		public ActionResult Contact() {
			return View();
		}

		[HttpPost]
		public ActionResult Contact(ContactViewModel userdata) {
			if (!ModelState.IsValid) {
				return View();
			}

			var message = new MailMessage("teamreview@teamaton.com", "hello@teamreview.net")
			              	{
			              		Body = userdata.Message,
			              		Subject = "TeamReview.net - contact form"
			              	};
			var displayName = userdata.GetDisplayName();
			message.ReplyToList.Add(new MailAddress(userdata.EmailAddress, displayName));

			new SmtpClient().Send(message);

			TempData["MessageSent"] = "true";
			return RedirectToAction("Contact");
		}
	}
}