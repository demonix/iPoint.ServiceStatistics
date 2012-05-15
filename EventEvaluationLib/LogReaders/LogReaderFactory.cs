using System.Text;
using System.Threading;

namespace EventEvaluationLib.LogReaders
{
    public class LogReaderFactory
    {

        public  ILogReader CreateReader(string logFileName, Encoding encoding)
        {
            Thread.Sleep(500); //NOTE: file might be locked just after creating, so wait a bit
            return new TextLogReader(logFileName, encoding);
        }

        public ILogReader CreateReader(string logFileName, long currentPosition, Encoding encoding)
        {
            Thread.Sleep(500); //NOTE: file might be locked just after creating, so wait a bit
            return new TextLogReader(logFileName, currentPosition, encoding);
        }
    }
}