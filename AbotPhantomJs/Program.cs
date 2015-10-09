using System;
using System.Net;
using Abot.Core;
using Abot.Crawler;
using Abot.Poco;
using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;

namespace AbotPhantomJs
{
    class Program
    {
        static void Main(string[] args)
        {
            CrawlConfiguration config = new CrawlConfiguration();
            config.MaxConcurrentThreads = 1; // Web Extractor is not currently thread-safe.

            // Create the PhantomJS instance. This will spawn a new PhantomJS process using phantomjs.exe.
            // Make sure to dispose this instance or you will have a zombie process!
            IWebDriver driver = CreatePhantomJsDriver(config);

            // Create the content extractor that uses PhantomJS.
            IWebContentExtractor extractor = new JavaScriptContentExtractor(driver);

            // Create a PageRequester that will use the extractor.
            IPageRequester requester = new PageRequester(config, extractor);

            using (IWebCrawler crawler = new PoliteWebCrawler(config, null, null, null, requester, null, null, null, null)) {
                crawler.PageCrawlCompleted += OnPageCrawlCompleted;

                CrawlResult result = crawler.Crawl(new Uri("http://wvtesting2.com/"));
                if (result.ErrorOccurred)
                    Console.WriteLine("Crawl of {0} completed with error: {1}", result.RootUri.AbsoluteUri, result.ErrorException.Message);
                else
                    Console.WriteLine("Crawl of {0} completed without error.", result.RootUri.AbsoluteUri);
            }

            Console.Read();
        }

        private static void OnPageCrawlCompleted(object p_Sender, PageCrawlCompletedArgs p_PageCrawlCompletedArgs)
        {
            CrawledPage page = p_PageCrawlCompletedArgs.CrawledPage;
            if (page.WebException != null) {
                Console.WriteLine("Crawl of page \"{0}\" failed{2}. Error: {1}",
                    page.Uri.AbsoluteUri,
                    page.WebException.Message,
                    page.IsRetry ? String.Format(" (Retry #{0})", page.RetryCount) : "");
            } else if (page.HttpWebResponse.StatusCode != HttpStatusCode.OK) {
                Console.WriteLine("Crawl of page \"{0}\" failed{2}. HTTP Status Code: {1}",
                    page.Uri.AbsoluteUri,
                    page.HttpWebResponse.StatusCode,
                    page.IsRetry ? String.Format(" (Retry #{0})", page.RetryCount) : "");
            } else {
                Console.WriteLine("Page crawl completed [{0}]", page.Uri.AbsoluteUri);
            }
        }

        private static IWebDriver CreatePhantomJsDriver(CrawlConfiguration p_Config)
        {
            // Optional options passed to the PhantomJS process.
            PhantomJSOptions options = new PhantomJSOptions();
            options.AddAdditionalCapability("phantomjs.page.settings.userAgent", p_Config.UserAgentString);
            options.AddAdditionalCapability("phantomjs.page.settings.javascriptCanCloseWindows", false);
            options.AddAdditionalCapability("phantomjs.page.settings.javascriptCanOpenWindows", false);
            options.AddAdditionalCapability("acceptSslCerts", !p_Config.IsSslCertificateValidationEnabled);

            // Basic auth credentials.
            options.AddAdditionalCapability("phantomjs.page.settings.userName", p_Config.LoginUser);
            options.AddAdditionalCapability("phantomjs.page.settings.password", p_Config.LoginPassword);

            // Create the service while hiding the prompt window.
            PhantomJSDriverService service = PhantomJSDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;
            IWebDriver driver = new PhantomJSDriver(service, options);

            return driver;
        }
    }
}
