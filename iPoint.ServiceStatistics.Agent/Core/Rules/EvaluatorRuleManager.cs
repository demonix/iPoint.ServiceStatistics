using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NLog;

namespace iPoint.ServiceStatistics.Agent.Core.Rules
{
    public class EvaluatorRuleManager : ChangeMonitorableConfigsDirectory, IDisposable
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private Dictionary<string, EvaluatorRule> _rules = new Dictionary<string, EvaluatorRule>();
        private ReaderWriterLockSlim _rwLocker = new ReaderWriterLockSlim();


        public EvaluatorRuleManager(string directoryPath) : base(directoryPath, "*")
        {
            
            FileInfo[] fileInfos = new DirectoryInfo(directoryPath).GetFiles();
            foreach (FileInfo fileInfo in fileInfos)
            {
                AddRule(fileInfo.FullName, CreateEvaluatorRule(fileInfo.FullName));
            }
        }


        public IEnumerable<EvaluatorRule> Rules
        {
            get
            {
                _rwLocker.EnterReadLock();
                try
                {
                    foreach (KeyValuePair<string, EvaluatorRule> evaluatorRule in _rules)
                    {
                        yield return evaluatorRule.Value;
                    }
                }
                finally
                {
                    _rwLocker.ExitReadLock();
                }
            }
        }

        #region IDisposable Members

        public new void Dispose()
        {
            base.Dispose();
            RemoveAllRules();
        }

        #endregion

        private static EvaluatorRule CreateEvaluatorRule(string filePath)
        {
            EvaluatorRule rule = null;
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                EvaluatorRuleConfig evaluatorRuleConfig = new EvaluatorRuleConfig(fileInfo.FullName);
                rule = new EvaluatorRule(evaluatorRuleConfig);
            }
            catch (Exception ex)
            {
                _logger.Error("Cannot create rule from rule config file " + filePath + ": " + ex);
            }
            return rule;
        }

        protected override void OnConfigAdded(string filePath)
        {
             AddRule(filePath, CreateEvaluatorRule(filePath));
        }
        protected override void OnConfigRemoved(string filePath)
        {
            RemoveRule(filePath);
        }
        
        private void UpdateRule(object sender, ConfigChangedEventArgs e)
        {
            EvaluatorRule rule = CreateEvaluatorRule(e.ConfigPath);
            RemoveRule(e.ConfigPath);
            AddRule(e.ConfigPath, rule);
        }

        private void AddRule(string fullPath, EvaluatorRule evaluatorRule)
        {
            if (evaluatorRule == null) return;
            _rwLocker.EnterWriteLock();
            try
            {
                if (_rules.ContainsKey(fullPath))
                {
                    _logger.Warn(String.Format("Rule from file {0} already added.", fullPath));
                    return;
                }
                _rules.Add(fullPath, evaluatorRule);
                evaluatorRule.Config.WatchForChanges();
                evaluatorRule.Config.ConfigChanged += UpdateRule;
                evaluatorRule.Config.ConfigRenamed += RenameRule;
                _logger.Info("New rule added " + evaluatorRule.Id);
            }
            finally
            {
                _rwLocker.ExitWriteLock();
            }
        }

        private void RenameRule(object sender, ConfigRenamedEventArgs e)
        {
            _rwLocker.EnterWriteLock();
            try
            {
                _rules.Add(e.NewFileName,_rules[e.OldFileName]);
                _rules.Remove(e.OldFileName);
            }
            finally
            {
                _rwLocker.ExitWriteLock();
            }
        }

        private void RemoveAllRules()
        {
            _rwLocker.EnterWriteLock();
            try
            {
                foreach (var key in _rules.Keys)
                {
                    _rules[key].Dispose();
                }
                _rules.Clear();
                _logger.Info("All rules removed");
            }
            finally
            {
                _rwLocker.ExitWriteLock();
            }
        }

        private void RemoveRule(string fullPath)
        {
            _rwLocker.EnterWriteLock();
            try
            {
                if (!_rules.ContainsKey(fullPath))
                {
                    _logger.Warn(String.Format("Rule from file {0} does not exists", fullPath));
                    return;
                }
                _rules[fullPath].Dispose();
                _rules.Remove(fullPath);
                _logger.Info("rule removed " + fullPath);
            }
            finally
            {
                _rwLocker.ExitWriteLock();
            }
        }
    }
}