using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace EventEvaluationLib
{
    internal class LogEventEvaluator
    {
        ReaderWriterLockSlim _rwLocker = new ReaderWriterLockSlim();
        List<EventEvaluatorRule> _rules = new List<EventEvaluatorRule>();

        public void Configure(List<EventEvaluatorRule> rules)
        {
            _rwLocker.EnterWriteLock();
            try
            {
                _rules = rules;
            }
            finally
            {
                _rwLocker.ExitWriteLock();
            }
        }

        public IEnumerable<LogEvent> Evaluate(string logFileName, string line)
        {
            _rwLocker.EnterReadLock();
            try
            {
                foreach (EventEvaluatorRule eventEvaluationRule in _rules)
                {
                    Match m = eventEvaluationRule.RegexRule.Match(line);
                    if (!m.Success) continue;
                    LogEvent evt = new LogEvent
                                       {
                                           Source = eventEvaluationRule.GetSource(logFileName, m),
                                           Category = eventEvaluationRule.GetCounterCategory(logFileName, m),
                                           Counter = eventEvaluationRule.GetCounterName(logFileName, m),
                                           Instance = eventEvaluationRule.GetInstance(logFileName, m),
                                           Type = eventEvaluationRule.EventType,
                                           DateTime = eventEvaluationRule.GetDateTime(logFileName, m),

                                           ExtendedData = eventEvaluationRule.GetExtendedData(logFileName, m),
                                           Value = eventEvaluationRule.GetValue(logFileName, m)
                                       };
                    yield return evt;
                }
            }
            finally
            {
                _rwLocker.ExitReadLock();
            }

        }

    }
}