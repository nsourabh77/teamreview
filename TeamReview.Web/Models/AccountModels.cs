using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Web.Mvc;
using DataAnnotationsExtensions;

namespace TeamReview.Web.Models {
	public class DatabaseContext : DbContext {
		public DatabaseContext()
			: base("DefaultConnection") {
		}

		public DbSet<UserProfile> UserProfiles { get; set; }
		public DbSet<ReviewConfiguration> ReviewConfigurations { get; set; }
		public DbSet<ReviewCategory> ReviewCategories { get; set; }

		protected override void OnModelCreating(DbModelBuilder modelBuilder) {
			base.OnModelCreating(modelBuilder);
			modelBuilder.Entity<ReviewConfiguration>()
				.HasMany(s => s.Peers)
				.WithMany(a => a.ReviewConfigurations)
				.Map(m => {
				     	m.MapLeftKey("ReviewId");
				     	m.MapRightKey("PeerId");
				     	m.ToTable("ReviewsPeers");
				     });
		}
	}

	[Table("ReviewConfiguration")]
	public class ReviewConfiguration {
		public ReviewConfiguration() {
			Categories = new List<ReviewCategory>();
			Peers = new List<UserProfile>();
			Feedback = new List<ReviewFeedback>();
		}

		[Key]
		[DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
		public int ReviewId { get; set; }

		public string Name { get; set; }
		public bool Active { get; set; }
		public IList<ReviewCategory> Categories { get; set; }
		public IList<UserProfile> Peers { get; set; }
		public IList<ReviewFeedback> Feedback { get; set; }
	}

	[Table("ReviewCategory")]
	public class ReviewCategory {
		[Key]
		[DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
		public int CatId { get; set; }

		public string Name { get; set; }
		public string Description { get; set; }
	}

	[Table("ReviewFeedback")]
	public class ReviewFeedback {
		public ReviewFeedback() {
			Assessments = new List<Assessment>();
		}

		[Key]
		[DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
		public int FeedbackId { get; set; }

		public UserProfile Reviewer { get; set; }
		public IList<Assessment> Assessments { get; set; }
	}

	[Table("Assessment")]
	public class Assessment {
		[Key]
		[DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
		public int AssessmentId { get; set; }

		public ReviewCategory ReviewCategory { get; set; }
		public UserProfile ReviewedPeer { get; set; }
		public int Rating { get; set; }
	}

	[Table("UserProfile")]
	public class UserProfile {
		[Key]
		[DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
		public int UserId { get; set; }

		public string UserName { get; set; }
		public string EmailAddress { get; set; }

		public ICollection<ReviewConfiguration> ReviewConfigurations { get; set; }
	}

	public class RegisterExternalLoginModel {
		[Required]
		[Display(Name = "User name")]
		public string UserName { get; set; }

		[Required]
		[Email]
		[Display(Name = "Email address")]
		public string EmailAddress { get; set; }

		public string ExternalLoginData { get; set; }
	}

	public class LocalPasswordModel {
		[Required]
		[DataType(DataType.Password)]
		[Display(Name = "Current password")]
		public string OldPassword { get; set; }

		[Required]
		[StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
		[DataType(DataType.Password)]
		[Display(Name = "New password")]
		public string NewPassword { get; set; }

		[DataType(DataType.Password)]
		[Display(Name = "Confirm new password")]
		[Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
		public string ConfirmPassword { get; set; }
	}

	public class LoginModel {
		[Required]
		[Display(Name = "User name")]
		public string UserName { get; set; }

		[Required]
		[DataType(DataType.Password)]
		[Display(Name = "Password")]
		public string Password { get; set; }

		[Display(Name = "Remember me?")]
		public bool RememberMe { get; set; }
	}

	public class RegisterModel {
		[Required]
		[Display(Name = "User name")]
		public string UserName { get; set; }

		[Required]
		[StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
		[DataType(DataType.Password)]
		[Display(Name = "Password")]
		public string Password { get; set; }

		[DataType(DataType.Password)]
		[Display(Name = "Confirm password")]
		[Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
		public string ConfirmPassword { get; set; }
	}

	public class ExternalLogin {
		public string Provider { get; set; }
		public string ProviderDisplayName { get; set; }
		public string ProviderUserId { get; set; }
	}
}