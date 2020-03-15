using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BlockChain
{
    public static class Logger
    {
        public static bool echo { get; set; }
        static string logDir = "";
        static string logFilename = "";
        
        static Logger()
        {
            string cryptoProvider = System.Configuration.ConfigurationManager.AppSettings["cryptoProvider"];
            logDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            logDir = Path.Combine(logDir,cryptoProvider,"logs");

            logFilename = Path.Combine(logDir, System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".log");

            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
        }

        public static void Log(string message)
        {
            System.DateTime timestamp = System.DateTime.Now;
            using (StreamWriter writer = new StreamWriter(logFilename, true))
            {
                writer.WriteLine($"{timestamp} : {message}");
            }

            if (echo)
                System.Console.WriteLine($"{timestamp} : {message}");
        }
    }
}
