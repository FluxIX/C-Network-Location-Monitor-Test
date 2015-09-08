using System;
using System.Net;
using System.Net.Cache;
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
            if( !IsMonitoring )
            {
               if( value != null )
                  monitoredUri = value;
               else
                  throw new ArgumentNullException( "value", "Monitored URI cannot be null." );
            }
            else
               throw new InvalidOperationException( "Cannot change the monitored URI when monitoring." );
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
               MonitorTimer = MakeTimer( monitorUpdateFrequency_ms );
               MonitorTimer.Start();

               Request = CreateWebRequest( MonitoredUri );
               if( Request != null )
               {
                  if( MonitoringStarted != null )
                     MonitoringStarted.Invoke( this, new EventArgs() );

                  HttpWebResponse response;
                  result = IsMonitoring = TryGetResponse( Request, out response );
               }
            }
         }

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

      public const HttpRequestCacheLevel DefaultRequestCacheLevel = HttpRequestCacheLevel.NoCacheNoStore;

      protected HttpWebRequest CreateWebRequest( Uri uri, HttpRequestCacheLevel cacheLevel = DefaultRequestCacheLevel )
      {
         HttpWebRequest result = WebRequest.Create( uri ) as HttpWebRequest;

         if( result != null )
         {
            HttpRequestCachePolicy cachePolicy = new HttpRequestCachePolicy( cacheLevel );
            result.CachePolicy = cachePolicy;
         }

         return result;
      }

      protected Boolean TryGetResponse( HttpWebRequest request, out HttpWebResponse response )
      {
         DateTime requestTimestamp;
         DateTime? responseTimestamp;

         return TryGetResponse( request, out response, out requestTimestamp, out responseTimestamp );
      }

      protected Boolean TryGetResponse( HttpWebRequest request, out HttpWebResponse response, out DateTime requestTimestamp, out DateTime? responseTimestamp )
      {
         Boolean result;

         Boolean responseRecieved = false;
         HttpWebResponse receivedResponse = null;
         DateTime? responseTime = null;

         requestTimestamp = DateTime.Now;

         try
         {
            receivedResponse = request.GetResponse() as HttpWebResponse;
            responseTime = DateTime.Now;
            responseRecieved = true;
         }
         catch( ProtocolViolationException )
         {
         }
         catch( WebException )
         {
         }
         catch( InvalidOperationException )
         {
         }
         catch( NotSupportedException )
         {
         }

         if( result = responseRecieved && receivedResponse != null )
            receivedResponse.Close();
         response = receivedResponse;
         responseTimestamp = responseTime;

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

      protected Object requestLock = new Object();

      public Boolean IsRequesting
      {
         get;
         protected set;
      }

      protected virtual void MonitorTimer_Elapsed( Object sender, ElapsedEventArgs e )
      {
         if( !IsRequesting )
         {
            HttpWebResponse response;
            DateTime requestTimestamp;
            DateTime? responseTimestamp;

            lock( requestLock )
            {
               IsRequesting = true;

               TryGetResponse( ( Request = CreateWebRequest( MonitoredUri ) ), out response, out requestTimestamp, out responseTimestamp );

               IsRequesting = false;
            }

            if( Updated != null )
               Updated.Invoke( this, new NetworkMonitorEventArgs( requestTimestamp, responseTimestamp, MonitoredUri, response ) );
         }
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
      public NetworkMonitorEventArgs( DateTime requestTimestamp, DateTime? responseTimestamp, Uri identifier, HttpWebResponse response )
      {
         RequestTimestamp = requestTimestamp;
         ResponseTimestamp = responseTimestamp;
         Identifier = identifier;
         Response = response;
      }

      public DateTime RequestTimestamp
      {
         get;
         protected set;
      }

      public DateTime? ResponseTimestamp
      {
         get;
         protected set;
      }

      public Boolean HasResponseTimestamp
      {
         get
         {
            return ResponseTimestamp != null;
         }
      }

      public TimeSpan RequestDuration
      {
         get
         {
            if( HasResponseTimestamp )
            {
               TimeSpan result = ( ( DateTime ) ResponseTimestamp ) - RequestTimestamp;
               return result;
            }
            else
               throw new InvalidOperationException( "No response timestamp present." );
         }
      }

      public Uri Identifier
      {
         get;
         protected set;
      }

      public Boolean ReceivedResponse
      {
         get
         {
            return Response != null;
         }
      }

      public HttpWebResponse Response
      {
         get;
         protected set;
      }
   }
}
