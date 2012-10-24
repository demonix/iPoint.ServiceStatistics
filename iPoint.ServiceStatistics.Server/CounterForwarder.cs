using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading;
using EventEvaluationLib;
using MyLib.ClientSide.Networking;
using NLog;

namespace iPoint.ServiceStatistics.Server
{
    internal class CounterForwarder
    {

        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private static TcpClient _tcpClient = null;
        private static bool _debug;
        static IPAddress _currentServerIpAddress = null;
        static int _currentServerPort = -1;
        private static Timer _reconnectTimer;

        static ReaderWriterLockSlim _tcpClientLocker = new ReaderWriterLockSlim();

        public CounterForwarder(IObservable<LogEventArgs> observableEvents)
        {
            _reconnectTimer = new Timer(CreateTcpClientTimerWrapper);
            _observableEvents = observableEvents;
        }

        private static void CreateTcpClientTimerWrapper(object obj)
        {
            CreateTcpClient("settings\\ServerAddress");
            _logger.Info("tcpClient reconnected");
            _isReconnectSheduled = ReconnectNotScheduled;

        }

        private readonly IObservable<LogEventArgs> _observableEvents;
        private IDisposable _unsubscribtionToken;
        private bool _isForwarding = false;

        public bool IsForwarding
        {
            get
            {
                lock (_observableEvents)
                {
                    return _isForwarding;
                }
            }
        }

    

        public void StartForwarding()
        {
            lock (_observableEvents)
            {
                _unsubscribtionToken = _observableEvents.Subscribe(Forward);
                _isForwarding = true;
            }
        }

        public void StopForwarding()
        {
            lock (_observableEvents)
            {
                _unsubscribtionToken.Dispose();
               _isForwarding = false;
            }
        }

        private static void CreateTcpClient(string configPath)
        {
            if (!File.Exists(configPath))
            {
                _logger.Error("Server address config not exists");
                return;
            }

            string[] file = File.ReadAllLines(configPath);
            if (file.Length == 0)
            {
                _logger.Error("Server address config file is invalid");
                return;
            }

            string[] addressAndPort = file[0].Split(':');
            if (addressAndPort.Length != 2)
            {
                _logger.Error("Server address config is invalid: " + file[0]);
                return;
            }

            IPAddress ipAddress;
            int port;

            _tcpClientLocker.EnterWriteLock();
            if (IPAddress.TryParse(addressAndPort[0], out ipAddress) && Int32.TryParse(addressAndPort[1], out port))
            {
                if (_currentServerIpAddress == ipAddress && _currentServerPort == port && _tcpClient != null)
                    return;
            }
            else
            {
                _logger.Error("Server address config is invalid: " + file[0]);
                return;
            }

            try
            {
                _logger.Info("Server address is " + ipAddress + ":" + port);
                _tcpClient = new TcpClient(new IPEndPoint(IPAddress.Any, 0),
                                           new IPEndPoint(ipAddress, port));
                _currentServerIpAddress = ipAddress;
                _currentServerPort = port;
            }
            finally
            {
                _tcpClientLocker.ExitWriteLock();
            }

        }

        private const int ReconnectScheduled = 1;
        private const int ReconnectNotScheduled = 0;
        private static int _isReconnectSheduled = ReconnectNotScheduled;
        private static void SheduleReconnect()
        {
            if (Interlocked.Exchange(ref _isReconnectSheduled, ReconnectScheduled) == ReconnectNotScheduled)
            {
                _reconnectTimer.Change(TimeSpan.FromSeconds(5), TimeSpan.Zero);
                _logger.Info("Sheduling tcpClient reconnect");
            }
        }

        private void Forward(LogEventArgs eventArgs)
        {
            if (_tcpClient == null)
            {
                SheduleReconnect();
                return;
            }
            byte[] data = eventArgs.LogEvent.Serialize();
            _tcpClientLocker.EnterReadLock();
            try
            {
                _tcpClient.Send(new MessagePacket(data).GetBytesForTransfer());
            }
            catch (Exception ex)
            {
                _logger.Error("Error while sending\r\n" + ex);
            }
            finally
            {
                _tcpClientLocker.ExitReadLock();
            }
        }

    }
}