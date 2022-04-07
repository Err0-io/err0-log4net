using System;
using log4net.Appender;
using log4net.Core;
using Newtonsoft.Json.Linq;

namespace err0.log4net
{
    public class Err0Appender : AppenderSkeleton
    {
        public class Err0Log {
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
        
        protected override void Append(LoggingEvent loggingEvent)
        {
            Console.Out.WriteLine("ERR0\t" + loggingEvent.RenderedMessage);
        }
    }
}