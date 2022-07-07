using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace err0.log4net
{
    public class Err0Http
    {
        private static readonly HttpClient client = new HttpClient();
        private static int inFlight = 0;
        private static long errorUntil = 0;

        public static bool canCall()
        {
            return inFlight < 4;
        }

        public static async void call(Uri uri, string token, JObject payload)
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

            HttpRequestMessage request = new HttpRequestMessage()
            {
                RequestUri = uri,
                Method = HttpMethod.Post
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
            var result = await client.SendAsync(request);
            //Console.Out.WriteLine(result.StatusCode);
            Interlocked.Decrement(ref inFlight);
            if (result.IsSuccessStatusCode)
            {
                Interlocked.Exchange(ref errorUntil, 0);
            }
            else
            {
                Interlocked.Exchange(ref errorUntil, DateTime.UtcNow.Ticks + 30 * 60 * TimeSpan.TicksPerSecond);
            }
        }
    }
}