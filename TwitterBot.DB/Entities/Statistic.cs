using System;

namespace TwitterBot.DB.Entities
{
    public class Statistic
    {
        public Statistic()
        {
            Timestamp = DateTime.UtcNow;
        }

        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public int Followers { get; set; }
        public int Followings { get; set; }
    }
}