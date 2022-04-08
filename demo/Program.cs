using System;
using log4net;

namespace demo
{
    internal class Program
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void Main(string[] args)
        {
            Logger.Info("[EG-2] Info");
            Logger.Warn("[EG-3] Warn");
            Logger.Error("[EG-4] Error");
            Logger.Fatal("[EG-5] Fatal");
        }
    }
}