namespace TeamReview.Web.ViewModels {
	public class PeerShowModel {
		public int UserId { get; set; }
		public string UserName { get; set; }
		public string EmailAddress { get; set; }
	}

	public class PeerAddModel {
		public string UserName { get; set; }
		public string EmailAddress { get; set; }
	}
}