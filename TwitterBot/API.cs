using System;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;

using LinqToTwitter;

using TwitterBot.DB;
using TwitterBot.DB.Entities;

namespace TwitterBot
{
	class API
    {
        #region vars
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
        private const string Username = "svittex";
        private const string FollowersLimitName = "/followers/ids";
        private const string FriendsLimitName = "/friends/ids";
        private const string SearchTweetsLimitName = "/search/tweets";
//        private const string DestroyFriendshipLimitName = "/friendships/destroy";
//        private const string CreateFriendshipLimitName = "/friendships/create";
//        private const string TweetLimitName = "/statuses/update/new";
        private const string RetweetsOfMeLimitName = "/statuses/retweets_of_me";
        private const string RetweetsLimitName = "/statuses/retweets/:id";
        private const string MentionsLimitName = "/statuses/mentions_timeline";


        private Dictionary<FriendshipType, List<ulong>> _friendshipCache = new Dictionary<FriendshipType, List<ulong>>();
        private Dictionary<string, int> _limitCache = new Dictionary<string, int>();


        private readonly TwitterContext _twitterCtx;
        private readonly Logs _logs = new Logs();
		private readonly Random _random = new Random();
        private const int _usersToFollow = 5;

        private readonly List<string> _wordsToSearch = new List<string> {
            "я закрыла",
            "мои сиськи",
            "моими сиськами",
            "я собиралась",
            "я решила",
            "я предлагаю",
            "я люблю",
            "я ебала",
            "я вздохнула",
            "я заплатила",
            "я купила",
            "секс",
            "мой муж",
            "мой мужик",
            "я улыбаюсь",
            "я сняла",
            "я привыкла",
            "любимые",
            "моя юбка",
            "оргазм",
            "я плакала",
            "я узнала",
            "я решила",
        };
        #endregion

        public API()
        {
            _twitterCtx = new TwitterContext(Auth);
        }

        public void UpdateStatus()
        {
            try
            {
                var status = StatusToPost();
                //            Limiter(TweetLimitName);
                var tweet = _twitterCtx.TweetAsync(status).Result;
//                var tweet = _twitterCtx.NewDirectMessageAsync("Immelstorn", status).Result;
                if (tweet != null)
                {
                    _logs.WriteStatusLog(status);
                    _logs.WriteLog($"Tweet sent: {tweet.StatusID}");
                }
                else
                {
                    _logs.WriteLog($"Tweet is null,");
                }
            }
            catch (Exception e)
            {
                _logs.WriteLog($"UpdateStatus Failed. Exception was thrown.");
                _logs.WriteErrorLog(e);
            }
        }

        public void ClearCache()
        {
           _friendshipCache = new Dictionary<FriendshipType, List<ulong>>();
            _limitCache = new Dictionary<string, int>();
        }

        private List<ulong> GetFriendship(FriendshipType type)
        {
            if(!_friendshipCache.ContainsKey(type) || _friendshipCache[type] == null || _friendshipCache[type].Count == 0)
            {
                var followers = new List<ulong>();
                long cursor = -1;
                do
                {
                    Limiter(type == FriendshipType.FriendIDs ? FriendsLimitName : FollowersLimitName);
                    var friendship = _twitterCtx.Friendship.SingleOrDefault(f => f.Type == type && f.ScreenName == Username && f.Cursor == cursor);
                    if(friendship?.IDInfo?.IDs != null)
                    {
                        followers.AddRange(friendship.IDInfo.IDs);
                        cursor = friendship.CursorMovement.Next;
                    }
                }
                while(cursor != 0);

                _friendshipCache[type] = followers;
            }

            return _friendshipCache[type];
        }

