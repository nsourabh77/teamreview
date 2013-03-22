using System;
using System.IO;
using Coypu;
using Coypu.Drivers;
using NUnit.Framework;
using OpenQA.Selenium;
using TechTalk.SpecFlow;

namespace TeamReview.Specs {
	[Binding]
	public class Steps : IDisposable {
		private static IisExpressProcess _iisExpress;
		private static BrowserSession _browser;
		private static SeleniumServerProcess _seleniumServer;
		private static bool _dbRestored;

		private static readonly string WebPath = Path.Combine(
			new DirectoryInfo(Environment.CurrentDirectory).Parent.Parent.Parent.FullName, "TeamReview.Web");

		private static string _dbPath;

		private static string DbPath {
			get {
				return _dbPath ??
				       (_dbPath = Path.Combine(WebPath, "App_Data", GetDbName()));
			}
		}

		private static string DbBkpPath {
			get { return DbPath + ".bak"; }
		}

		#region IDisposable Members

		public void Dispose() {
			Console.WriteLine("Disposing...");
			if (!_dbRestored) {
				RestoreExistingDatabase();
			}
		}

		#endregion

		[BeforeTestRun]
		public static void BeforeTestRun() {
			Assert.That(Directory.Exists(WebPath), WebPath + " not found!");

			BackupExistingDatabase();

			_seleniumServer = new SeleniumServerProcess();
			_seleniumServer.Start();

			_iisExpress = new IisExpressProcess(WebPath);
			_iisExpress.Start();
			var sessionConfiguration = new SessionConfiguration
			                           	{
			                           		AppHost = "localhost",
			                           		Port = _iisExpress.Port,
			                           		//Browser = Browser.Firefox,
			                           		Browser = Browser.HtmlUnitWithJavaScript,
			                           		Timeout = TimeSpan.FromSeconds(15),
			                           		RetryInterval = TimeSpan.FromSeconds(1),
			                           	};
			_browser = new BrowserSession(sessionConfiguration);
		}

		[AfterScenario]
		public void AfterScenario() {
			if (ScenarioContext.Current.TestError != null) {
				Console.WriteLine("Error Html:" + Environment.NewLine +
				                  ((IWebDriver) _browser.Driver.Native).PageSource);
#if DEBUG
				// needs to be closed manually
				ProcessHelper.StartInteractive("cmd").WaitForExit();
#endif
			}
		}

		[AfterTestRun]
		public static void AfterTestRun() {
			try {
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
			finally {
				RestoreExistingDatabase();
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

		[Given(@"I am not logged into TeamReview")]
		public void GivenIAmNotLoggedIntoTeamReview() {
			_browser.Visit("/");
			Assert.That(_browser.HasCss("#loginLink"));
			Assert.That(_browser.HasCss("#registerLink"));
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
			_browser.FindId("approve_button").Click(); // sign in to Google
		}

		[When(@"I fill in a user name")]
		public void WhenIFillInMyUserName() {
			_browser.FillIn("UserName").With("Tester");
		}

		[When(@"I finish registering")]
		public void WhenIFinishRegistering() {
			_browser.FindId("Register").Click();
		}

		[Then(@"a new account was created with my Google address")]
		public void ThenANewAccountWasCreatedWithMyGoogleAddress() {
			//
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

		#region Helpers

		private static void BackupExistingDatabase() {
			if (!File.Exists(DbPath)) {
				Console.WriteLine(string.Format("INFO: {0} does not exist", DbPath));
			}
			else {
				File.Copy(DbPath, DbPath + "." + DateTime.Now.ToString("yyyyMMdd-HHmmss"), true);
				if (File.Exists(DbBkpPath)) {
					File.Delete(DbBkpPath);
				}
				File.Move(DbPath, DbBkpPath);
			}
		}

		private static void RestoreExistingDatabase() {
			if (File.Exists(DbBkpPath)) {
				if (File.Exists(DbPath)) {
					File.Delete(DbPath);
				}
				File.Move(DbBkpPath, DbPath);
			}
			Assert.That(File.Exists(DbPath));
			_dbRestored = true;
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

	public class Email {
		public string Address { get; set; }
		public string Password { get; set; }
	}
}