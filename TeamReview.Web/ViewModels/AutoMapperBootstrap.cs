using AutoMapper;
using TeamReview.Web.Models;

namespace TeamReview.Web.ViewModels {
	public class AutoMapperBootstrap {
		public static void Initialize() {
			Mapper.CreateMap<ReviewConfiguration, ReviewEditModel>()
				.ForMember(model => model.ExistingCategories, opt => opt.ResolveUsing(review => review.Categories))
				.ForMember(model => model.ExistingPeers, opt => opt.ResolveUsing(review => review.Peers));
			Mapper.CreateMap<ReviewCategory, ReviewEditModel.CategoryAddModel>().ReverseMap();
			Mapper.CreateMap<ReviewCategory, ReviewEditModel.CategoryShowModel>();
			Mapper.CreateMap<UserProfile, ReviewEditModel.PeerAddModel>().ReverseMap();
			Mapper.CreateMap<UserProfile, ReviewEditModel.PeerShowModel>();
		}
	}
}