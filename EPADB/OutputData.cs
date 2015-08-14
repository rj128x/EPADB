using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPADB {
    public class OutputData {
        protected static Dictionary<string, log4net.ILog> data;

        public static void InitOutput(string name) {
            if (data == null) {
                data = new Dictionary<string, ILog>();
            }
            string fileName = String.Format("{0}", name);
            try {
                if (!data.ContainsKey(name)) {
                    LogManager.CreateRepository(name);
                    ILog logger = LogManager.GetLogger(name, name);
                    data.Add(name, logger);
                }

                FileAppender appender = new FileAppender();
                PatternLayout layout = new PatternLayout(@"%m%n");
                appender.Layout = layout;
                appender.File = fileName;
                appender.AppendToFile = true;
                BasicConfigurator.Configure(LogManager.GetRepository(name), appender);
                appender.ActivateOptions();

            }
            catch (Exception e) {
                Console.WriteLine("Ошибка при создании log-файла " + fileName);
                Console.WriteLine(e.Message);
            }
        }

        public static void writeToOutput(string name, string dataStr) {
            if (data == null || !data.ContainsKey(name)) {
                InitOutput(name);
            }
            data[name].Info(dataStr);
        }
    }
}
