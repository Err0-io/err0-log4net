using System;
using log4net;

namespace demo
{
    internal class Program
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void Main(string[] args)
        {
            Logger.Info("Info");
            Logger.Warn("Warn");
            Logger.Error("Error");
            Logger.Fatal("Fatal");
        }
    }
}