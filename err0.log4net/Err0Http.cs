using System;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace err0.log4net
{
    public class Err0Http
    {
        private static int inFlight = 0;
        private static long errorUntil = 0;

        public static bool canCall()
        {
            return inFlight < 4;
        }

        public static void call(Uri uri, string token, JObject payload)
        {
            if (errorUntil != 0)
            {
                long now = DateTime.UtcNow.Ticks;
                if (now < errorUntil)
                {
                    return; // silently drops logs, there is an error on http.
                }
            }

            Interlocked.Increment(ref inFlight);

            using (var client = new WebClient())
            {
                try
                {
                    client.Headers.Add("Authorization", "Bearer " + token);
                    client.Headers.Add("Content-Type", "application/json");
                    client.UploadString(uri, payload.ToString());

                    Interlocked.Decrement(ref inFlight);
                    Interlocked.Exchange(ref errorUntil, 0);
                }
                catch (Exception e)
                {
                    Interlocked.Exchange(ref errorUntil, DateTime.UtcNow.Ticks + 30 * 60 * TimeSpan.TicksPerSecond);
                }
            }
        }
    }
}