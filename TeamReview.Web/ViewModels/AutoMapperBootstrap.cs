using AutoMapper;
using TeamReview.Web.Models;

namespace TeamReview.Web.ViewModels {
	public class AutoMapperBootstrap {
		public static void Initialize() {
			Mapper.CreateMap<ReviewConfiguration, ReviewCreateModel>().ReverseMap();
			Mapper.CreateMap<ReviewConfiguration, ReviewEditModel>()
				.ForMember(model => model.ExistingCategories, opt => opt.ResolveUsing(review => review.Categories))
				.ForMember(model => model.ExistingPeers, opt => opt.ResolveUsing(review => review.Peers));
			Mapper.CreateMap<ReviewCategory, CategoryAddModel>().ReverseMap();
			Mapper.CreateMap<ReviewCategory, CategoryShowModel>();
			Mapper.CreateMap<UserProfile, PeerAddModel>().ReverseMap();
			Mapper.CreateMap<UserProfile, PeerShowModel>();
		}
	}
}