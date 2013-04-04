using System.ComponentModel.DataAnnotations;

namespace TeamReview.Web.ViewModels {
	public class CategoryShowModel {
		public int Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
	}

	public class CategoryAddModel {
		[Required]
		public string Name { get; set; }

		public string Description { get; set; }
	}
}