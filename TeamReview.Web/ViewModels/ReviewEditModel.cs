using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TeamReview.Web.ViewModels {
	public class ReviewCreateEditModel {
		public ReviewCreateEditModel() {
			ExistingCategories = new CategoryShowModel[0];
			ExistingPeers = new PeerShowModel[0];
			AddedCategories = new List<CategoryAddModel>();
			AddedPeers = new List<PeerAddModel>();
		}

		[Required]
		public string Name { get; set; }

		public CategoryShowModel[] ExistingCategories { get; set; }

		public PeerShowModel[] ExistingPeers { get; set; }

		public IList<CategoryAddModel> AddedCategories { get; set; }

		public IList<PeerAddModel> AddedPeers { get; set; }
	}
}