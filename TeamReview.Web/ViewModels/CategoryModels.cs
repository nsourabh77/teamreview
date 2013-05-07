﻿namespace TeamReview.Web.ViewModels {
	public class CategoryShowModel {
		public int CatId { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
	}

	public class CategoryAddModel {
		public string Name { get; set; }
		public string Description { get; set; }
	}
}