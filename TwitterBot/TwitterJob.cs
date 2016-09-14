using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

using LinqToTwitter;

using Quartz;

namespace TwitterBot
{
    internal class TwitterJob :IJob {

        private static readonly SingleUserAuthorizer Auth = new SingleUserAuthorizer
        {
            CredentialStore = new SingleUserInMemoryCredentialStore
            {
                ConsumerKey = ConfigurationManager.AppSettings["consumerKey"],
                ConsumerSecret = ConfigurationManager.AppSettings["consumerSecret"],
                AccessToken = ConfigurationManager.AppSettings["accessToken"],
                AccessTokenSecret = ConfigurationManager.AppSettings["accessTokenSecret"]
            }
        };

        public void Execute(IJobExecutionContext context)
        {
            RunTask();
        }

        public static void RunTask()
        {
            try
            {
//                var answers = new List<string>();
//                foreach (var item in AnswersDict)
//                {
//                    for (var i = 0; i < item.Value; i++)
//                    {
//                        answers.Add(item.Key);
//                    }
//                }

                var twitterCtx = new TwitterContext(Auth);
//                var weekstatuses = twitterCtx.Status
//                        .Where(s => s.Type == StatusType.User
//                                   && s.ScreenName == "visafreealready"
//                                   && s.CreatedAt > DateTime.Now.AddDays(-14)).Select(s => s.Text)
//                        .Distinct().ToList();

//                int answerNumber;
//                do
//                {
//                    answerNumber = Random.Next(answers.Count - 1);
//                    Console.WriteLine("answer: {0}", answers[answerNumber]);
//                }
//                while (weekstatuses.Contains(answers[answerNumber]));

//                var tweet = twitterCtx.TweetAsync(DateTime.Now.ToString()).Result;
                var tweet = twitterCtx.NewDirectMessageAsync("Immelstorn", DateTime.Now.ToString()).Result;
                Console.WriteLine(tweet == null ? "an error occured, tweet is null" : string.Format("tweet.StatusID: {0}", tweet.ID));
            }
            catch (Exception e)
            {
                Console.WriteLine("an error occured: {0}", e.Message);
            }
        }
    }
}