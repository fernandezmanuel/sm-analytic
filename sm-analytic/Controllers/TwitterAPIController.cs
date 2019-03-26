using System;
using Microsoft.AspNetCore.Mvc;
using Tweetinvi;
using Tweetinvi.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http;
using System.Collections.Generic;
using Tweetinvi.Parameters;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Rest;
using System.Threading;


namespace sm_analytic.Controllers
{

    /*
     * This controller takes care of the Twitter authentication process
     * For both our application and individual users
     */ 
    [ApiController]
    [EnableCors("AllowMyOrigin")]
    public class TwitterAPIController : ControllerBase
    {

        private IMemoryCache _cache;
        private readonly IHttpClientFactory _clientFactory;

        /*
         * Creates cache to store authentication data 
         * This makes it so that the same authentication data
         * can be stored and shared between requests
         */
        public TwitterAPIController(IMemoryCache memoryCache, IHttpClientFactory clientFactory)
        {
            _cache = memoryCache;
            _clientFactory = clientFactory;
        }

        /*
         * Function authenticates our application to user Twitter API
         * Also begins the user authentication process
         * and gets URL for login page that user needs to be redirected to
         * This URL gets returned to the front end
         */
        [Route("~/api/TwitterAuth")]
        [HttpGet]
        public string TwitterAuth()
        {

            AuthorizeOurApp();

            var appCreds = new ConsumerCredentials(
                Environment.GetEnvironmentVariable("CONSUMER_KEY"),
                Environment.GetEnvironmentVariable("CONSUMER_SECRET")
            );

            var redirectURL = Environment.GetEnvironmentVariable("redirectURL");
            IAuthenticationContext _authenticationContext = AuthFlow.InitAuthentication(appCreds, redirectURL);
            _cache.Set("_authContext", _authenticationContext);

            return _authenticationContext.AuthorizationURL;

        }

        /* After the user logins in/authorises our app through the 
         * redirected url, their credentials get passed to this function
         * This function authenticates the user
         * Returns user info after successful authentication
         * Returns an error otherwise
         */
        [Route("~/api/ValidateTwitterAuth")]
        [HttpPost]
        public ObjectResult ValidateTwitterAuth([FromBody] Credentials credentials)
        {


            IAuthenticationContext _authenticationContext;
            _cache.TryGetValue("_authContext", out _authenticationContext);

            try
            {
                var userCreds = AuthFlow.CreateCredentialsFromVerifierCode(credentials.oauth_verifier, _authenticationContext);
                var user = Tweetinvi.User.GetAuthenticatedUser(userCreds);

                var allHashtagsUsed = CountHashtags(user.GetUserTimeline());
                var publicPostsWithHashtags = SearchHashtags(allHashtagsUsed);
                var sentimentData = GetSentiment(user.GetMentionsTimeline());

                ObjectResult userInfo = new ObjectResult(user);
                ObjectResult tweetTimeline = new ObjectResult(user.GetUserTimeline());
                ObjectResult followers = new ObjectResult(user.GetFollowers());
                ObjectResult mentions = new ObjectResult(user.GetMentionsTimeline());
                ObjectResult hashtagCount = new ObjectResult(allHashtagsUsed);
                ObjectResult searchedHashtags = new ObjectResult(publicPostsWithHashtags);
                // ObjectResult sentiment = new ObjectResult(sentimentData);

                IEnumerable<ObjectResult> results = new List<ObjectResult>() {
                    userInfo,
                    tweetTimeline,
                    followers,
                    mentions,
                    hashtagCount,
                    searchedHashtags
                    // sentiment
                };

                return Ok(results);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Something went wrong: {ex}");
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        private const string SubscriptionKey = "Put subscription key here!"; // Environment.GetEnvironmentVariable("SUBSCRIPTION_KEY")

        /// </summary>
        class ApiKeyServiceClientCredentials : ServiceClientCredentials
        {
            public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Headers.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);
                return base.ProcessHttpRequestAsync(request, cancellationToken);
            }
        }

        private Dictionary<string, double> GetSentiment(IEnumerable<ITweet> mentions)
        {

            // Create a client.
            ITextAnalyticsClient client = new TextAnalyticsClient(new ApiKeyServiceClientCredentials())
            {
                Endpoint = "https://canadacentral.api.cognitive.microsoft.com"

            }; //Replace 'westus' with the correct region for your Text Analytics subscription

            Console.WriteLine("\n\n===== SENTIMENT ANALYSIS ======");

            List<MultiLanguageInput> rawText = new List<MultiLanguageInput>();

            var i = 0;

            foreach (var mention in mentions)
            {
                Console.WriteLine(i + "====" + mention.FullText);
                MultiLanguageInput text = new MultiLanguageInput("en", i.ToString(), mention.FullText);
                rawText.Add(text);
                i++;
            }

                Console.WriteLine("************************************************************************");

            try
            {

                SentimentBatchResult result3 = client.SentimentAsync(
                    new MultiLanguageBatchInput(rawText)).Result;

                foreach (var document in result3.Documents)
                {
                    Console.WriteLine($"Document ID: {document.Id} , Sentiment Score: {document.Score:0.00}");
                }

            } catch(System.AggregateException e)
            {
                Console.WriteLine(e);
            }


            // Printing sentiment results


            Console.WriteLine("************************************************************************");

            // create empty dictionary
            // for every mention
            // send mention to api
            // add mention and its score to dictionary
            // return dictionary of scores

            /**
             * { "@sm_analytic Some text about our app", 123}
             */

            return new Dictionary<string, double>();
        }

        private Dictionary<string, int> CountHashtags(IEnumerable<ITweet> tweets)
        {

            var hashtags = new Dictionary<string, int>();

            foreach (var tweet in tweets)
            {
                foreach (var hashtag in tweet.Hashtags)
                {
                    int value;

                    if (hashtags.TryGetValue(hashtag.Text, out value)) {
                        hashtags[hashtag.Text]++;
                    }
                    else {
                        hashtags[hashtag.Text] = 1;
                    }
                }  
            }

            return hashtags;

        }

        private Dictionary<string, int> SearchHashtags(Dictionary<string, int> hashtags) {

            var hashtagCount = new Dictionary<string, int>();

            foreach (KeyValuePair<string, int> hashtag in hashtags)
            {

                var searchParameter = new SearchTweetsParameters("#" + hashtag.Key);

                searchParameter.Lang = LanguageFilter.English;
                searchParameter.SearchType = SearchResultType.Popular;

                var tweets = Search.SearchTweets(searchParameter);
                var tweetAmount = new List<ITweet>(tweets).Count;

                hashtagCount[hashtag.Key] = tweetAmount;
            
            }

            return hashtagCount;

        }

        private void AuthorizeOurApp()
        {
            Auth.SetUserCredentials(
                Environment.GetEnvironmentVariable("CONSUMER_KEY"),
                Environment.GetEnvironmentVariable("CONSUMER_SECRET"),
                Environment.GetEnvironmentVariable("ACCESS_TOKEN"),
                Environment.GetEnvironmentVariable("ACCESS_TOKEN_SECRET")
            );
        }

        /*
         * Model for the user credentials passed into
         * ValidateTwitterAuth()
         */
        public class Credentials {
            public string authorization_id { get; set; }
            public string oauth_token { get; set; }
            public string oauth_verifier { get; set; }
        }
    }
}
