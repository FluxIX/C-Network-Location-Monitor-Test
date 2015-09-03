using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetworkMonitoring;

namespace NetworkMonitorTest
{
   class Program
   {
      private static NetworkMonitor Monitor;

      static void Main( String[] args )
      {
         const String address = "http://www.google.com";

         {
            NetworkMonitor monitor = new NetworkMonitor( address );

            monitor.MonitoringStarted += monitor_MonitoringStarted;
            monitor.MonitoringStopped += monitor_MonitoringStopped;
            monitor.Updated += monitor_Updated;

            Monitor = monitor;
         }

         Monitor.BeginMonitoring( 1500 );

         Console.Out.WriteLine( "Press any key to stop monitoring..." );
         Console.In.Read();

         Monitor.StopMonitoring();
      }

      static void monitor_MonitoringStarted( NetworkMonitor sender, EventArgs args )
      {
         DateTime now = DateTime.Now;

         Console.Out.WriteLine( String.Format( "{0}: Monitoring {1} started.", now.ToString(), sender.MonitoredUri.AbsoluteUri ) );
      }

      static void monitor_MonitoringStopped( NetworkMonitor sender, EventArgs args )
      {
         DateTime now = DateTime.Now;

         Console.Out.WriteLine( String.Format( "{0}: Monitoring {1} stopped.", now.ToString(), sender.MonitoredUri.AbsoluteUri ) );
      }

      static void monitor_Updated( NetworkMonitor sender, NetworkMonitorEventArgs args )
      {
         DateTime now = DateTime.Now;

         String message;
         if( args.ReceivedResponse )
            message = String.Format( "{0}: Monitoring {1} updated. Response status: {2} ({3})", now.ToString(), sender.MonitoredUri.AbsoluteUri, args.Response.StatusCode, Convert.ToUInt16( args.Response.StatusCode ) );
         else
            message = String.Format( "{0}: Monitoring {1} updated. Response not received.", now.ToString(), sender.MonitoredUri.AbsoluteUri );

         Console.Out.WriteLine( message );
      }
   }
}
