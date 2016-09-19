using System;

using TwitterBot.DB;
using TwitterBot.DB.Entities;

namespace TwitterBot
{
    internal class Logs
    {
        internal void WriteStatusLog(string status)
        {
            using (var db = new TwitterBotContext())
            {
                var log = new StatusLog();
                log.Message = status;
                db.StatusLogs.Add(log);
                db.SaveChanges();
            }
        }

        internal void WriteLog(string message)
        {
            using (var db = new TwitterBotContext())
            {
                var log = new Log();
                log.Message = message;
                db.Logs.Add(log);
                db.SaveChanges();
            }
        }


        internal void WriteErrorLog(Exception e)
        {
            var ex = e;
            do
            {
                WriteErrorLog(ex.Message);
                ex = e.InnerException;
            }
            while (ex != null);
        }

        private void WriteErrorLog(string message)
        {
            using (var db = new TwitterBotContext())
            {
                var log = new ErrorLog();
                log.Message = message;
                db.Errors.Add(log);
                db.SaveChanges();
            }
        }


        internal void WriteBlackList(decimal user)
		{
            using (var db = new TwitterBotContext())
            {
                var blackList = new BlackList();
                blackList.UserId = user;
                db.BlackLists.Add(blackList);
                db.SaveChanges();
            }
        }
    }
}
