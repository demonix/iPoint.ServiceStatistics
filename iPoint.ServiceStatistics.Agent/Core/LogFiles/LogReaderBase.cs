using System;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using NLog;
using iPoint.ServiceStatistics.Agent.Core.LogEvents;

namespace iPoint.ServiceStatistics.Agent.Core.LogFiles
{
    public abstract class LogReaderBase : ILogReader
    {
        protected string _logFileName;
        protected long _currentPosition;
        protected StreamReader _logFileStreamReader;
        protected Encoding _logFileEncoding;
        private bool _reading = false;
        private object _locker = new object();
        protected FileSystemWatcher _watcher;
        public event EventHandler<LineReadedEventArgs> LineReaded;
        public event EventHandler<LogEventArgs> OnLogEvent;

        public event EventHandler<EventArgs> FinishedReading;
        private readonly LogDescription _logDescription;
        protected readonly LogEventMatcher LogEventMatcher;
        private EventWaitHandle _mayStopFlag = new EventWaitHandle(true, EventResetMode.ManualReset);

        public LogDescription LogDescription
        {
            get { return _logDescription; }
        }

        public LogReaderBase(string logFileName, Encoding encoding, LogDescription logDescription,
                             LogEventMatcher logEventMatcher)
        {
            _logFileName = logFileName;
            _logFileEncoding = encoding;
            _logDescription = logDescription;
            LogEventMatcher = logEventMatcher;
            _currentPosition = 0;
            CreateWatcher();
        }

        public LogReaderBase(string logFileName, long currentPosition, Encoding encoding, LogDescription logDescription,
                             LogEventMatcher logEventMatcher)
        {
            _logFileName = logFileName;
            _logFileEncoding = encoding;
            _logDescription = logDescription;
            LogEventMatcher = logEventMatcher;
            _currentPosition = currentPosition;
            CreateWatcher();
        }

        public LogReaderBase(Stream stream, Encoding encoding, LogDescription logDescription,
                             LogEventMatcher logEventMatcher)
        {
            _logFileName = "undefined";
            _logFileEncoding = encoding;
            _logDescription = logDescription;
            LogEventMatcher = logEventMatcher;
            _currentPosition = stream.Position;
            CreateWatcher();
        }

        private FileInfo _fileInfo;
        private Timer _fileInfoRefreshTimer;
        private void CreateWatcher()
        {
            _fileInfo = new FileInfo(Path.GetFullPath(_logFileName));
            _fileInfoRefreshTimer = new Timer(RefreshFileInfo);
            _fileInfoRefreshTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
            _watcher = new FileSystemWatcher(Path.GetDirectoryName(Path.GetFullPath(_logFileName)),
                                             Path.GetFileName(_logFileName));
            _watcher.NotifyFilter = NotifyFilters.Size | NotifyFilters.LastWrite;
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed += FileSystemWatcherFired;
        }

        private void RefreshFileInfo(object state)
        {
            try
            {
                //NOTE: винда обновляет информациию о файле при закрытии хендла. Поэтому открываем и тут же закрываем хендл :)
                _fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite).Close();
                _fileInfo.Refresh();
            }
            catch(Exception ex)
            {
                LogManager.GetCurrentClassLogger().Debug("Something went wrong during refreshing FileInfo data:\r\n" + ex );
            }

           

        }

        protected delegate void ReadInternalDelegate();

        private ReadInternalDelegate _readInternal;
        private bool _stopRequested;

        public void BeginRead()
        {
            _readInternal = new ReadInternalDelegate(ReadInternal);
            _readInternal.Invoke();
        }


        protected void FileSystemWatcherFired(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            _readInternal.Invoke();
        }

        protected void ReadInternal()
        {
            if (_reading) return;
            lock (_locker)
            {
                if (_reading) return;
                _mayStopFlag.Reset();
                _reading = true;
                _watcher.EnableRaisingEvents = false;
                string line;
                while (((line = _logFileStreamReader.ReadLine()) != null) && !_stopRequested)
                {
                    _currentPosition = _logFileStreamReader.GetRealPosition();
                    if (LineReaded != null)
                    {
                        LineReaded(this, new LineReadedEventArgs(_logFileName, line, LogEventMatcher));
                        
                    }
                    if (OnLogEvent != null)
                    {
                        foreach (LogEvent logEvent in LogEventMatcher.FindMatches(_logFileName, line))
                        {
                            OnLogEvent(this, new LogEventArgs(logEvent));
                        }
                    }
                }

                if (FinishedReading != null)
                    FinishedReading(this, new EventArgs());
                if (!_stopRequested)
                    _watcher.EnableRaisingEvents = true;
                _reading = false;
                _mayStopFlag.Set();
            }

        }


        protected abstract void CreateReader();

        public void Close()
        {
            
            _watcher.EnableRaisingEvents = false;
            _stopRequested = true;
            _mayStopFlag.WaitOne();
            if (_logFileStreamReader != null)
                _logFileStreamReader.Dispose();
            if (_watcher != null)
                _watcher.Dispose();
            if (_fileInfoRefreshTimer != null)
                _fileInfoRefreshTimer.Dispose();
            _mayStopFlag.Close();
        }
    }
}