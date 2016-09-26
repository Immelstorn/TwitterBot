using System;

namespace TwitterBot.DB.Entities
{
    public class ErrorLog
    {
        public ErrorLog()
        {
            Timestamp = DateTime.UtcNow;
        }

        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }
}