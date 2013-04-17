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
		public int PeerStackedRating { get; set; }
		public int MyStackedRating { get; set; }

		public IList<CategoryWithPeersWithResults> CategoriesWithPeersWithResults { get; set; }
		public IList<PeerWithStackedRating> PeersWithStackedRatings { get; set; }
	}

	public class CategoryWithResults {
		public string CategoryName { get; set; }
		public string CategoryDescription { get; set; }
		public int PeerRating { get; set; }
		public int MyRating { get; set; }
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
		public int PeerRating { get; set; }
	}

	public class PeerWithStackedRating {
		public string PeerName { get; set; }
		public int PeerStackedRating { get; set; }		
	}
}