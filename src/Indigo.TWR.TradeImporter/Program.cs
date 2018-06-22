using Indigo.TWR.TradeImporter.Library;
using System;

namespace Indigo.TWR.TradeImporter
{
    class Program
    {
        private const int ERROR_RESULT = 255;
        private const int SUCCESS_RESULT = 0;

        private static AzureImporter importer;

        /// <summary>
        /// Main entry point for the application. Registers dependencies, and runs the processor.
        /// </summary>
        static int Main(string[] args)
        {
            importer = new AzureImporter();
#if DEBUG
            importer.Log.Warning("This is a DEBUG build.");
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
#else
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
#endif
            importer.Run();

            return SUCCESS_RESULT;
        }

        /// <summary>
        /// Unhandled Exception Handler
        /// </summary>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            importer.Log.Fatal((Exception)e.ExceptionObject, "Unhandled Exception --");
            Environment.Exit(ERROR_RESULT);
        }

        /// <summary>
        /// First Chance Exception Handler - Debug Mode Only
        /// </summary>
        private static void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            importer.Log.Error(e.Exception, "First Chance Exception Handler -- ");
        }
    }
}
