using System;
using System.Collections.Generic;
using System.Threading;

namespace TwitterBot
{
    class Program
    {
        #region vars

        private static API _api = API.GetApi();
        private static List<DateTime> _updateTimes;
        private static Logs _logs = Logs.GetLogsClass();
        private static Thread _thr;
        private static bool _excep;
        private static bool newDay = false;
        #endregion

        static void Main(string[] args)
        {
            _thr = new Thread(MainMethod) { Name = "Main method" };
            _thr.Start();
        }

        /// <summary>
        /// Main после эксепшна
        /// </summary>
        private static void MainAfterExeption()
        {
            _logs.WriteLog("log.txt", "Exception");
            Console.WriteLine("Exception");
            _thr = new Thread(MainMethod) { Name = "Main method" };
            _thr.Start();
        }

        private static void MainMethod()
        {
            try
            {
                while (true)
                {
                    //получили время апдейтов
                    if (!_excep)
                    {
                        _updateTimes = _api.GetUpdateTimes();
                        newDay = false;
                    }

                    while (!newDay)
                    {
                        try
                        {
                            if (_updateTimes.Count != 0)
                            {
                                SleepAndUpdate();
                            }
                            else
                            {
                                SleepUntilTomorrow();

                                ClearingFollowers();

                                if (newDay)
                                {
                                    _excep = false;
                                    break;
                                }
                            }
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            //сюда попадаем если заснуть не удалось т.к. время апдейта уже прошло. тогда просто прибиваем его
                            _updateTimes.RemoveAt(0);
                        }
                    }

                }
            }
            catch (Exception e)
            {
                CatchException(e);
            }
        }

        /// <summary>
        /// Поймали эксепшн
        /// </summary>
        /// <param name="e">The e.</param>
        private static void CatchException(Exception e)
        {
            _logs.WriteLog("errorlog.txt", string.Format("\nMessage: {0}\n TargetSite: {1}\n Data: {2}\n StackTrace: {3}\n InnerException: {4}" +
                                                        "\n Source: {5}\n Data: {6}\n GetBaseException: {7}\n HelpLink: {8}\n",
                e.Message, e.TargetSite, e.Data, e.StackTrace, e.InnerException, e.Source, e.Data, e.GetBaseException(), e.HelpLink));
            Console.WriteLine("{0} in {1}", e.Message, e.TargetSite);
            _excep = true;
            MainAfterExeption();
        }

        /// <summary>
        /// каждую неделю чистим фолловеров
        /// </summary>
        private static void ClearingFollowers()
        {
            _logs.WriteLog("log.txt", "Day of week: " + DateTime.Now.DayOfWeek + ", Is Sunday: " + (DateTime.Now.DayOfWeek == DayOfWeek.Sunday));
            if (DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
            {
                Console.WriteLine("Clearing followings:");
                _logs.WriteLog("log.txt", "Clearing followings:");
                _api.ClearFollowings();
            }
        }

        /// <summary>
        ///это последний раз на сегодня, то спим до наступления следующего дня
        /// </summary>
        private static void SleepUntilTomorrow()
        {
            Console.WriteLine(DateTime.Now + " ==>" + " Sleeping until 00:01\n");
            _logs.WriteLog("log.txt", "Sleeping until 00:01\n");
            Thread.Sleep(DateTime.Parse("23:59") - DateTime.Now + TimeSpan.FromMinutes(2));
            newDay = true;
            _logs.WriteLog("log.txt", "newDay =" + newDay);
        }

        /// <summary>
        /// попробовали заснуть на время до следующего апдейта
        /// </summary>
        private static void SleepAndUpdate()
        {
            _logs.WriteLog("log.txt", "Sleeping until " + _updateTimes[0] + "\n");
            Console.WriteLine("Sleeping until " + _updateTimes[0] + "\n");
            Thread.Sleep(_updateTimes[0] - DateTime.Now);
            Updates();
            _updateTimes.RemoveAt(0);
            _excep = false;
        }

        /// <summary>
        /// апдейтим
        /// </summary>
        private static void Updates()
        {
            //апдейтили статус
            try
            {
                _api.UpdateStatus(_api.StatusToPost());
            }
            catch (Exception e)
            {
                CatchExceptionWhileUpdate(e);
            }

            //фолловим по 5 человек три раза в день
            try
            {
                _logs.WriteLog("log.txt", "Follow suggested users:");
                Console.WriteLine("Follow suggested users:");
                _api.Follow(_api.UsersToFollow());
            }
            catch (Exception e)
            {
                CatchExceptionWhileUpdate(e);
            }

            //фолловим всех кто отреплаил
            try
            {
                _logs.WriteLog("log.txt", "Follow who replied me:");
                Console.WriteLine("Follow who replied me:");
                _api.Follow(_api.MentionsToMe());
            }
            catch (Exception e)
            {
                CatchExceptionWhileUpdate(e);
            }

            //фолловим всех кто отретвитил
            try
            {
                _logs.WriteLog("log.txt", "Follow who retweeted me:");
                Console.WriteLine("Follow who retweeted me:");
                _api.Follow(_api.RetweetsOfMe());
            }
            catch (Exception e)
            {
                CatchExceptionWhileUpdate(e);
            }

            //фолловим всех кто зафоловил
            try
            {
                _logs.WriteLog("log.txt", "Follow who followed me:");
                Console.WriteLine("Follow who followed me:");
                _api.Follow(_api.WhoFollowedMe());
            }
            catch (Exception e)
            {
                CatchExceptionWhileUpdate(e);
            }
        }

        /// <summary>
        /// Catches the exception while update.
        /// </summary>
        /// <param name="e">The e.</param>
        private static void CatchExceptionWhileUpdate(Exception e)
        {
            _logs.WriteLog("errorlog.txt", string.Format("\nMessage: {0}\n TargetSite: {1}\n Data: {2}\n StackTrace: {3}\n InnerException: {4}" +
                                                    "\n Source: {5}\n Data: {6}\n GetBaseException: {7}\n HelpLink: {8}\n",
            e.Message, e.TargetSite, e.Data, e.StackTrace, e.InnerException, e.Source, e.Data, e.GetBaseException(), e.HelpLink));
            Console.WriteLine("{0} in {1}", e.Message, e.TargetSite);
        }
    }
}
