using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading;

namespace TimeSyncService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Check if running in console mode (for testing/debugging)
            if (Environment.UserInteractive || args.Contains("--console"))
            {
                Console.WriteLine("=== Time Sync Service - Console Mode ===");
                Console.WriteLine("Running in console mode for testing...");
                Console.WriteLine();
                
                var service = new TimeSyncService();
                
                try
                {
                    Console.WriteLine("Starting service execution...");
                    
                    // Call OnStart method directly
                    var onStartMethod = service.GetType()
                        .GetMethod("OnStart", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    
                    if (onStartMethod != null)
                    {
                        onStartMethod.Invoke(service, new object[] { args });
                        Console.WriteLine("Service execution completed successfully.");
                    }
                    else
                    {
                        Console.WriteLine("ERROR: Could not find OnStart method.");
                    }
                    
                    Console.WriteLine();
                    Console.WriteLine("Check the TLogs directory for detailed log files.");
                    Console.WriteLine();
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine($"ERROR: {ex.Message}");
                    
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                        Console.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                    }
                    else
                    {
                        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    }
                    
                    Console.WriteLine();
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                }
            }
            else
            {
                // Run as Windows service
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new TimeSyncService()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
