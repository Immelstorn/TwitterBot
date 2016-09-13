using System;
using System.IO;


namespace TwitterBot
{
    class Logs
    {
        private static Logs _instance;
        private static object _syncLock = new object();

        private Logs()
        {
            if (!(File.Exists("log.txt")))
            {
                File.Create("log.txt");
            }
            if (!(File.Exists("StatusLog.txt")))
            {
                File.Create("StatusLog.txt");
            }
            if (!(File.Exists("errorlog.txt")))
            {
                File.Create("errorlog.txt");
            }
			if (!(File.Exists("blacklist.txt")))
			{
				File.Create("blacklist.txt");
			}
        }

        //синглтон
        public static Logs GetLogsClass()
        {
            if (_instance == null)
            {
                lock (_syncLock)
                {
                    if (_instance == null)
                    {
                        _instance = new Logs();
                    }
                }
            }
            return _instance;
        }

        /// <summary>
        /// Writes the status log.
        /// </summary>
        /// <param name="logstring">The logstring.</param>
        public void WriteStatusLog(string logstring)
        {
            var fi = new FileInfo("StatusLog.txt");
            if ((DateTime.Now - fi.CreationTime).Days >= 365)
            {
                File.Delete("StatusLog.txt");
                File.Create("StatusLog.txt");
            }
            File.AppendAllText("StatusLog.txt", logstring);
        }

        /// <summary>
        /// Writes log.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="logstring">The logstring.</param>
        public void WriteLog(string file, string logstring)
        {
            var fi = new FileInfo(file);
            if (fi.Length > 10485760)
            {
                //если больше 10 мб то бекапим лог и создаем новый
                File.Replace(file, file + DateTime.Now, file + "old");
                File.AppendAllText(file, "Old log was renamed to " + file + DateTime.Now);
            }

            File.AppendAllText(file, $"{DateTime.Now} => {logstring}\n");
        }

		/// <summary>
		/// Writes the black list.
		/// </summary>
		/// <param name="user">The user.</param>
		public void WriteBlackList(decimal user)
		{
		    File.AppendAllText("blacklist.txt", user + "\n");
		}
    }
}
