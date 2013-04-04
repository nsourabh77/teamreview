using System.ComponentModel.DataAnnotations;

namespace TeamReview.Web.ViewModels {
	public class PeerShowModel {
		public int Id { get; set; }
		public string UserName { get; set; }
		public string EmailAddress { get; set; }
	}

	public class PeerAddModel {
		[Required]
		public string UserName { get; set; }

		[Required]
		public string EmailAddress { get; set; }
	}
}