        public void ClearFollowings()
        {
            if (DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
            {
                try
                {
                    var followers = GetFriendship(FriendshipType.FollowerIDs);
                    var followings = GetFriendship(FriendshipType.FriendIDs);
                    var count = 0;

                    foreach (var id in followings)
                    {
                        if (!followers.Contains(id))
                        {
//                            Limiter(DestroyFriendshipLimitName);
                            var user = _twitterCtx.DestroyFriendshipAsync(id).Result;
                            if (user?.Status != null)
                            {
                                _logs.WriteBlackList(id);
                                count++;
#if DEBUG
                                Console.WriteLine(count);
#endif
                            }
                            else
                            {
                                _logs.WriteLog($"An error during unfollowing. Returned user is null.");
                            }
                        }
                    }

                    _logs.WriteLog($"Unfollowed {count} users");
                }
                catch (Exception e)
                {
                    _logs.WriteLog($"ClearFollowings Failed. Exception was thrown.");
                    _logs.WriteErrorLog(e);
                }
            }
        }

        public string StatusToPost()
        {
            List<string> previousStatuses;
            using(var db = new TwitterBotContext())
            {
                previousStatuses = db.StatusLogs.Select(s => s.Message).ToList();
            }

            var wordsForSearch = _wordsToSearch.GetRange(0, _wordsToSearch.Count);


            var validStatuses = new List<string>();
            var isNew = false;
            var validAndNotPosted = new List<string>();

            while(!isNew)
            {
                while(validStatuses.Count == 0)
                {
                    var r = _random.Next(wordsForSearch.Count - 1);
                    validStatuses = SearchForStatus(wordsForSearch[r]);
                    wordsForSearch.RemoveAt(r);
                }

                var temp = new string[validStatuses.Count];
                validStatuses.CopyTo(temp);
                foreach(var t in temp)
                {
                    if(!previousStatuses.Contains(t))
                    {
                        validAndNotPosted.Add(t);
                        isNew = true;
                    }
                    validStatuses.Remove(t);
                }
            }
            _logs.WriteLog($"Valid and not posted count: {validAndNotPosted.Count}");

            return validAndNotPosted[_random.Next(validAndNotPosted.Count - 1)];
        }

        private List<string> SearchForStatus(string query)
        {
            var validStatuses = new List<string>();
            _logs.WriteLog($"Searching for status: {query}");
            Limiter(SearchTweetsLimitName);
            var searchResponse = _twitterCtx.Search.SingleOrDefault(s => s.Type == SearchType.Search
                                                                        && s.ResultType == ResultType.Mixed
                                                                        && s.Query == query);
            if (searchResponse?.Statuses != null)
            {
                _logs.WriteLog($"Found {searchResponse.Statuses.Count} statuses");
                validStatuses.AddRange(searchResponse.Statuses
                                               .Where(item => !(item.Text.Contains('@') || item.Text.Contains('#') || item.Text.Contains("http")))
                                               .Select(item => item.Text));
                _logs.WriteLog("Valid " + validStatuses.Count);
            }

            return validStatuses;
        }

        public void Follow(List<ulong> users, bool skipBlackList = true)
        {
            if(users.Any())
            {
                try
                {
                    var followings = GetFriendship(FriendshipType.FriendIDs);

                    List<long> blacklist;
                    using (var db = new TwitterBotContext())
                    {
                        blacklist = db.BlackLists.Select(s => s.UserId).ToList();
                    }

                    //если пришли из whoFollowedMe то фолловим не оглядываясь на черный список
                    var usersToFollow = users.Where(item => !followings.Contains(item));

                    if (!skipBlackList)
                    {
                        usersToFollow = usersToFollow.Where(item => !blacklist.Contains((long)item)).ToList();
                    }

                    FollowUsers(usersToFollow.Distinct().Take(50).ToList());
                }
                catch (Exception e)
                {
                    _logs.WriteLog($"Follow Failed. Exception was thrown.");
                    _logs.WriteErrorLog(e);
                }
            }
        }

        private void FollowUsers(List<ulong> usersToFollow)
        {
            var outgoing = _twitterCtx.Friendship.SingleOrDefault(req => req.Type == FriendshipType.Outgoing);
            if(outgoing?.IDInfo?.IDs != null)
            {
                usersToFollow = usersToFollow.Except(outgoing.IDInfo.IDs).ToList();
            }

            if(usersToFollow.Any())
            {
                var users = _twitterCtx.User.Where(u => u.Type == UserType.Lookup && u.UserIdList == string.Join(",", usersToFollow)).ToList();

                foreach (var user in users)
                {
                    var result = _twitterCtx.CreateFriendshipAsync(ulong.Parse(user.UserIDResponse), false).Result;
                    if (result?.Status == null)
                    {
                        _logs.WriteLog($"An error during following. Returned user is null.");
                    }
                }
                _logs.WriteLog($"Followed {usersToFollow.Count()} users");
            }
        }

        public List<ulong> UsersToFollow()
        {
            try
            {
                var users = new List<ulong>();
                var myfollowers = new List<ulong>();

                //получаем список из случайных юзеров, которых фолловят мои фолловеры. _usersToFollow штук.
                var followers = GetFriendship(FriendshipType.FollowerIDs);

                for (var i = 0; i < _usersToFollow; i++)
                {
                    myfollowers.Add(followers[_random.Next(followers.Count)]);
                }

                //random choose one following of each of my followers chosen
                foreach (var item in myfollowers)
                {
                    Limiter(FriendsLimitName);
                    var friendship = _twitterCtx.Friendship.SingleOrDefault(f => f.Type == FriendshipType.FriendIDs && f.UserID == item.ToString());
                    if (friendship?.IDInfo?.IDs != null)
                    {
                        users.Add(friendship.IDInfo.IDs[_random.Next(friendship.IDInfo.IDs.Count)]);
                    }
                }
                return users;
            }
            catch (Exception e)
            {
                _logs.WriteLog($"UsersToFollow Failed. Exception was thrown.");
                _logs.WriteErrorLog(e);
            }
            return new List<ulong>();
        }

        public List<ulong> RetweetsOfMe()
        {
            var users = new List<ulong>();
            try
            {
                _logs.WriteLog($"Retweets of Me");
                Limiter(RetweetsOfMeLimitName); 
                var retweetsOfMe = _twitterCtx.Status.Where(s => s.Type == StatusType.RetweetsOfMe && s.Count == 10).ToList();
                foreach(var tweet in retweetsOfMe)
                {
                    Limiter(RetweetsLimitName);
                    var retweet = _twitterCtx.Status.Where(t => t.Type == StatusType.Retweets && t.ID == tweet.StatusID).ToList();
                    users.AddRange(retweet.Select(r => ulong.Parse(r.User.UserIDResponse)).ToList());
                }
            }
            catch (Exception e)
            {
                _logs.WriteLog($"ClearFollowings Failed. Exception was thrown.");
                _logs.WriteErrorLog(e);
            }
            return users;
        }

        public List<ulong> MentionsToMe()
        {
            try
            {
                _logs.WriteLog($"Mentions to Me");
                Limiter(MentionsLimitName); 
                var tweets = _twitterCtx.Status.Where(tweet => tweet.Type == StatusType.Mentions && tweet.ScreenName == Username).ToList();
                return tweets.Select(item => ulong.Parse(item.User.UserIDResponse)).ToList();
            }
            catch (Exception e)
            {
                _logs.WriteLog($"MentionsToMe Failed. Exception was thrown.");
                _logs.WriteErrorLog(e);
            }
            return new List<ulong>();

        }

        public List<ulong> WhoFollowedMe()
        {
            try
            {
                _logs.WriteLog($"Who Followed Me");
                var followers = GetFriendship(FriendshipType.FollowerIDs);
                var followings = GetFriendship(FriendshipType.FriendIDs);
                return followers.Except(followings).ToList();
            }
            catch (Exception e)
            {
                _logs.WriteLog($"WhoFollowedMe Failed. Exception was thrown.");
                _logs.WriteErrorLog(e);
            }
            return new List<ulong>();
        }

        private void Limiter(string limitName)
        {
//            _logs.WriteLog($"looking for limit for {limitName}");
            if(!_limitCache.ContainsKey(limitName) || _limitCache[limitName] == 0)
            {
                var limit = _twitterCtx.Help.Where(h => h.Type == HelpType.RateLimits).ToList();
                var limitToCache = limit[0].RateLimits.SelectMany(l => l.Value).FirstOrDefault(l => l.Resource.Equals(limitName));
                if(limitToCache != null)
                {
                    _limitCache[limitName] = limitToCache.Remaining;
                    _logs.WriteLog($"limit from web for {limitName} is {limitToCache.Remaining}");
                    if (_limitCache[limitName] == 0)
                    {
                        _logs.WriteLog($"limit for {limitName} is 0");
                        var _limitReset = limit.Select(l => l.RateLimits["application"][0].Reset).FirstOrDefault();
                        var resetTime = FromUnixTime(_limitReset);
                        var timeToSleep = resetTime - DateTime.UtcNow;
                        _logs.WriteLog($"sleeping for {timeToSleep}");
                        Thread.Sleep(timeToSleep);
                        _logs.WriteLog("wake up");
                        Limiter(limitName);
                    }
                }
                else
                {
                    _logs.WriteLog($"limitToCache is null for limit {limitName}");
                }
            }
//            _logs.WriteLog($"limit from cache for {limitName} is {_limitCache[limitName]}. Reducing.");
            _limitCache[limitName]--;
        }

        public DateTime FromUnixTime(ulong unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTime);
        }

	    public void WriteStats()
	    {
            var followers = GetFriendship(FriendshipType.FollowerIDs);
            var followings = GetFriendship(FriendshipType.FriendIDs);

	        var stat = new Statistic {
	            Followers = followers.Count,
                Followings = followings.Count
	        };

	    }
    }
}