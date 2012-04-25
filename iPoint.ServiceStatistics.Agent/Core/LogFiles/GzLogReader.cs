using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.GZip;
using iPoint.ServiceStatistics.Agent.Core.LogEvents;

namespace iPoint.ServiceStatistics.Agent.Core.LogFiles
{
    public sealed class GzLogReader : LogReaderBase
    {
        private GZipInputStream _logFileStream;
        public GzLogReader(string logFileName, Encoding encoding, LogDescription logDescription/*, LogEventEvaluator logEventEvaluator*/)
            : base(logFileName, 0, encoding, logDescription/*,logEventEvaluator*/)
        {
            CreateReader();
        }

        public GzLogReader(string logFileName, long currentPosition, Encoding encoding, LogDescription logDescription/*, LogEventEvaluator logEventEvaluator*/)
            : base(logFileName, currentPosition, encoding, logDescription/*,logEventEvaluator*/)
        {
            CreateReader();
        }

        protected override void CreateReader()
        {
            _logFileStream = new GZipInputStream(new FileStream(_logFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,8*1024, FileOptions.SequentialScan));
            if (_currentPosition > 0)
            {
                long seekPosition = 0;
                int readBlockSize = 2;//1024*1024;
                byte[] temp = new byte[readBlockSize];
                while (seekPosition < _currentPosition - readBlockSize)
                {
                    _logFileStream.Read(temp, 0, readBlockSize);
                    seekPosition += readBlockSize;
                }
                _logFileStream.Read(temp, 0, (int)(_currentPosition - seekPosition));
            }
            _logFileStreamReader = new StreamReader(_logFileStream, _logFileEncoding);
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