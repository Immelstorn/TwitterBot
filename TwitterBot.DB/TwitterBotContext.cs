using System.Data.Entity;

using TwitterBot.DB.Entities;

namespace TwitterBot.DB
{
    public class TwitterBotContext: DbContext
    {
        public TwitterBotContext()
            : base("name=TwitterBotDb") { }

        public DbSet<Log> Logs { get; set; }
        public DbSet<StatusLog> StatusLogs { get; set; }
        public DbSet<BlackList> BlackLists { get; set; }
        public DbSet<ErrorLog> Errors { get; set; }
        public DbSet<UpdateTime> UpdateTimes{ get; set; }

    }
}
