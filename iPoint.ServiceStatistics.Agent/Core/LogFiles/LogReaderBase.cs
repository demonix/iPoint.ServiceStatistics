using System;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using iPoint.ServiceStatistics.Agent.Core.LogEvents;

namespace iPoint.ServiceStatistics.Agent.Core.LogFiles
{
    public abstract class LogReaderBase : ILogReader
    {
        protected string _logFileName;
        protected long _currentPosition;
        protected StreamReader _logFileStreamReader;
        private byte[] _clrfCheck = new byte[2];
        protected Encoding _logFileEncoding;
        private bool _reading = false;
        private object _locker = new object();
        protected FileSystemWatcher _watcher;
        public event EventHandler<LineReadedEventArgs> LineReaded;
        public event EventHandler<EventArgs> FinishedReading;
        private readonly LogDescription _logDescription;
        protected readonly LogEventMatcher LogEventMatcher;

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


        private void CreateWatcher()
        {
            _watcher = new FileSystemWatcher(Path.GetDirectoryName(Path.GetFullPath(_logFileName)),
                                             Path.GetFileName(_logFileName));
            _watcher.NotifyFilter = NotifyFilters.Size | NotifyFilters.LastWrite;
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed += FileSystemWatcherFired;
        }

        protected delegate void ReadInternalDelegate();

        private ReadInternalDelegate _readInternal;

        public void BeginRead()
        {
            _readInternal = new ReadInternalDelegate(ReadInternal);
            _readInternal.Invoke();
        }


        protected void FileSystemWatcherFired(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            //Console.Out.WriteLine("FileSystemWatcher Fired");
            _readInternal.Invoke();
        }

        protected void ReadInternal()
        {
            if (_reading) return;
            lock (_locker)
            {
                if (_reading) return;
                _reading = true;
                _watcher.EnableRaisingEvents = false;
                string line;
                while ((line = _logFileStreamReader.ReadLine()) != null)
                {
                    _currentPosition = _logFileStreamReader.GetRealPosition();
                    if (LineReaded != null)
                    {
                        LineReaded(this, new LineReadedEventArgs(_logFileName, line, LogEventMatcher));
                    }
                }
            }
            if (FinishedReading != null)
                FinishedReading(this, new EventArgs());

            _watcher.EnableRaisingEvents = true;
            _reading = false;
        }


        protected abstract void CreateReader();

        public void Close()
        {
            if (_watcher != null)
                _watcher.Dispose();
            if (_logFileStreamReader != null)
                _logFileStreamReader.Dispose();
        }
    }
}