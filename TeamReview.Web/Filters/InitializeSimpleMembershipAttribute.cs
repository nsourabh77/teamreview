﻿using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Threading;
using System.Web.Mvc;
using TeamReview.Web.Models;
using WebMatrix.WebData;

namespace TeamReview.Web.Filters {
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public sealed class InitializeSimpleMembershipAttribute : ActionFilterAttribute {
		private static SimpleMembershipInitializer _initializer;
		private static object _initializerLock = new object();
		private static bool _isInitialized;

		public override void OnActionExecuting(ActionExecutingContext filterContext) {
			// Ensure ASP.NET Simple Membership is initialized only once per app start
			LazyInitializer.EnsureInitialized(ref _initializer, ref _isInitialized, ref _initializerLock);
		}

		#region Nested type: SimpleMembershipInitializer

		private class SimpleMembershipInitializer {
			public SimpleMembershipInitializer() {
				Database.SetInitializer<DatabaseContext>(null);

				try {
					using (var context = new DatabaseContext()) {
						if (!context.Database.Exists()) {
							// Create the SimpleMembership database without Entity Framework migration schema
							((IObjectContextAdapter) context).ObjectContext.CreateDatabase();
						}
					}

					WebSecurity.InitializeDatabaseConnection("DefaultConnection", "UserProfile", "UserId", "EmailAddress",
					                                         autoCreateTables: true);
				}
				catch (Exception ex) {
					throw new InvalidOperationException(
						"The ASP.NET Simple Membership database could not be initialized. For more information, please see http://go.microsoft.com/fwlink/?LinkId=256588",
						ex);
				}
			}
		}

		#endregion
	}
}