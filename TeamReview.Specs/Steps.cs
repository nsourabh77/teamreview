using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Coypu;
using Coypu.Drivers;
using NUnit.Framework;
using OpenQA.Selenium;
using TeamReview.Web.Models;
using TechTalk.SpecFlow;

namespace TeamReview.Specs {
	[Binding]
	public class Steps {
		private const int _port = 12345;
		private static IisExpressProcess _iisExpress;
		private static BrowserSession _browser;
		private static SeleniumServerProcess _seleniumServer;

		private static readonly string WebPath = Path.Combine(
			new DirectoryInfo(Environment.CurrentDirectory).Parent.Parent.Parent.FullName, "TeamReview.Web");

		private static string _dbPath;

		private static readonly string ConnectionString = string.Format("Data Source={0};Persist Security Info=False;", DbPath);

		private static string DbPath {
			get {
				return _dbPath ??
				       (_dbPath = Path.Combine(WebPath, "App_Data", GetDbName()));
			}
		}

		private static string DbBkpPath {
			get { return DbPath + ".bak"; }
		}

		[BeforeTestRun]
		public static void BeforeTestRun() {
			Assert.That(Directory.Exists(WebPath), WebPath + " not found!");

			BackupExistingDatabase();

			AppDomain.CurrentDomain.SetData("DataDirectory", Path.GetFullPath(Path.Combine(WebPath, "App_Data")));

			_seleniumServer = new SeleniumServerProcess();
			_seleniumServer.Start();

			var sessionConfiguration = new SessionConfiguration
			                           	{
			                           		AppHost = "localhost",
			                           		Port = _port,
			                           		Browser = Browser.Firefox,
			                           		//Browser = Browser.HtmlUnitWithJavaScript,
			                           		Timeout = TimeSpan.FromSeconds(15),
			                           		RetryInterval = TimeSpan.FromSeconds(1),
			                           	};
			_browser = new BrowserSession(sessionConfiguration);
		}

		[BeforeScenario]
		public void BeforeScenario() {
			_iisExpress = new IisExpressProcess(WebPath, _port);
			_iisExpress.Start();
			DeleteAllCookies();
		}

		private static void DeleteAllCookies() {
			((IWebDriver) _browser.Driver.Native).Manage().Cookies.DeleteAllCookies();
		}

		[AfterScenario]
		public void AfterScenario() {
			var testError = ScenarioContext.Current.TestError;
			if (testError != null) {
				Console.WriteLine(testError.Message);
				Console.WriteLine(testError.StackTrace);
				Console.WriteLine(testError.InnerException);
				Console.WriteLine("Error Html:" + Environment.NewLine +
				                  ((IWebDriver) _browser.Driver.Native).PageSource);
#if DEBUG
				// needs to be closed manually
				ProcessHelper.StartInteractive("cmd").WaitForExit();
#endif
			}
			_iisExpress.Dispose();
			DeleteTestDatabase();
		}

		[AfterTestRun]
		public static void AfterTestRun() {
			RestoreExistingDatabase();
			try {
				_browser.Dispose();
			}
			catch {
			}
			try {
				_iisExpress.Dispose();
			}
			catch {
			}
			try {
				_seleniumServer.Dispose();
			}
			catch {
			}
		}

		[Given(@"I navigate to the homepage")]
		public void GivenINavigateToTheHomepage() {
			_browser.Visit("/");
			Assert.That(_browser.Location.AbsolutePath, Is.EqualTo("/"));
		}

		[Then(@"I should find <(.*)> on page")]
		public void ThenIShouldFindOnPage(string htmlElement) {
			Assert.That(_browser.HasCss(htmlElement));
		}

		[Given(@"I own a Google account")]
		public void GivenIOwnAGoogleAccount() {
			ScenarioContext.Current.Set(
				new Email { Address = "test@teamaton.com", Password = "9c60026e5467eeeee49c7d2b491dd6d2" });
		}

