using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using EventEvaluationLib.LogReaders;
using NLog;

namespace EventEvaluationLib
{
    public class LogWatcher: ChangeMonitorableConfigStore
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger(); 
        public List<string> WatchedDirectories;
        public Regex FileMask { get; private set; }
        public Encoding Encoding { get; private set; }
        private readonly string _logWatcherConfigFilePath;
        private readonly string _logEventEvaluatorRulesConfigDirectoryPath;
        private readonly Dictionary<string, ILogReader> _readers = new Dictionary<string, ILogReader>();
        private readonly LogEventEvaluator _eventEvaluator = new LogEventEvaluator();
        private readonly LogReaderFactory _logReaderFactory = new LogReaderFactory();
        private readonly MultiDirectoryFileChangeMonitor _multiDirectoryFileChangeMonitor;
        public event EventHandler<LogEventArgs> EventFromLog;

        

        public override void Dispose()
        {
            EventFromLog = null;
            RemoveAllReaders();
            _multiDirectoryFileChangeMonitor.Dispose();
            base.Dispose();
        }

        private void RemoveAllReaders()
        {
            lock (_readers)
            {
                foreach (var key in _readers.Keys)
                {
                    _readers[key].Dispose();
                }
                _readers.Clear();
            }
        }

        public LogWatcher(string configFilePath, List<string> watchedDirectories, Regex fileMask, Encoding encoding)
            : base(Path.ChangeExtension(configFilePath, "rules"), "*")
        {
            _logger.Info("Watcher for "+configFilePath+" monitors files in folders:\r\n" + String.Join("\r\n",watchedDirectories.ToArray()));
            _logWatcherConfigFilePath = configFilePath;
            _logEventEvaluatorRulesConfigDirectoryPath = Path.ChangeExtension(_logWatcherConfigFilePath, "rules");
            if (!Directory.Exists(_logEventEvaluatorRulesConfigDirectoryPath)) 
                throw new Exception("Directory with rules does not exists");

            WatchedDirectories = watchedDirectories;
            FileMask = fileMask;
            Encoding = encoding;
            ReloadRules();
            foreach (string logDirectory in WatchedDirectories)
            {
                List<string> currentLogs = new DirectoryInfo(logDirectory).GetFiles().Where(fi => fi.LastWriteTime >= DateTime.Now.Date).Select(fi => fi.FullName).ToList();
                foreach (var currentLog in currentLogs)
                {
                    AddReaderWithFileNameCheck(currentLog);
                }
            }
            _multiDirectoryFileChangeMonitor = new MultiDirectoryFileChangeMonitor(watchedDirectories, AddReaderWithFileNameCheck, RemoveReaderWithFileNameCheck, null, null);
        }
        

        private void RemoveReaderWithFileNameCheck(string path)
        {
            if (!FileMask.IsMatch(Path.GetFileName(path)))
                return;
            lock (_readers)
            {
                if (_readers.ContainsKey(path))
                {
                    _readers[path].Dispose();
                    _readers.Remove(path);
                }
            }
        }

        private void AddReaderWithFileNameCheck(string path)
        {
            if (!FileMask.IsMatch(Path.GetFileName(path)))
                return;
            lock (_readers)
            {
                if (!_readers.ContainsKey(path))
                {
                    ILogReader reader = _logReaderFactory.CreateReader(path, new FileInfo(path).Length, Encoding);
                    reader.LineReaded += EvaluateEvents;
                    reader.BeginRead();
                    _readers.Add(path, reader);
                }
            }
        }

        private void EvaluateEvents(object sender, LineReadedEventArgs e)
        {
            var handler = EventFromLog;
            if (handler == null) return;
            foreach (var logEvent in _eventEvaluator.Evaluate(e.LogFileName, e.Line))
            {
                handler(this, new LogEventArgs(logEvent));
                
            }
        }

        private void ReloadRules()
        {
            List<EventEvaluatorRule> rules =new List<EventEvaluatorRule>();
            FileInfo[] fileInfos = new DirectoryInfo(_logEventEvaluatorRulesConfigDirectoryPath).GetFiles();
            foreach (FileInfo fileInfo in fileInfos)
            {
                EventEvaluatorRule rule = EventEvaluatorRule.CreateFromFile(fileInfo.FullName);
                if (rule != null)
                    rules.Add(rule);
            }
            _eventEvaluator.Configure(rules);
            _logger.Debug("Rules for " + _logWatcherConfigFilePath + " were reloaded");
        }

       

        public static LogWatcher CreateFromFile(string configFilePath)
        {
            try
            {
                SettingsReader settingsReader = new SettingsReader(configFilePath);
                Regex fileMask = new Regex(settingsReader.GetConfigParam("FileMask"), RegexOptions.Compiled);
                Encoding encoding = Encoding.GetEncoding(settingsReader.GetConfigParam("Encoding"));
                IEnumerable<string> possibleLogDirectories =
                    settingsReader.GetConfigParams("LogDirectory").SelectMany(
                        FilePathHelpers.FindDirectoriesOnFixedDisks);
                List<string> directories =
                    possibleLogDirectories.SelectMany(FilePathHelpers.GetDirectoriesByMaskedPath).Distinct().ToList();
                return new LogWatcher(configFilePath, directories, fileMask, encoding);
            }
            catch (Exception ex)
            {
                _logger.Error("Cannot load LogWatcher from " + configFilePath + ": " + ex);
                return null;
            }
        }

        protected override void ConfigChanged(string fileName)
        {
            
            ReloadRules();
        }

        protected override void ConfigRenamed(string oldFileName, string newFileName)
        {
            ReloadRules();
        }

        protected override void ConfigCreated(string fileName)
        {
            ReloadRules();
        }

        protected override void ConfigDeleted(string fileName)
        {
            ReloadRules();
        }
    }
}