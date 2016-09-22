using System;

using TwitterBot.Jobs;

namespace TwitterBot
{
    class Program
    {
        private static readonly Logs Logs = new Logs();

        public static void Main()
        {
            try
            {
//                TwitterJob.ScheduleTwitterJob();
                TwitterJob.RunTask();
            }
            catch(Exception e)
            {
               Logs.WriteErrorLog(e);
            }
        }
    }
}
