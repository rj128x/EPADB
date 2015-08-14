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
    class Logger {
        public enum LoggerSource { server, client, service, none }
        public log4net.ILog logger;

        protected static Logger context;

        public Logger() {

        }

        public bool IsFileLogger { get; protected set; }
        public FileAppender appender { get; protected set; }
        public string Path { get; protected set; }
        public string Name { get; protected set; }
        public DateTime Date { get; protected set; }

        public static void InitFileLogger(string path, string name, Logger newLogger = null) {
            try {
                if (context != null && context.IsFileLogger && context.appender != null) {
                    context.appender.Close();
                }
            }
            catch { }

            if (newLogger == null) {
                newLogger = new Logger();
            }

            string fileName = String.Format("{0}/{1}_{2}.txt", path, name, DateTime.Now.ToString("dd_MM_yyyy"));
            try {
                PatternLayout layout = new PatternLayout(@"[%d] %-10p %m%n");
                newLogger.appender = new FileAppender();
                newLogger.appender.Layout = layout;
                newLogger.appender.File = fileName;
                newLogger.appender.AppendToFile = true;
                BasicConfigurator.Configure(newLogger.appender);
                newLogger.appender.ActivateOptions();

                newLogger.logger = LogManager.GetLogger(name);
                newLogger.Path = path;
                newLogger.Name = name;
                newLogger.IsFileLogger = true;
                newLogger.Date = DateTime.Now.Date;
                Logger.context = newLogger;
            }
            catch (Exception e) {
                Console.WriteLine("Ошибка при создании log-файла " + fileName);
                Console.WriteLine(e.Message);
            }
        }

        public static void init(Logger context) {
            Logger.context = context;
        }

        public static void checkFileLogger() {
            if (context.IsFileLogger && (DateTime.Now.Date > context.Date)) {
                InitFileLogger(context.Path, context.Name);
            }
        }

        protected virtual string createMessage(string message, LoggerSource source = LoggerSource.none) {
            if (source != LoggerSource.none) {
                return String.Format("{0,-10} {1}", source.ToString(), message);
            }
            else {
                return String.Format("{0}", message);
            }
        }

        protected virtual void info(string str, LoggerSource source = LoggerSource.none) {
            try { logger.Info(createMessage(str, source)); }
            catch { }
            Console.WriteLine(createMessage(str, source));
        }

        protected virtual void error(string str, LoggerSource source = LoggerSource.none) {
            try { logger.Error(createMessage(str, source)); }
            catch { }
            Console.WriteLine(createMessage(str, source));
        }

        protected virtual void debug(string str, LoggerSource source = LoggerSource.none) {
            try { logger.Debug(createMessage(str, source)); }
            catch { }
            Console.WriteLine(createMessage(str, source));
        }


        public static void Info(string str, LoggerSource source = LoggerSource.none) {
            Logger.checkFileLogger();
            context.info(str, source);
        }

        public static void Error(string str, LoggerSource source = LoggerSource.none) {
            Logger.checkFileLogger();
            context.info(str, source);
        }

        public static void Debug(string str, LoggerSource source = LoggerSource.none) {
            Logger.checkFileLogger();
            context.info(str, source);
        }
    }
}
