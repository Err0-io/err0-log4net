using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Threading;
using log4net.Appender;
using log4net.Core;
using Newtonsoft.Json.Linq;

namespace err0.log4net
{
    public class Err0Appender : AppenderSkeleton
    {
        public Err0Appender()
        {
            token = ConfigurationManager.AppSettings["err0.token"];
            url = ConfigurationManager.AppSettings["err0.url"];
            bulkLogApi = new Uri(url + "~/api/bulk-log");

            thread = new Thread(new Err0Thread(this).Loop);
            thread.IsBackground = true;
            thread.Start();
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
        }

        void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            stopped = true;
            while (pollQueue())
            {
            }
        }

        private readonly String token;
        private readonly String url;
        private readonly Uri bulkLogApi;
        private static readonly Regex pattern = new Regex("\\[([A-Z][A-Z0-9]*-[0-9]+)\\]", RegexOptions.Compiled);

        private bool stopped = false;
        private readonly Thread thread;

        public sealed class Err0Thread
        {
            public Err0Thread(Err0Appender appender)
            {
                this.appender = appender;
            }

            private readonly Err0Appender appender;
            
            public void Loop()
            {
                for (; !appender.stopped;)
                {
                    if (!Err0Http.canCall())
                    {
                        Thread.Yield();
                    }
                    else
                    {
                        bool wasEmpty = appender.pollQueue();
                        if (wasEmpty)
                        {
                            Thread.Yield();
                        }
                    }
                }
            }
        }
        
        public sealed class Err0Log {
            internal Err0Log(string error_code, long ts, string message, JObject metadata) {
                this.error_code = error_code;
                this.ts = ts;
                this.message = message;
                this.metadata = metadata;
            }
            public readonly string error_code;
            public readonly long ts;
            public readonly string message;
            public readonly JObject metadata;
        }

        private readonly ConcurrentQueue<Err0Log> queue = new ConcurrentQueue<Err0Log>();

        private bool pollQueue()
        {
            List<Err0Log> list = new List<Err0Log>();
            Err0Log logEvent = null;
            for(;;)
            {
                if (queue.TryDequeue(out logEvent))
                {
                    list.Add(logEvent);
                }
                else
                {
                    break;
                }
            }

            if (list.Count > 0)
            {
                JObject bulkLog = new JObject();
                JArray logs = new JArray();
                foreach (Err0Log log in list)
                {
                    JObject o = new JObject();
                    o.Add("error_code", log.error_code);
                    o.Add("ts", "" + log.ts);
                    o.Add("msg", log.message);
                    o.Add("metadata", log.metadata);
                    logs.Add(o);
                }
                bulkLog.Add("logs", logs);
                Err0Http.call(bulkLogApi, token, bulkLog);
            }
            else
            {
                return false;
            }

            return true;
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            string formattedMessage = loggingEvent.RenderedMessage;
            Match match = pattern.Match(formattedMessage);
            if (match.Success)
            {
                //Console.Out.WriteLine("ERR0\t" + formattedMessage);
                string error_code = match.Groups[1].Value;
                DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                long ts = (loggingEvent.TimeStampUtc - unixStart).Ticks / TimeSpan.TicksPerMillisecond;
                JObject metadata = new JObject();
                JObject log4netMetadata = new JObject();
                log4netMetadata.Add("level", loggingEvent.Level.Name);
                log4netMetadata.Add("source_class", loggingEvent.LocationInformation.ClassName);
                log4netMetadata.Add("source_file", loggingEvent.LocationInformation.FileName);
                log4netMetadata.Add("source_line", loggingEvent.LocationInformation.LineNumber);
                log4netMetadata.Add("source_method", loggingEvent.LocationInformation.MethodName);
                metadata.Add("log4net", log4netMetadata);
                queue.Enqueue(new Err0Log(error_code, ts, formattedMessage, metadata));
            }
        }
    }
}