using System;

namespace TwitterBot.DB.Entities
{
    public class StatusLog
    {
        public StatusLog()
        {
            Timestamp = DateTime.UtcNow;
        }

        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
    }
}