		[StepDefinition(@"I am not logged into TeamReview")]
		public void GivenIAmNotLoggedIntoTeamReview() {
			// in case we haven't visited any other resource yet
			Console.WriteLine("Currently on " + _browser.Location);
			if (_browser.Location.ToString() == "about:blank") {
				_browser.Visit("/");
			}
			if (_browser.HasCss("#logoffLink")) {
				WhenILogOut();
			}
			Assert.That(_browser.HasCss("#loginLink"));
		}

		[When(@"I register a new account")]
		public void WhenIRegisterANewAccount() {
			_browser.Visit("/Account/Register");
		}

		[When(@"I use my Google account")]
		public void WhenIUseMyGoogleAccount() {
			_browser.ClickButton("Google");

			// Google login page
			var email = ScenarioContext.Current.Get<Email>();
			_browser.FillIn("Email").With(email.Address);
			_browser.FillIn("Passwd").With(email.Password);
			_browser.Uncheck("PersistentCookie"); // don't remember me
			_browser.FindId("signIn").Click(); // sign in to Google

			// Google OpenID acceptance page
			_browser.Uncheck("remember_choices_checkbox"); // don't remember my choice
			_browser.FindId("approve_button").Click(); // authenticate using Google
		}

		[When(@"I fill in a user name")]
		public void WhenIFillInMyUserName() {
			_browser.FillIn("UserName").With("Tester");
		}

		[When(@"I finish registering")]
		public void WhenIFinishRegistering() {
			_browser.FindId("Register").Click();
			Thread.Sleep(1000);
			using (var ctx = new ReviewsContext()) {
				Console.WriteLine("Retrieving user from DB");
				ScenarioContext.Current.Set(ctx.UserProfiles.Single());
			}
		}

		[Then(@"a new account was created with my Google address")]
		public void ThenANewAccountWasCreatedWithMyGoogleAddress() {
			var emailAddress = ScenarioContext.Current.Get<Email>().Address;
			Thread.Sleep(1000);
			using (var ctx = new ReviewsContext()) {
				Console.WriteLine("Retrieving user from DB");
				Assert.That(ctx.UserProfiles.Single().EmailAddress, Is.EqualTo(emailAddress));
			}
		}

		[Then(@"I am logged in")]
		public void ThenIAmLoggedIn() {
			Assert.That(_browser.HasCss("#logoffLink"));
		}

		[Then(@"I am on the ""(.*)""")]
		public void ThenIAmOnThe(string pageName) {
			string path;
			switch (pageName) {
				case "Dashboard":
					path = "/";
					break;
				default:
					throw new ArgumentOutOfRangeException("pageName", "No mapping from '{0}' to concrete url path exists!");
			}
			Assert.That(_browser.Location.AbsolutePath, Is.EqualTo(path));
		}

		[When(@"I create a new review")]
		public void WhenICreateANewReview() {
			ScenarioContext.Current.Set(new ReviewConfiguration { Peers = { ScenarioContext.Current.Get<UserProfile>() } });
			_browser.Visit("/Review/Create");
		}

		[When(@"I edit my review")]
		public void WhenIEditMyReview()
		{
			var reviewId = ScenarioContext.Current.Get<ReviewConfiguration>().ReviewId;
			_browser.Visit("/Review/Edit/"+reviewId);
		}

		[Given(@"I am logged in")]
		public void GivenIAmLoggedIn() {
			GivenIOwnAGoogleAccount();
			WhenILogInUsingMyGoogleAccount();
			WhenIFinishRegistering();
		}

		[Given(@"I own a review")]
		public void GivenIOwnAReview()
		{
			var thisIsMe = ScenarioContext.Current.Get<UserProfile>();
			var reviewConfiguration = new ReviewConfiguration { Name = "NewReview", Peers = { thisIsMe } };
			using (var ctx = new ReviewsContext())
			{
				Console.WriteLine("Writing review to DB");
				ctx.ReviewConfigurations.Add(reviewConfiguration);
				ctx.SaveChanges();
			}
			ScenarioContext.Current.Set(reviewConfiguration);
		}

