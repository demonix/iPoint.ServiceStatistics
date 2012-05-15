using System;

namespace EventEvaluationLib.LogReaders
{
    public class LineReadedEventArgs: EventArgs
    {
        public string LogFileName { get; private set; }
        public string Line { get; private set; }

        public LineReadedEventArgs(string logFileName, string line)
        {
            LogFileName = logFileName;
            Line = line;
        }
    }
   
}