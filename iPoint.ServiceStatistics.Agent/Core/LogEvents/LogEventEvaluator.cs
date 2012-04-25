using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using iPoint.ServiceStatistics.Agent.Core.LogFiles;

namespace iPoint.ServiceStatistics.Agent.Core.LogEvents
{
    public class LogEventEvaluator
    {
        ReaderWriterLockSlim _rwLocker = new ReaderWriterLockSlim();
        private List<LogEventEvaluationRule> _eventEvaluationRules;

        public LogEventEvaluator(List<LogEventEvaluationRule> eventEvaluationRules)
        {
            _eventEvaluationRules = eventEvaluationRules;
        }
        
        
        public LogEventEvaluator()
        {
            _eventEvaluationRules = new List<LogEventEvaluationRule>();
            
        }

        public void RegisterRule(LogEventEvaluationRule rule)
        {
            _rwLocker.EnterWriteLock();
            
            try
            {
                _eventEvaluationRules.Add(rule);
            }
            finally
            {
                _rwLocker.ExitWriteLock();
            }
        }
        
        public void UnregisterRule(LogEventEvaluationRule rule)
        {
            _rwLocker.EnterWriteLock();

            try
            {
            _eventEvaluationRules.Remove(rule);
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
                foreach (LogEventEvaluationRule eventEvaluationRule in _eventEvaluationRules)
                {
                    Match m = eventEvaluationRule.Match(line);
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