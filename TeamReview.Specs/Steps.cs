using System;
using System.IO;
using Coypu;
using Coypu.Drivers;
using NUnit.Framework;
using TechTalk.SpecFlow;

namespace TeamReview.Specs {
	[Binding]
	public class Steps {
		private static IisExpressProcess _iisExpress;
		private static BrowserSession _browser;
		private static SeleniumServerProcess _seleniumServer;

		[BeforeTestRun]
		public static void BeforeTestRun() {
			var webPath = Path.Combine(
				new DirectoryInfo(Environment.CurrentDirectory).Parent.Parent.Parent.FullName, "TeamReview.Web");
			Assert.That(Directory.Exists(webPath), webPath + " not found!");

			_seleniumServer = new SeleniumServerProcess();
			_seleniumServer.Start();

			_iisExpress = new IisExpressProcess(webPath);
			_iisExpress.Start();
			var sessionConfiguration = new SessionConfiguration
			                           	{
			                           		AppHost = "localhost",
			                           		Port = _iisExpress.Port,
			                           		Browser = Browser.HtmlUnitWithJavaScript,
			                           	};
			_browser = new BrowserSession(sessionConfiguration);
		}

		[AfterScenario]
		public void AfterScenario() {
			if (ScenarioContext.Current.TestError != null) {
#if DEBUG
				// needs to be closed manually
				ProcessHelper.StartInteractive("cmd").WaitForExit();
#endif
			}
		}

		[AfterTestRun]
		public static void AfterTestRun() {
			_browser.Dispose();
			_iisExpress.Dispose();
			_seleniumServer.Dispose();
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
	}
}