		[Given(@"I have an account at TeamReview")]
		public void GivenIHaveAnAccountAtTeamReview() {
			GivenIOwnAGoogleAccount();
			WhenIRegisterANewAccount();
			WhenIUseMyGoogleAccount();
			WhenIFillInMyUserName();
			WhenIFinishRegistering();
			WhenILogOut();
		}

		[When(@"I log in using my Google account")]
		public void WhenILogInUsingMyGoogleAccount() {
			GivenIOwnAGoogleAccount();
			_browser.Visit("/Account/Login");
			WhenIUseMyGoogleAccount();
		}

		[Given(@"I don't have an account at TeamReview")]
		public void GivenIDonTHaveAnAccountAtTeamReview() {
			using (var ctx = new ReviewsContext()) {
				Console.WriteLine("Retrieving user from DB");
				if (!ctx.Database.Exists())
					Console.WriteLine("DB does not exist yet - no account exists");
				else
					Assert.That(ctx.UserProfiles.ToList(), Has.Count.EqualTo(0));
			}
		}

		[Given(@"I am logged into TeamReview")]
		public void GivenIAmLoggedIntoTeamReview() {
			GivenIAmLoggedIn();
		}

		/// <summary>
		/// Logs user out and deletes all Cookies.
		/// </summary>
		[When(@"I log out")]
		public void WhenILogOut() {
			_browser.Visit("https://www.google.com");
			DeleteAllCookies();
			_browser.Visit("/");
			_browser.FindId("logoffLink").Click();
			DeleteAllCookies();
		}

		[Then(@"I am on the login page")]
		public void ThenIAmOnTheLoginPage() {
			Assert.That(_browser.Location.AbsolutePath, Is.EqualTo("/Account/Login"),
			            "Should be on the login page but am on " + _browser.Location);
		}

		[When(@"I fill in a review name")]
		public void WhenIFillInAReviewName() {
			const string reviewName = "NewReview";
			ScenarioContext.Current.Get<ReviewConfiguration>().Name = reviewName;
			_browser.FillIn("Name").With(reviewName);
		}

		[When(@"I add (?:a|another) category")]
		public void WhenIAddACategory() {
			ScenarioContext.Current.Get<ReviewConfiguration>().Categories.Add(new ReviewCategory());
			_browser.FindId("addCategory").Click();
		}

		[When(@"I fill in a category name")]
		public void WhenIFillInACategoryName() {
			var name = "cat-" + new Random().Next(100, 1000);
			ScenarioContext.Current.Get<ReviewConfiguration>().Categories.Last().Name = name;
			_browser.FindAllCss("section.category").Last().FillIn("Name").With(name);
		}

		[When(@"I fill in a category description")]
		public void WhenIFillInACategoryDescription() {
			var description = "desc-" + Guid.NewGuid();
			ScenarioContext.Current.Get<ReviewConfiguration>().Categories.Last().Description = description;
			_browser.FindAllCss("section.category").Last().FillIn("Description").With(description);
		}

		[When(@"I save the review")]
		public void WhenISaveTheReview() {
			_browser.ClickButton("Save");
		}

		[Then(@"my new review was created with those categories")]
		public void ThenMyNewReviewWasCreatedWithThoseCategories() {
			var review = ScenarioContext.Current.Get<ReviewConfiguration>();
			Thread.Sleep(1000);
			using (var ctx = new ReviewsContext()) {
				Console.WriteLine("Retrieving review from DB");
				var reviewFromDb = ctx.ReviewConfigurations.Include("Categories").Single();
				Assert.AreEqual(review.Name, reviewFromDb.Name);
				Assert.AreEqual(review.Categories.Count, reviewFromDb.Categories.Count);
				Assert.That(reviewFromDb.Categories, Is.EqualTo(
					review.Categories).AsCollection.Using(new CategoryComparer()));
			}
		}

