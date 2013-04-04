using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TeamReview.Web.ViewModels {
	public class ReviewEditModel {
		public ReviewEditModel() {
			ExistingCategories = new CategoryShowModel[0];
			ExistingPeers = new PeerShowModel[0];
			AddedCategories = new List<CategoryAddModel>();
			AddedPeers = new List<PeerAddModel>();
		}

		public int Id { get; set; }

		public string Name { get; set; }

		public CategoryShowModel[] ExistingCategories { get; set; }

		public PeerShowModel[] ExistingPeers { get; set; }

		public IList<CategoryAddModel> AddedCategories { get; set; }

		public IList<PeerAddModel> AddedPeers { get; set; }

		#region Nested type: CategoryAddModel

		public class CategoryAddModel {
			[Required]
			public string Name { get; set; }
			public string Description { get; set; }
		}

		#endregion

		#region Nested type: CategoryShowModel

		public class CategoryShowModel {
			public int Id { get; set; }
			public string Name { get; set; }
			public string Description { get; set; }
		}

		#endregion

		#region Nested type: PeerAddModel

		public class PeerAddModel {
			[Required]
			public string UserName { get; set; }
			public string EmailAddress { get; set; }
		}

		#endregion

		#region Nested type: PeerShowModel

		public class PeerShowModel {
			public int Id { get; set; }
			public string UserName { get; set; }
			public string EmailAddress { get; set; }
		}

		#endregion
	}
}