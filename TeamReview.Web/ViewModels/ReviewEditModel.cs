using System.Collections.Generic;

namespace TeamReview.Web.ViewModels {
	public class ReviewEditModel {
		public int Id { get; set; }

		public string Name { get; set; }

		public CategoryShowModel[] ExistingCategories { get; set; }

		public PeerShowModel[] ExistingPeers { get; set; }

		public IList<CategoryEditModel> AddedCategories { get; set; }

		public IList<PeerEditModel> AddedPeers { get; set; }

		#region Nested type: CategoryEditModel

		public class CategoryEditModel {
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

		#region Nested type: PeerEditModel

		public class PeerEditModel {
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