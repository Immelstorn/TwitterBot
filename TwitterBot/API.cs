using System;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;

using LinqToTwitter;

using TwitterBot.DB;

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
        private readonly TwitterContext _twitterCtx;
        private Dictionary<FriendshipType, List<ulong>> _friendshipCache = new Dictionary<FriendshipType, List<ulong>>();
        private int _limitCache = 0;
		private readonly Logs _logs = new Logs();
		private readonly Random _random = new Random();
        private const int _usersToFollow = 15;

        private List<string> _wordsToSearch = new List<string> {
            "закрыла",
            "мои сиськи",
            "моими сиськами",
            "собиралась",
            "решила",
            "предлагаю",
            "люблю",
            "ебала",
            "вздохнула",
            "заплатила",
            "купила",
            "секс",
            "муж",
            "мой мужик",
            "улыбаюсь",
            "сняла",
            "привыкла",
            "любимые",
            "охуенно",
            "юбка",
            "ПМС",
            "оргазм",
            "плкакать",
            "узнала",
        };
        #endregion

        public API()
        {
            _twitterCtx = new TwitterContext(Auth);
        }

        public void UpdateStatus()
        {
            var status = StatusToPost();
            //var tweet = twitterCtx.TweetAsync(status).Result;
            Limiter();
            var tweet = _twitterCtx.NewDirectMessageAsync("Immelstorn", status).Result;
            if (tweet != null)
            {
                _logs.WriteStatusLog(status);
                _logs.WriteLog($"Tweet sent: {tweet.ID}");
            }
            else
            {
                _logs.WriteLog($"Tweet is null, limit is {_limitCache}");
            }
        }

        public void ClearCache()
        {
           _friendshipCache = new Dictionary<FriendshipType, List<ulong>>();
            _limitCache = 0;
        }

        private List<ulong> GetFriendship(FriendshipType type)
        {
            if(!_friendshipCache.ContainsKey(type) || _friendshipCache[type] == null || _friendshipCache[type].Count == 0)
            {
                var followers = new List<ulong>();
                long cursor = -1;
                do
                {
                    Limiter();
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
                            Limiter();
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
                                _logs.WriteLog($"An error during unfollowing. Returned user is null. Limit is {_limitCache}");
                            }
                        }
                    }

                    _logs.WriteLog($"Unfollowed {count} users");
                }
                catch (Exception e)
                {
                    _logs.WriteLog($"Exception was thrown. Limit is {_limitCache}");
                    _logs.WriteErrorLog(e);

                    throw;
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
            Limiter();
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

        public void Follow(IEnumerable<ulong> users, bool whoFollowedMe = false)
        {
            var followings = GetFriendship(FriendshipType.FriendIDs);

            List<long> blacklist;
            using (var db = new TwitterBotContext())
            {
                blacklist = db.BlackLists.Select(s => s.UserId).ToList();
            }

            //если пришли из whoFollowedMe то фолловим не оглядываясь на черный список
            var usersToFollow = users.Where(item => !followings.Contains(item));

            if (!whoFollowedMe)
            {
                usersToFollow = usersToFollow.Where(item => !blacklist.Contains((long)item)).ToList();
            }

            FollowUsers(usersToFollow);
        }

        private void FollowUsers(IEnumerable<ulong> usersToFollow)
        {
            foreach (var user in usersToFollow)
            {
                Limiter();
                var result = _twitterCtx.CreateFriendshipAsync(user, false).Result;
                if (result?.Status == null)
                {
                    _logs.WriteLog($"An error during following. Returned user is null. Limit is {_limitCache}");
                }
            }
            _logs.WriteLog($"Followed {usersToFollow} users");
        }

        public IEnumerable<ulong> UsersToFollow()
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
                var friendship = _twitterCtx.Friendship.SingleOrDefault(f => f.Type == FriendshipType.FriendIDs && f.UserID == item.ToString());
                if (friendship?.IDInfo?.IDs != null)
                {
                    users.Add(friendship.IDInfo.IDs[_random.Next(friendship.IDInfo.IDs.Count)]);
                }
            }
            return users;
        }

        public IEnumerable<ulong> RetweetsOfMe()
        {
            Limiter();
            var retweetsOfMe = _twitterCtx.Status.Where(s => s.Type == StatusType.RetweetsOfMe && s.Count == 25).ToList();
            return retweetsOfMe.Select(item => item.UserID).ToList();
        }

        public IEnumerable<ulong> MentionsToMe()
        {
            Limiter();
            var tweets = _twitterCtx.Status.Where(tweet => tweet.Type == StatusType.Mentions && tweet.ScreenName == Username).ToList();
            return tweets.Select(item => item.UserID);
        }

        public IEnumerable<ulong> WhoFollowedMe()
        {
            var followers = GetFriendship(FriendshipType.FollowerIDs);
            var followings = GetFriendship(FriendshipType.FriendIDs);
            return followers.Except(followings);
        }

        private void Limiter()
        {
            if (_limitCache == 0)
            {
                var limit = _twitterCtx.Help.Where(h => h.Type == HelpType.RateLimits).ToList();
                _limitCache = limit.Select(l => l.RateLimits["application"][0].Remaining).FirstOrDefault();
                if (_limitCache == 0)
                {
                    var _limitReset = limit.Select(l => l.RateLimits["application"][0].Reset).FirstOrDefault();
                    //TODO: if limit is 0 I have to wait.
                }
            }

            
        }
	}
}
