using System;
using System.IO;
using System.Text;
using System.Threading;
using NLog;

namespace EventEvaluationLib.LogReaders
{
    public abstract class LogReaderBase : ILogReader
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        protected string LogFileName;
        protected long CurrentPosition;
        protected StreamReader LogFileStreamReader;
        protected Encoding LogFileEncoding;
        private bool _reading;
        private readonly object _locker = new object();
        protected FileSystemWatcher Watcher;
        public event EventHandler<LineReadedEventArgs> LineReaded;

        public event EventHandler<EventArgs> FinishedReading;
        private readonly EventWaitHandle _mayStopFlag = new EventWaitHandle(true, EventResetMode.ManualReset);


        protected LogReaderBase(string logFileName, Encoding encoding):this(logFileName, 0 , encoding)
        {
        }

        protected LogReaderBase(string logFileName, long currentPosition, Encoding encoding)
        {
            _logger.Debug("Logger for " + logFileName+ " created. Begin reading from offset " + currentPosition);
            LogFileName = logFileName;
            LogFileEncoding = encoding;
            CurrentPosition = currentPosition;
            CreateWatcher();
        }

        protected LogReaderBase(Stream stream, Encoding encoding)
        {
            LogFileName = "undefined";
            LogFileEncoding = encoding;
            CurrentPosition = stream.Position;
            CreateWatcher();
        }

        private FileInfo _fileInfo;
        private Timer _fileInfoRefreshTimer;
        private void CreateWatcher()
        {
            _fileInfo = new FileInfo(Path.GetFullPath(LogFileName));
            _fileInfoRefreshTimer = new Timer(RefreshFileInfo);
            _fileInfoRefreshTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1));
            Watcher = new FileSystemWatcher(Path.GetDirectoryName(Path.GetFullPath(LogFileName)),
                                             Path.GetFileName(LogFileName));
            Watcher.NotifyFilter = NotifyFilters.Size | NotifyFilters.LastWrite;
            Watcher.EnableRaisingEvents = false;
            Watcher.Changed += FileSystemWatcherFired;
        }

        private void RefreshFileInfo(object state)
        {
            try
            {
                //NOTE: винда обновляет информациию о файле при закрытии хендла. Поэтому открываем и тут же закрываем хендл :)
                _logger.Trace("RefreshFileInfo for " + _fileInfo.FullName);
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
            _readInternal = ReadInternal;
            _readInternal.Invoke();
        }


        protected void FileSystemWatcherFired(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            _logger.Trace("FileSystemWatcher Fired for " + fileSystemEventArgs.FullPath);
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
                Watcher.EnableRaisingEvents = false;
                string line = null;
                bool eof = false;
                while (!_stopRequested)
                {
                    try
                    {
                        line = LogFileStreamReader.ReadLine();
                    }
                    catch(Exception ex)
                    {
                        _logger.Warn("Error while reading file " + _fileInfo.FullName + ": "+ex.Message);
                        line = null;
                        ReCreateReader();
                    }
                    if (line == null)
                        break;
                    CurrentPosition = LogFileStreamReader.GetRealPosition();
                    if (LineReaded != null)
                    {
                        LineReaded(this, new LineReadedEventArgs(LogFileName, line));
                    }
                }

                if (FinishedReading != null)
                    FinishedReading(this, new EventArgs());
                if (!_stopRequested)
                    Watcher.EnableRaisingEvents = true;
                _reading = false;
                _mayStopFlag.Set();
            }

        }


        protected abstract void CreateReader();

        public virtual void Close()
        {
            
            Watcher.EnableRaisingEvents = false;
            _stopRequested = true;
            _mayStopFlag.WaitOne();
            if (LogFileStreamReader != null)
                LogFileStreamReader.Dispose();
            if (Watcher != null)
                Watcher.Dispose();
            if (_fileInfoRefreshTimer != null)
                _fileInfoRefreshTimer.Dispose();
            _mayStopFlag.Close();
        }

        public void Dispose()
        {
            _logger.Debug("Logger for " + LogFileName + " disposed");
            Close();
            LineReaded = null;
        }

        protected abstract void ReCreateReader();
    }
}