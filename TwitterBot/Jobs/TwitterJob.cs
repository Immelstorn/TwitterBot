using System;
using System.Collections.Generic;
using System.Linq;

using Quartz;
using Quartz.Impl;

using TwitterBot.DB;

namespace TwitterBot.Jobs
{
    internal class TwitterJob: IJob
    {

      private static readonly Logs Logs = new Logs();


        public void Execute(IJobExecutionContext context)
        {
            try
            {
                Logs.WriteLog("Starting to execute twitter job");
                RunTask();
                ScheduleTwitterJob();
            }
            catch(Exception e)
            {
                Logs.WriteErrorLog(e);
            }
        }

        public static void ScheduleTwitterJob()
        {
            Logs.WriteLog("Starting to schedule twitter job");

            var updateTimes = GetUpdateTimes();

            var schedulerFactory = new StdSchedulerFactory();
            var scheduler = schedulerFactory.GetScheduler();
            scheduler.Start();

            if(updateTimes.Any())
            {
                Logs.WriteLog("Creating new twitter job");
                var job = JobBuilder.Create<TwitterJob>().Build();
                var utcTime = new DateTime(updateTimes.First().Ticks, DateTimeKind.Utc);

                var trigger = TriggerBuilder.Create().StartAt(utcTime.ToLocalTime()).Build();
                Logs.WriteLog($"Scheduling to: {utcTime}");

                scheduler.ScheduleJob(job, trigger);
            }
            else
            {
                Logs.WriteLog("Creating new generate times job");
                var job = JobBuilder.Create<GenerateUpdateTimesJob>().Build();

                //                var startTime = new DateTime(DateTime.Today.AddHours(13).AddMinutes(47).Ticks, DateTimeKind.Utc);
                //                var trigger = TriggerBuilder.Create().StartAt(startTime.ToLocalTime()).Build();

                var startTime = new DateTime(DateTime.Today.AddHours(6).AddDays(1).Ticks, DateTimeKind.Utc);
                var trigger = TriggerBuilder.Create().StartAt(startTime.ToLocalTime()).Build();
                Logs.WriteLog($"Scheduling to: {startTime}");
                scheduler.ScheduleJob(job, trigger);
            }
            Logs.WriteLog("Done");
        }

        public static List<DateTime> GetUpdateTimes()
        {
            Logs.WriteLog("Get update times");
            using(var db = new TwitterBotContext())
            {
                var times = db.UpdateTimes.Where(x => x.Time > DateTime.UtcNow).OrderBy(x => x.Time).ToList();
                Logs.WriteLog($"Found {times.Count} times");
                return times.Select(x => x.Time).ToList();
            }
        }


        public static void RunTask()
        {
            try
            {
                //once per week cleaning followers
                var api = new API();
                api.ClearFollowings();

                //update status
                //follow 5 users 
                //follow replied
                //follow who followed

                //                var twitterCtx = new TwitterContext(Auth);
                //              
                //                var tweet = twitterCtx.TweetAsync(DateTime.Now.ToString()).Result;
                //                var tweet = twitterCtx.NewDirectMessageAsync("Immelstorn", DateTime.UtcNow.ToString()).Result;
                //                Console.WriteLine(tweet == null ? "an error occured, tweet is null" : string.Format("tweet.StatusID: {0}", tweet.ID));
                //                Logs.WriteLog("Tweet sent");
            }
            catch(Exception e)
            {
                Console.WriteLine("an error occured: {0}", e.InnerException?.Message ?? e.Message);
            }
        }
    }
}