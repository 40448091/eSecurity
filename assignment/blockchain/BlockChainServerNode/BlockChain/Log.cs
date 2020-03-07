using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BlockChainDemo
{
    public static class Logger
    {
        public static bool echo { get; set; }
        static string logDir = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "logs");
        static string logFilename =Path.Combine(logDir, System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".log");
        
        static Logger()
        {
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
