using System;
using System.Collections.Generic;
using System.Linq;

namespace TeamReview.Web.Models {
	public static class ReviewFeedbackExtensions {
		public static decimal GetRatingForPeerForCategory(
			this IEnumerable<ReviewFeedback> reviewFeedbacks, UserProfile peer, ReviewCategory category) {
			if (reviewFeedbacks == null) throw new ArgumentNullException("reviewFeedbacks");

			var otherReviewersCount = reviewFeedbacks.Count(fb => fb.Reviewer.UserId != peer.UserId);
			return otherReviewersCount > 0
			       	? reviewFeedbacks.SelectMany(fb => fb.Assessments)
			       	  	.Where(a => a.ReviewCategory.CatId == category.CatId)
			       	  	.Where(a => a.ReviewedPeer.UserId == peer.UserId && a.Reviewer.UserId != peer.UserId)
			       	  	.Sum(a => a.Rating)/(decimal) otherReviewersCount
			       	: 0;
		}
	}
}