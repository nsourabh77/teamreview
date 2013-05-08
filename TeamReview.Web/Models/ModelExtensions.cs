using System;
using System.Collections.Generic;
using System.Linq;

namespace TeamReview.Web.Models {
	public static class ModelExtensions {
		public static decimal GetOwnRatingForCategory(
			this IEnumerable<ReviewFeedback> reviewFeedbacks, int userId, ReviewCategory category) {
			if (reviewFeedbacks == null) throw new ArgumentNullException("reviewFeedbacks");

			var assessment = reviewFeedbacks.SelectMany(fb => fb.Assessments)
				.Where(a => a.ReviewCategory.CatId == category.CatId)
				.Where(a => a.ReviewedPeer.UserId == userId && a.Reviewer.UserId == userId)
				.SingleOrDefault();

			return assessment != null ? assessment.Rating : 0;
		}

		public static decimal GetPeerRatingForPeerForCategory(
			this IEnumerable<ReviewFeedback> reviewFeedbacks, int peerId, ReviewCategory category) {
			if (reviewFeedbacks == null) throw new ArgumentNullException("reviewFeedbacks");

			var otherReviewersCount = reviewFeedbacks.Count(fb => fb.Reviewer.UserId != peerId);
			return otherReviewersCount > 0
			       	? reviewFeedbacks.SelectMany(fb => fb.Assessments)
			       	  	.Where(a => a.ReviewCategory.CatId == category.CatId)
			       	  	.Where(a => a.ReviewedPeer.UserId == peerId && a.Reviewer.UserId != peerId)
			       	  	.Sum(a => a.Rating)/(decimal) otherReviewersCount
			       	: 0;
		}

		public static IEnumerable<string> Names(this IEnumerable<UserProfile> users) {
			if (users == null) return new string[0];
			return users.Select(u => u.UserName);
		}
	}
}