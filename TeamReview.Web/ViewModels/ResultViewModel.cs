using System.Collections.Generic;

namespace TeamReview.Web.ViewModels {
	public class ResultViewModel {
		public ResultViewModel() {
			CategoriesWithMyResults = new List<CategoryWithResults>();
			CategoriesWithPeersWithResults = new List<CategoryWithPeersWithResults>();
			PeersWithStackedRatings = new List<PeerWithStackedRating>();
		}

		public string ReviewName { get; set; }
		
		public IList<CategoryWithResults> CategoriesWithMyResults { get; set; }
		public decimal PeerStackedRating { get; set; }
		public int MyStackedRating { get; set; }

		public IList<CategoryWithPeersWithResults> CategoriesWithPeersWithResults { get; set; }
		public IList<PeerWithStackedRating> PeersWithStackedRatings { get; set; }
		public CategoryPeerRatings CategoryPeerRatings { get; set; }

		// [cat1: [p1: 5, p2: 6.5, p3: 3.67], cat2: [p1: 5, p2: 6.5, p3: 3.67], ...]
		public IList<decimal[]> RatingsForPeersPerCategory { get; set; }
	}

	public class CategoryWithResults {
		public string CategoryName { get; set; }
		public string CategoryDescription { get; set; }
		public decimal PeerRating { get; set; }
		public int MyRating { get; set; }
	}

	public class CategoryPeerRatings {
		public IEnumerable<string> Categories { get; set; }
		public IList<PeerWithRatings> PeersWithRatings { get; set; }
	}

	public class CategoryWithPeersWithResults {
		public CategoryWithPeersWithResults() {
			PeersWithResult = new List<PeerWithResult>();
		}

		public string CategoryName { get; set; }
		public string CategoryDescription { get; set; }
		public IList<PeerWithResult> PeersWithResult { get; set; }
	}

	public class PeerWithResult {
		public string PeerName { get; set; }
		public decimal PeerRating { get; set; }
	}

	public class PeerWithRatings {
		public string PeerName { get; set; }
		/// <summary>
		/// In the same order as the category names
		/// </summary>
		public IList<float> Ratings { get; set; }
	}

	public class PeerWithStackedRating {
		public string PeerName { get; set; }
		public decimal PeerStackedRating { get; set; }		
	}
}