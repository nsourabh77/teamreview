using System;
using System.IO;
using Massive;
using NUnit.Framework;

namespace TeamReview.Tests {
	[TestFixture]
	public class MassiveSqlCeTest {
		[Test]
		public void Should_connect_to_sql_ce_db() {
			var dbPath = Path.Combine(new DirectoryInfo(Environment.CurrentDirectory).Parent.Parent.Parent.FullName,
			                          "TeamReview.Web", "App_Data", "TeamReview.sdf");
			var connectionString = string.Format("Data Source={0};Persist Security Info=False;", dbPath);
			var database = new DynamicModel("DefaultConnection", "UserProfile", "UserId").SetConnectionString(connectionString);
			var results = database.Query("SELECT UserName FROM UserProfile");
			foreach (var result in results) {
				Console.WriteLine(result.UserName);
			}
		}
	}
}