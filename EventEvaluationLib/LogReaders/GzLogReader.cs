using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;

namespace EventEvaluationLib.LogReaders
{
    public sealed class GzLogReader : LogReaderBase
    {
        private GZipInputStream _logFileStream;
        public GzLogReader(string logFileName, Encoding encoding)
            : base(logFileName, 0, encoding)
        {
            CreateReader();
        }

        public GzLogReader(string logFileName, long currentPosition, Encoding encoding)
            : base(logFileName, currentPosition, encoding)
        {
            CreateReader();
        }

        protected override void CreateReader()
        {
            _logFileStream = new GZipInputStream(new FileStream(LogFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,8*1024, FileOptions.SequentialScan));
            if (CurrentPosition > 0)
            {
                long seekPosition = 0;
                int readBlockSize = 2;//1024*1024;
                byte[] temp = new byte[readBlockSize];
                while (seekPosition < CurrentPosition - readBlockSize)
                {
                    _logFileStream.Read(temp, 0, readBlockSize);
                    seekPosition += readBlockSize;
                }
                _logFileStream.Read(temp, 0, (int)(CurrentPosition - seekPosition));
            }
            LogFileStreamReader = new StreamReader(_logFileStream, LogFileEncoding);
        }

        public new void Close()
        {
            base.Close();
            if (_logFileStream != null)
            {
                _logFileStream.Close();
                _logFileStream.Dispose();
            }
        }

    }
}