using System.IO;
using System.Text;

namespace EventEvaluationLib.LogReaders
{
    public sealed class TextLogReader : LogReaderBase
    {
        private FileStream _logFileStream;

        public TextLogReader(string logFileName, Encoding encoding)
            : base(logFileName, 0, encoding)
        {
            CreateReader();
        }

        public TextLogReader(string logFileName, long currentPosition, Encoding encoding)
            : base(logFileName, currentPosition, encoding)
        {
            CreateReader();
        }

        protected override void ReCreateReader()
        {
            LogFileStreamReader.Close();
            _logFileStream.Close();
            if (File.Exists(LogFileName))
                CreateReader();
        }

        protected override void CreateReader()
        {
            _logFileStream = new FileStream(LogFileName,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete,8*1024,FileOptions.SequentialScan);
            _logFileStream.Seek(CurrentPosition, SeekOrigin.Begin);
            LogFileStreamReader = new StreamReader(_logFileStream, LogFileEncoding);
        }

        
    }
}
