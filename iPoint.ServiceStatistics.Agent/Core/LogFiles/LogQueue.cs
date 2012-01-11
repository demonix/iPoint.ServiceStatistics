using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using iPoint.ServiceStatistics.Agent.Core.LogEvents;


namespace iPoint.ServiceStatistics.Agent.Core.LogFiles
{
    public class LogQueue  
    {
        Dictionary<string, LogDescription> _logDescriptions = new Dictionary<string, LogDescription>();
        Dictionary<string, FileSystemWatcher> _logDirectoryWatchers = new Dictionary<string, FileSystemWatcher>();
        List<ILogReader> _logReaders = new List<ILogReader>();
        
        public void AddQueueWatcher (LogDescription logDescription)
        {
            string fullpath = Path.GetFullPath(logDescription.LogDirectory);

            if (_logDescriptions.ContainsKey(fullpath))
                throw new Exception(String.Format("Directory {0} already in watch list", fullpath));
            _logDescriptions.Add(fullpath, logDescription);
            FileSystemWatcher fsWatcher = new FileSystemWatcher();
            fsWatcher.Path = fullpath;
            fsWatcher.Created += fsWatcher_Created;
        }

        void fsWatcher_Created(object sender, FileSystemEventArgs e)
        {
            ILogReader textLogReader = new TextLogReader(e.FullPath, Encoding.Default, _logDescriptions[e.FullPath],
                                                         new LogEventMatcher(new List<LogEventDescription>()
                                                                                 {new TestLogEventDescription()}));
            textLogReader.LineReaded += WriteLineToConsole;
            _logReaders.Add(textLogReader);
        }

        void WriteLineToConsole(object sender, LineReadedEventArgs eventArgs)
        {

            Console.WriteLine(((LogReaderBase)sender).LogDescription.LogDirectory +": "+ eventArgs.Line);
        }
    }
}