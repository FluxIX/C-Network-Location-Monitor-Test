using System;
using System.Net;
using System.Timers;

namespace NetworkMonitoring
{
   public class NetworkMonitor : IDisposable
   {
      #region Events
      #region Delegates
      public delegate void MonitoringStartedDelegate( NetworkMonitor sender, EventArgs args );
      public delegate void UpdatedDelegate( NetworkMonitor sender, NetworkMonitorEventArgs args );
      public delegate void MonitoringStoppedDelegate( NetworkMonitor sender, EventArgs args );
      #endregion

      public event MonitoringStartedDelegate MonitoringStarted;
      public event UpdatedDelegate Updated;
      public event MonitoringStoppedDelegate MonitoringStopped;
      #endregion

      public NetworkMonitor( String address )
         : this( new Uri( address ) )
      {
      }

      public NetworkMonitor( Uri address )
      {
         MonitoredUri = address;
      }

      protected Uri monitoredUri;

      public Uri MonitoredUri
      {
         get
         {
            return monitoredUri;
         }

         protected set
         {
            if( value != null )
               monitoredUri = value;
            else
               throw new ArgumentNullException( "value", "Monitored URI cannot be null." );
         }
      }

      protected Object MonitoringLock = new Object();

      protected Boolean isMonitoring;

      public Boolean IsMonitoring
      {
         get
         {
            lock( MonitoringLock )
            {
               return isMonitoring;
            }
         }

         protected set
         {
            lock( MonitoringLock )
            {
               isMonitoring = value;
            }
         }
      }

      protected HttpWebRequest Request
      {
         get;
         set;
      }

      /// <summary>
      /// Begins monitoring with an update period of 5 seconds.
      /// </summary>
      /// <returns><c>true</c> if monitoring is started, <c>false</c> otherwise.</returns>
      public virtual Boolean BeginMonitoring()
      {
         return BeginMonitoring( 5000 );
      }

      public virtual Boolean BeginMonitoring( Double monitorUpdateFrequency_ms )
      {
         Boolean result = false;

         lock( MonitoringLock )
         {
            if( !IsMonitoring )
            {
               Request = WebRequest.Create( MonitoredUri ) as HttpWebRequest;
               if( Request != null )
               {
                  HttpWebResponse response = Request.GetResponse() as HttpWebResponse;

                  result = IsMonitoring = response != null;
               }
            }
         }

         MonitorTimer = MakeTimer( monitorUpdateFrequency_ms );
         MonitorTimer.Start();

         if( MonitoringStarted != null )
            MonitoringStarted.Invoke( this, new EventArgs() );

         return result;
      }

      public virtual Boolean StopMonitoring()
      {
         Boolean result = false;

         lock( MonitoringLock )
         {
            if( IsMonitoring )
            {
               MonitorTimer.Elapsed -= MonitorTimer_Elapsed;
               MonitorTimer.Stop();

               result = true;
            }
         }

         if( result && MonitoringStopped != null )
            MonitoringStopped.Invoke( this, new EventArgs() );

         return result;
      }

      protected Timer MonitorTimer
      {
         get;
         set;
      }

      protected Timer MakeTimer( Double monitorFrequency_ms )
      {
         Timer result = new Timer( monitorFrequency_ms );

         result.Enabled = true;
         result.Elapsed += MonitorTimer_Elapsed;

         return result;
      }

      protected virtual void MonitorTimer_Elapsed( Object sender, ElapsedEventArgs e )
      {
         HttpWebResponse response = Request.GetResponse() as HttpWebResponse;

         if( response != null && Updated != null )
            Updated.Invoke( this, new NetworkMonitorEventArgs( response ) );
      }

      #region IDisposable Members
      public void Dispose()
      {
         lock( MonitoringLock )
         {
            if( IsMonitoring && StopMonitoring() )
               MonitorTimer.Dispose();
         }
      }
      #endregion
   }

   public class NetworkMonitorEventArgs : EventArgs
   {
      public NetworkMonitorEventArgs( HttpWebResponse response )
      {
         Response = response;
      }

      public HttpWebResponse Response
      {
         get;
         protected set;
      }
   }
}
