using System.Collections.Generic;

namespace TeamReview.Web.ViewModels {
	public class FeedbackViewModel {
		public FeedbackViewModel() {
			CategoriesWithPeersAndRatings = new List<CategoryWithPeersAndRatings>();
		}
		
		public int ReviewId { get; set; }
		public string ReviewName { get; set; }

		public IList<CategoryWithPeersAndRatings> CategoriesWithPeersAndRatings { get; set; }
	}

	public class CategoryWithPeersAndRatings {
		public CategoryWithPeersAndRatings() {
			PeersWithRatings = new List<PeerWithRating>();
		}

		public CategoryShowModel Category { get; set; }
		public IList<PeerWithRating> PeersWithRatings { get; set; }
	}

	public class PeerWithRating {
		public PeerShowModel Peer { get; set; }
		public int Rating { get; set; }
	}
}