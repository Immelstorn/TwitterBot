using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Twitterizer;

namespace TwitterBot
{
	class API
    {
        #region vars

        private readonly OAuthTokens _tokens = new OAuthTokens();
		private static API _instance;
		private static object _syncLock = new object();
		private readonly Logs _logs;
		private readonly Random _random = new Random();
		private int _remainingLimit;
		private int _usersToFollow = 50;

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

        private API()
		{
			_tokens.AccessToken = "";
			_tokens.AccessTokenSecret = "";
			_tokens.ConsumerKey = "";
			_tokens.ConsumerSecret = "";
			var apiStatus = TwitterRateLimitStatus.GetStatus(_tokens);
			_remainingLimit = apiStatus.ResponseObject.RemainingHits;
		}

		public static API GetApi()
		{
			if (_instance == null)
			{
				lock (_syncLock)
				{
					if (_instance == null)
					{
						_instance = new API();
					}
				}
			}
			return _instance;
		}

		/// <summary>
		/// Retweets  of me.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<decimal> RetweetsOfMe()
		{
			var users = new List<decimal>();
			//получить список юзеров которые меня ретвитнули
			Limiter();
			var options = new RetweetsOfMeOptions { Count = 25 };

			//получаем список ретвитнутых твитов
			Limiter();
			var retweetsOfMe = TwitterTimeline.RetweetsOfMe(_tokens, options);

			//смотрим кто ретвитнул этот твит и добавляем его в список юзеров
			foreach (var item in retweetsOfMe.ResponseObject)
			{
				Limiter();
				var thisTweetRetweets = TwitterStatus.Retweets(_tokens, item.Id);
				users.AddRange(thisTweetRetweets.ResponseObject.Select(rt => rt.User.Id));
			}
			return users;
		}

		/// <summary>
		/// Mentions to me.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<decimal> MentionsToMe()
		{
			var users = new List<decimal>();

			//получить список юзеров которые мне написали
			Limiter();
			var mentionsToMe = TwitterTimeline.Mentions(_tokens);
			if (mentionsToMe.ResponseObject != null)
			{
				users.AddRange(mentionsToMe.ResponseObject.Select(item => item.User.Id));
			}
			return users;
		}

		/// <summary>
		/// Users to follow.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<decimal> UsersToFollow()
		{
			var users = new List<decimal>();
			var myfollowers = new List<decimal>();

			//получаем список из случайных юзеров, которых фолловят мои фолловеры. _usersToFollow штук.
			Limiter();
			var user = TwitterUser.Show("svittex");
			if (user.ResponseObject != null)
			{
				var options = new UsersIdsOptions { ScreenName = "svittex" };
				Limiter();
				var followersResponse = TwitterFriendship.FollowersIds(_tokens, options);
				var random = new Random();

				//choose _usersToFollow of my followers
				for (var i = 0; i < _usersToFollow; i++)
				{
					myfollowers.Add(followersResponse.ResponseObject[random.Next(followersResponse.ResponseObject.Count)]);
				}

				//random choose one following of each of my followers chosen
				foreach (var item in myfollowers)
				{
					Limiter();
					options.ScreenName = TwitterUser.Show(item).ResponseObject.ScreenName;
					Limiter();
					followersResponse = TwitterFriendship.FriendsIds(_tokens, options);
					var id = followersResponse.ResponseObject[random.Next(followersResponse.ResponseObject.Count)];
					users.Add(id);
				}
			}
			return users;
		}

		/// <summary>
		/// Whoes the followed me.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<decimal> WhoFollowedMe()
		{
			//получаем список юзеров которые меня зафолловили
			var options = new UsersIdsOptions { ScreenName = "" };
			Limiter();
			var followersResponse = TwitterFriendship.FollowersIds(_tokens, options);
			return followersResponse.ResponseObject.ToList();
		}

		/// <summary>
		/// Follows the specified users.
		/// </summary>
		/// <param name="users">The users.</param>
		public void Follow(IEnumerable<decimal> users, bool whoFollowedMe = false)
		{
			var count = 0;

			//фолловим юзеров из переданного списка
			Limiter();

			//взяли моих фолловингов
			var iFollow = TwitterFriendship.FriendsIds(_tokens);

			//проверяли ли фолловлю ли я юзера из переданного списка и нет ли его в черном списке, если нет - фолловить
			var blacklist = File.ReadAllLines("blacklist.txt");

			//если пришли из whoFollowedMe то фолловим не оглядываясь на черный список
			if (whoFollowedMe)
			{
				foreach (var item in users)
				{
					if (!(iFollow.ResponseObject.Contains<decimal>(item)))
					{
						Limiter();
						TwitterFriendship.Create(_tokens, item);
						count++;
					}
				}
			}
			else
			{
				foreach (var item in users)
				{
					if (!(iFollow.ResponseObject.Contains<decimal>(item)) && !(blacklist.Contains<string>(item.ToString())))
					{
						Limiter();
						TwitterFriendship.Create(_tokens, item);
						count++;
					}
				}
			}


			Console.WriteLine("{0} => {1}", DateTime.Now, "Followed " + count + " people.");
			_logs.WriteLog("Followed " + count + " people.");
		}

		/// <summary>
		/// Updates the status.
		/// </summary>
		/// <param name="status">The status.</param>
		public void UpdateStatus(string status)
		{
			//апдейтим статус переданный в аргументе
			Limiter();
			TwitterStatus.Update(_tokens, status);
			_logs.WriteStatusLog(status);
			_logs.WriteLog("Updating status => " + status);
			Console.WriteLine("{0} => {1}", DateTime.Now, status);
		}

