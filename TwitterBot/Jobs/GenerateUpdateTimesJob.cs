using System;
using System.Collections.Generic;
using System.Linq;

using Quartz;

using TwitterBot.DB;
using TwitterBot.DB.Entities;

namespace TwitterBot.Jobs
{
    internal class GenerateUpdateTimesJob: IJob
    {
        private readonly Random _random = new Random();
        private readonly Logs _logs = new Logs();


        public void Execute(IJobExecutionContext context)
        {
            try
            {
                _logs.WriteLog("Generating update times");
                using(var db = new TwitterBotContext())
                {
                    var day = TimeSpan.FromHours(13);
                    var updateTimes = new List<DateTime>();
                    var done = true;
                    do
                    {
                        for (var i = 0; i < 5; i++)
                        {
                            var rnd = _random.Next(Convert.ToInt32(day.TotalSeconds));
                            updateTimes.Add(DateTime.Parse("07:00") + TimeSpan.FromSeconds(rnd)); //I am using UTC time, so 07:00 is 10:00 for UA time
                        }
                        updateTimes.Sort();
                        for(var i = 1; i < updateTimes.Count; i++)
                        {
                            if(updateTimes[i] - updateTimes[i - 1] < new TimeSpan(0, 0, 20, 0))
                            {
                                done = false;
                                updateTimes = new List<DateTime>();
                                break;
                            }
                        }
                    } while (!done);
                   

                    //remove if any
                    db.UpdateTimes.RemoveRange(db.UpdateTimes.ToList());
                    db.SaveChanges();

                    foreach(var time in updateTimes)
                    {
                        db.UpdateTimes.Add(new UpdateTime {
                                               Time = time
                                           });
                    }
                    _logs.WriteLog("Generated");
                    db.SaveChanges();
                    TwitterJob.ScheduleTwitterJob();
                }
            }
            catch (Exception e)
            {
                _logs.WriteErrorLog(e);
            }
        }
    }
}