		[Then(@"my review is updated with the new category")]
		public void ThenMyReviewIsUpdatedWithTheNewCategory()
		{
			ThenMyNewReviewWasCreatedWithThoseCategories();
		}

		[Then(@"I am added to the review")]
		public void ThenIAmAddedToTheReview() {
			var thisIsMe = ScenarioContext.Current.Get<UserProfile>();
			using (var ctx = new ReviewsContext()) {
				Console.WriteLine("Retrieving review from DB");
				var reviewFromDb = ctx.ReviewConfigurations.Include("Peers").Single();
				Assert.That(reviewFromDb.Peers.Count, Is.EqualTo(1));
				Assert.That(reviewFromDb.Peers[0].UserId, Is.EqualTo(thisIsMe.UserId));
			}
		}

		[Then(@"I am on the ""(.*)"" page for my review")]
		public void ThenIAmOnThePageForMyReview(string pagename)
		{
			Assert.IsTrue(_browser.Title.Contains(pagename));
		}

		[Then(@"I see the message ""(.*)""")]
		public void ThenISeeTheMessage(string message)
		{
			Assert.IsTrue(_browser.HasContent(message));
		}


		#region Helpers

		private static void BackupExistingDatabase() {
			if (!File.Exists(DbPath)) {
				Console.WriteLine(string.Format("INFO: {0} does not exist", DbPath));
			}
			else {
				// second backup copy with timestamp in case something goes wrong
				File.Copy(DbPath, DbPath + "." + DateTime.Now.ToString("yyyyMMdd-HHmmss"), true);
				File.Copy(DbPath, DbBkpPath, true);
			}
		}

		private static void DeleteTestDatabase() {
			if (File.Exists(DbPath)) {
				File.Delete(DbPath);
			}
		}

		private static void RestoreExistingDatabase() {
			DeleteTestDatabase();
			if (File.Exists(DbBkpPath)) {
				Console.WriteLine("Restoring original DB from Backup.");
				File.Move(DbBkpPath, DbPath);
				Assert.That(File.Exists(DbPath),
				            "The original DB file should have been restored but hasn't been! Try to restore it manually (in TeamReview.Web\\App_Data).");
			}
			else {
				Assert.That(!File.Exists(DbPath),
				            "The original DB file should not exist since it didn't exist when Specs were started!");
			}
		}

		private static string GetDbName() {
			var webConfigPath = Path.Combine(WebPath, "web.config");
			Assert.That(File.Exists(webConfigPath), string.Format("The web.config could not be found in '{0}'.", WebPath));
			var dbNameMarker = @"connectionString=""Data Source=|DataDirectory|\";
			var webConfigText = File.ReadAllText(webConfigPath);
			var dbNameIndex = webConfigText.IndexOf(dbNameMarker) + dbNameMarker.Length;
			Assert.That(dbNameIndex, Is.GreaterThan(dbNameMarker.Length),
			            "The database name could not be found in the web.config! Please provide one in the connectionString.");
			var dbNameLength = webConfigText.IndexOf(';', dbNameIndex) - dbNameIndex;
			return webConfigText.Substring(dbNameIndex, dbNameLength);
		}

		#endregion
	}

	internal class Email {
		public string Address { get; set; }
		public string Password { get; set; }
	}

	internal class CategoryComparer : IEqualityComparer<ReviewCategory> {
		#region IEqualityComparer<ReviewCategory> Members

		public bool Equals(ReviewCategory x, ReviewCategory y) {
			if (x == null && y == null) return true;
			if (x == null || y == null) return false;
			return x.Name == y.Name && x.Description == y.Description;
		}

		public int GetHashCode(ReviewCategory obj) {
			return obj.GetHashCode();
		}

		#endregion
	}
}