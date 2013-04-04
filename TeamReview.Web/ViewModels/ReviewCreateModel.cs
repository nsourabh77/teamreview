using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TeamReview.Web.ViewModels {
	public class ReviewCreateModel {
		public ReviewCreateModel() {
			AddedCategories = new List<CategoryAddModel>();
			AddedPeers = new List<PeerAddModel>();
		}

		[Required]
		public string Name { get; set; }

		public IList<CategoryAddModel> AddedCategories { get; set; }

		public IList<PeerAddModel> AddedPeers { get; set; }
	}
}