		/// <summary>
		/// Searches for status.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <returns></returns>
		private List<string> SearchForStatus(string query)
		{
			var validStatuses = new List<string>();

			if (query != null)
			{
				//ищем список статусов чтоб запостить
				var options = new SearchOptions { ResultType = SearchOptionsResultType.Mixed, NumberPerPage = 50 };
				Limiter();
				var tweetSearch = TwitterSearch.Search(_tokens, "\"я " + query + "\"", options);
				_logs.WriteLog("Ищем статус по слову " + query);
				_logs.WriteLog("Всего статусов: " + tweetSearch.ResponseObject.Count);

				//фильтруем реплаи, ссылки и хэштеги
			    validStatuses.AddRange(tweetSearch.ResponseObject
			                    .Where(item => !(item.Text.Contains('@') || item.Text.Contains('#') || item.Text.Contains("http")))
			                    .Select(item => item.Text));
			    _logs.WriteLog("Валидных статусов: " + validStatuses.Count);
			}
			else
			{
				_logs.WriteLog("query is NULL!");

			}
			return validStatuses;
		}

		/// <summary>
		/// Statuses to post.
		/// </summary>
		/// <returns></returns>
		public string StatusToPost()
		{
			//выбираем статус для постинга

			//получили список фраз для поиска
		    var wordsForSearch = _wordsToSearch.GetRange(0, _wordsToSearch.Count);
			var validStatuses = new List<string>();
			var previousStatuses = File.ReadAllLines("StatusLog.txt");
			var isNew = false;
			var validAndNotPosted = new List<string>();
			while (!isNew)
			{
				while (validStatuses.Count == 0)
				{
					//ищем случайную фразу пока не получим больше 0 статусов

					var r = _random.Next(wordsForSearch.Count - 1);
					validStatuses = SearchForStatus(wordsForSearch[r]);
					wordsForSearch.RemoveAt(r);
				}

				var temp = new string[validStatuses.Count];
				validStatuses.CopyTo(temp);
				foreach (var t in temp)
				{
					//проверяем, не постилось ли раньше
					if (!previousStatuses.Contains(t))
					{
						validAndNotPosted.Add(t);
						isNew = true;
					}
					validStatuses.Remove(t);
				}
			}
			_logs.WriteLog("Валидных новых статусов: " + validAndNotPosted.Count);

			return validAndNotPosted[_random.Next(validAndNotPosted.Count)];
		}

		/// <summary>
		/// Gets the update times.
		/// </summary>
		/// <returns></returns>
		public List<DateTime> GetUpdateTimes()
		{
			//получаем 5 точек апдейта статуса с 10.00 до 23.00
			var updateTimes = new List<DateTime>();
			var day = TimeSpan.FromHours(13);
			for (var i = 0; i < 5; i++)
			{
				var rnd = _random.Next(Convert.ToInt32(day.TotalSeconds));
				updateTimes.Add(DateTime.Parse("10:00") + TimeSpan.FromSeconds(rnd));
			}
			updateTimes.Sort();
			return updateTimes;
		}

		private void Limiter()
		{
			if (_remainingLimit == 0)
			{
				//если сюда попали, значит локальный счетчик обнулился. надо проверить действительно ли закончился лимит.
				var apiStatus = TwitterRateLimitStatus.GetStatus(_tokens);
				if (apiStatus.ResponseObject.RemainingHits == 0)
				{
					//если сюда попали, значит лимит действительно закончился. спим.
					Console.WriteLine("Rate limit exceeded, sleeping until " + apiStatus.ResponseObject.ResetTime);
					_logs.WriteLog("Rate limit exceeded, sleeping until " + apiStatus.ResponseObject.ResetTime);
					Thread.Sleep(apiStatus.ResponseObject.ResetTime - DateTime.Now);

					//проснулись. опять пощупали лимит, записали в переменную
					apiStatus = TwitterRateLimitStatus.GetStatus(_tokens);
					_remainingLimit = apiStatus.ResponseObject.RemainingHits;
					while (_remainingLimit == 0)
					{
						//лимит все еще не обновился. спим, но каждую минуту проверяем
						_logs.WriteLog("Лимит не обновился. Спим одну минуту");
						Thread.Sleep(TimeSpan.FromMinutes(1));
						apiStatus = TwitterRateLimitStatus.GetStatus(_tokens);
						_remainingLimit = apiStatus.ResponseObject.RemainingHits;
					}
				}
				else
				{
					//если тут, значит был сбой и просто приводим переменную в соответствие с реальным положением дел
					_remainingLimit = apiStatus.ResponseObject.RemainingHits;
				}
			}
			_remainingLimit -= 1;
		}

		/// <summary>
		/// Clears the followings.
		/// </summary>
		public void ClearFollowings()
		{
			try
			{

				//тупо сравниваем два списка - фолловерс и фолловинг, если кого-то из второго списка нет в первом - нахуй пошел в черный список.
				var count = 0;
				Limiter();
				var followers = TwitterFriendship.FollowersIds(_tokens);
				Limiter();
				var iFollow = TwitterFriendship.FriendsIds(_tokens);
				foreach (var item in iFollow.ResponseObject)
				{
					if (!(followers.ResponseObject.Contains<decimal>(item)))
					{
						Limiter();
						TwitterFriendship.Delete(_tokens, item);
						_logs.WriteBlackList(item);
						count++;
					}
				}
				_logs.WriteLog("Unfollowed " + count + " users.");
				Console.WriteLine("{0} => {1}", DateTime.Now, "Unfollowed " + count + " users.");
			}
			catch (Exception e)
			{
				_logs.WriteLog("Exception was thrown.");
				_logs.WriteErrorLog(e);

				throw;
			}
		}
	}
}
