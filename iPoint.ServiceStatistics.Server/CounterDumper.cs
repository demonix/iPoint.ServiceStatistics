using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using EventEvaluationLib;

namespace iPoint.ServiceStatistics.Server
{
    public class CounterDumper
    {
        private readonly IObservable<LogEventArgs> _observableEvents;
        private IDisposable _unsubscribtionToken;
        private ConcurrentDictionary<string, FileStream> _currentDumpFiles = new ConcurrentDictionary<string, FileStream>();
        private bool _isDumping = false;

        public bool IsDumping
        {
            get
            {
                lock (_observableEvents)
                {
                    return _isDumping;
                }
            }
        }

    

        public CounterDumper(IObservable<LogEventArgs> observableEvents)
        {
            _observableEvents = observableEvents;
        }
        public void StartDumping()
        {
            lock (_observableEvents)
            {
                _unsubscribtionToken = _observableEvents.Subscribe(DumpCounters);
                _isDumping = true;
            }
        }

        public void StopDumping()
        {
            lock (_observableEvents)
            {
                _unsubscribtionToken.Dispose();
                foreach (FileStream fileStream in _currentDumpFiles.Values)
                {
                    fileStream.Close();
                }
                _currentDumpFiles.Clear();
                _isDumping = false;
            }
        }
        
        private void DumpCounters(LogEventArgs eventArgs)
        {
            FileStream df = GetDumpFile(eventArgs.LogEvent.Category, eventArgs.LogEvent.Counter,
                                        eventArgs.LogEvent.DateTime);
            byte[] toWrite = Encoding.Default.GetBytes(eventArgs.LogEvent.ToString()+"\r\n");
            lock (df)
            {
                df.Write(toWrite, 0, toWrite.Length);    
            }
        }

        FileStream GetDumpFile(string category, string counterName, DateTime dateTime)
        {
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                category = category.Replace(invalidChar, '_');
                counterName = counterName.Replace(invalidChar, '_');
            }
            string dumpDirectory = "dump\\" +category + "\\" + counterName + "\\";
            string currentKey = dumpDirectory + GetDateKey(dateTime);
            string previousKey = dumpDirectory + GetDateKey(dateTime.Subtract(TimeSpan.FromHours(2)));
            FileStream result;
            if (_currentDumpFiles.TryGetValue(currentKey, out result))
            {
                FileStream previousFile;
                if (_currentDumpFiles.TryRemove(previousKey, out previousFile))
                {
                    previousFile.Close();
                }
                return result;
            }
            lock (_observableEvents)
            {
                if (!Directory.Exists(dumpDirectory))
                    Directory.CreateDirectory(dumpDirectory);
                if (!File.Exists(currentKey))
                    File.Create(currentKey).Close();
                else
                {
                    if (_currentDumpFiles.TryGetValue(currentKey, out result))
                        return result;
                }
                Thread.Sleep(100);
                result = new FileStream(currentKey, FileMode.Append, FileAccess.Write, FileShare.Read);
                if (!_currentDumpFiles.TryAdd(currentKey, result))
                {
                    result.Close();
                    _currentDumpFiles.TryGetValue(currentKey, out result);
                }
            }
            return result;
        }
        private string GetDateKey(DateTime dateTime)
        {
            return dateTime.Year + "." + dateTime.Month + "." + dateTime.Day + "." + dateTime.Hour;
        }

    }
}