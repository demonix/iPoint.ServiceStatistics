using System.IO;
using System.Text;
using System.Threading;
using iPoint.ServiceStatistics.Agent.Core.LogEvents;

namespace iPoint.ServiceStatistics.Agent.Core.LogFiles
{
    public sealed class TextLogReader : LogReaderBase, ILogReader
    {
        private FileStream _logFileStream;

        public TextLogReader(string logFileName, Encoding encoding, LogDescription logDescription, LogEventMatcher logEventMatcher)
            : base(logFileName, 0, encoding, logDescription, logEventMatcher)
        {
            CreateReader();
        }

        public TextLogReader(string logFileName, long currentPosition, Encoding encoding, LogDescription logDescription, LogEventMatcher logEventMatcher)
            : base(logFileName, currentPosition, encoding, logDescription, logEventMatcher)
        {
            CreateReader();
        }


        protected override void CreateReader()
        {
            Thread.Sleep(500); //file might be locked when created.
            _logFileStream = new FileStream(_logFileName,FileMode.Open,FileAccess.Read,FileShare.ReadWrite,8*1024,FileOptions.SequentialScan);
            _logFileStream.Seek(_currentPosition, SeekOrigin.Begin);
            _logFileStreamReader = new StreamReader(_logFileStream, _logFileEncoding);
        }

        public new void Close()
        {
            base.Close();
        }
    }
}
