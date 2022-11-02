using System;
using System.Threading;
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
            Logger.Error("[EG-4] Error [EG-5]");
            Thread.Sleep(1000);
        }
    }
}