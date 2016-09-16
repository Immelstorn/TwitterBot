using System;

namespace TwitterBot.DB.Entities
{
    public class Log
    {
        public Log()
        {
            Timestamp = DateTime.UtcNow;
        }

        public int ID { get; set; }
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
    }
}