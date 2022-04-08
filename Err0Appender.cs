using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Text.RegularExpressions;
using log4net.Appender;
using log4net.Core;
using Newtonsoft.Json.Linq;

namespace err0.log4net
{
    public class Err0Appender : AppenderSkeleton
    {
        public Err0Appender() : base()
        {
            token = ConfigurationManager.AppSettings["err0.token"];
            url = ConfigurationManager.AppSettings["err0.url"];
            realm_uuid = ConfigurationManager.AppSettings["err0.realm_uuid"];
            prj_uuid = ConfigurationManager.AppSettings["err0.prj_uuid"];
            pattern = new Regex(ConfigurationManager.AppSettings["err0.pattern"],
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        private readonly String token;
        private readonly String url;
        private readonly String realm_uuid;
        private readonly String prj_uuid;
        private readonly Regex pattern;
        
        public sealed class Err0Log {
            public Err0Log(string error_code, long ts, string message, JObject metadata) {
                this.error_code = error_code;
                this.ts = ts;
                this.message = message;
                this.metadata = metadata;
            }
            public string error_code;
            public long ts;
            public string message;
            public JObject metadata;
        }

        private readonly ConcurrentQueue<Err0Log> queue = new ConcurrentQueue<Err0Log>();

        protected override void Append(LoggingEvent loggingEvent)
        {
            string formattedMessage = loggingEvent.RenderedMessage;
            Match match = pattern.Match(formattedMessage);
            if (match.Success)
            {
                Console.Out.WriteLine("ERR0\t" + formattedMessage);
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