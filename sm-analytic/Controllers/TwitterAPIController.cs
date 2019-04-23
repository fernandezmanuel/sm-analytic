﻿using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;


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
                var sentimentData = GetSentimentRank(user.GetMentionsTimeline());
                var allMentionsUsed = CountMentions(user.GetMentionsTimeline());
                var allMentionCreators = GetMentionCreatorName(user.GetMentionsTimeline());
                var mentList = GetSentimentList(user.GetMentionsTimeline());

                ObjectResult userInfo = new ObjectResult(user);
                ObjectResult tweetTimeline = new ObjectResult(user.GetUserTimeline());
                ObjectResult followers = new ObjectResult(user.GetFollowers());
                ObjectResult mentions = new ObjectResult(user.GetMentionsTimeline());
                ObjectResult sentiment = new ObjectResult(sentimentData);
                ObjectResult mentionCreatedBy = new ObjectResult(allMentionCreators);
                ObjectResult hashtagCount = new ObjectResult(allHashtagsUsed);
                ObjectResult searchedHashtags = new ObjectResult(publicPostsWithHashtags);
                ObjectResult mentionList = new ObjectResult(mentList);          

                IEnumerable<ObjectResult> results = new List<ObjectResult>() {
                    userInfo,
                    tweetTimeline,
                    followers,
                    mentions,
                    hashtagCount,
                    searchedHashtags,
                    sentiment,
                    mentionList,
                    mentionCreatedBy,
                    mentions
                };

                return Ok(results);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Something went wrong: {ex}");
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        private const string SubscriptionKey = "82734afb721d44fd81a2e6cfdb610852"; // Environment.GetEnvironmentVariable("SUBSCRIPTION_KEY")

        /// </summary>
        class ApiKeyServiceClientCredentials : ServiceClientCredentials
        {
            public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Headers.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);
                return base.ProcessHttpRequestAsync(request, cancellationToken);
            }
        }

        private Dictionary<int, string> GetSentimentList(IEnumerable<ITweet> mentions)
        {
            Console.WriteLine("\n\n===== Mentions List ======");

            var ment = new Dictionary<int, string>();

            int i = 0;

            Console.WriteLine("************************************************************************");
            try
            {

                foreach (var mention in mentions)
                {
                    ment.Add(i, mention.FullText);
                    i++;
                }
            }
            catch (System.AggregateException e)
            {
                Console.WriteLine(e);
            }

            Console.WriteLine("************************************************************************");

            return ment;
        }


        private Dictionary<string, double> GetSentimentRank(IEnumerable<ITweet> mentions)
        {
            // Create a client.
            ITextAnalyticsClient client = new TextAnalyticsClient(new ApiKeyServiceClientCredentials())
            {
                Endpoint = "https://canadacentral.api.cognitive.microsoft.com"

            }; //Replace 'westus' with the correct region for your Text Analytics subscription

            Console.WriteLine("\n\n===== SENTIMENT ANALYSIS ======");

            List<MultiLanguageInput> rawText = new List<MultiLanguageInput>();

            var sentiment = new Dictionary<string, double>();

            var i = 0;

            foreach (var mention in mentions)
            {
                Console.WriteLine(i + "====" + mention.CreatedBy + "=====" + mention.FullText);
                MultiLanguageInput text = new MultiLanguageInput("en", i.ToString(), mention.FullText);
                rawText.Add(text);
                i++;
            }

                Console.WriteLine("************************************************************************");
            try
            {
                SentimentBatchResult result3 = client.SentimentAsync( new MultiLanguageBatchInput(rawText)).Result;
   
                foreach (var document in result3.Documents)
                {
                        // MultiLanguageInput text = new MultiLanguageInput("en", i.ToString(), mention.FullText);
                        double facerank = (double)(document.Score * 100);
                        // var mentionText = mentList[Int32.Parse(document.Id)];
                        sentiment.Add(document.Id, facerank);
                        Console.WriteLine($"Document ID: {document.Id} , Sentiment Score: {facerank:0.00}%");
                }

            } catch(System.AggregateException e)
            {
                Console.WriteLine(e);
            }

            // Printing sentiment results
            Console.WriteLine("************************************************************************");

            return sentiment;
        }

        private List<string> GetMentionCreatorName(IEnumerable<ITweet> mentions)
        {
            List<string> createdBy = new List<string>();

            Console.WriteLine("\n\n********************************************************************");
            Console.WriteLine("*****************GETTING MENTION CREATOR NAME***************************");

            foreach (var mention in mentions)
            {
                createdBy.Add((mention.CreatedBy).ToString());
            }

            foreach (var name in createdBy)
            {
                Console.WriteLine(name);
            }

            Console.WriteLine("\n\n********************************************************************");

            return createdBy;
        }

        private int CountMentions(IEnumerable<ITweet> mentions)
        {
            int mention = 0;
            foreach(var count in mentions)
            {
                mention++;
            }
           
            Console.WriteLine("\n\n************************************************************************");
            Console.WriteLine("*******************COUNTING TOTAL NUMBER OF MENTIONS**************************");
            Console.WriteLine(mention);
            Console.WriteLine("************************************************************************");

            return mention;
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
