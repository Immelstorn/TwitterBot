using System;
using System.Collections.Generic;
using System.Threading;

using TwitterBot.Jobs;

namespace TwitterBot
{
    class Program
    {
        #region vars

        private static readonly API _api ;//= API.GetApi();
        private static List<DateTime> _updateTimes;
        private static readonly Logs _logs =new Logs();
        private static Thread _thr;
        private static bool _excep;
        private static bool _newDay;

        #endregion

        public static void Main()
        {
            try
            {
                TwitterJob.ScheduleTwitterJob();
            }
            catch(Exception e)
            {
               _logs.WriteErrorLog(e);
            }
        }

       

        private static void MainAfterExeption()
        {
            _logs.WriteLog("Exception");
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
                        _newDay = false;
                    }

                    while (!_newDay)
                    {
                        try
                        {
                            if (_updateTimes.Count != 0)
                            {
//                                SleepAndUpdate();
                            }
                            else
                            {
//                                SleepUntilTomorrow();
                                ClearingFollowers();

                                if (_newDay)
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
            _logs.WriteErrorLog(e);
           _excep = true;
            MainAfterExeption();
        }

        /// <summary>
        /// каждую неделю чистим фолловеров
        /// </summary>
        private static void ClearingFollowers()
        {
            if (DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
            {
                _logs.WriteLog("Clearing followings:");
                _api.ClearFollowings();
            }
        }

//      /// <summary>
//        /// попробовали заснуть на время до следующего апдейта
//        /// </summary>
//        private static void SleepAndUpdate()
//        {
//            _logs.WriteLog("Sleeping until " + _updateTimes[0] + "\n");
//            Thread.Sleep(_updateTimes[0] - DateTime.Now);
//            Updates();
//            _updateTimes.RemoveAt(0);
//            _excep = false;
//        }

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
                _logs.WriteErrorLog(e);
            }

            //фолловим по 5 человек три раза в день
            try
            {
                _logs.WriteLog("Follow suggested users:");
                _api.Follow(_api.UsersToFollow());
            }
            catch (Exception e)
            {
                _logs.WriteErrorLog(e);
            }

            //фолловим всех кто отреплаил
            try
            {
                _logs.WriteLog("Follow who replied me:");
                _api.Follow(_api.MentionsToMe());
            }
            catch (Exception e)
            {
                _logs.WriteErrorLog(e);
            }

            //фолловим всех кто отретвитил
            try
            {
                _logs.WriteLog("Follow who retweeted me:");
                _api.Follow(_api.RetweetsOfMe());
            }
            catch (Exception e)
            {
                _logs.WriteErrorLog(e);
            }

            //фолловим всех кто зафоловил
            try
            {
                _logs.WriteLog("Follow who followed me:");
                _api.Follow(_api.WhoFollowedMe());
            }
            catch (Exception e)
            {
                _logs.WriteErrorLog(e);
            }
        }
    }
}
