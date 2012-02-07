using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace iPoint.ServiceStatistics.Server
{
    public class ExtendedDataTransformations
    {
        private Dictionary<string, string> _dict;
        private ReaderWriterLockSlim _rwLocker = new ReaderWriterLockSlim();
        public ExtendedDataTransformations()
        {
            _dict = new Dictionary<string, string>();
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExtendedDataTransformations");
            if (!Directory.Exists(path))
                return;
            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo fileInfo in files)
            {
                string[] extDatas = File.ReadAllLines(fileInfo.FullName);
                foreach (string extData in extDatas)
                {
                    if (String.IsNullOrEmpty(extData))
                        continue;
                    string[] mayBeReplicas = extData.Split(new []{'|',';'}, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string mayBeReplica in mayBeReplicas)
                    {
                        if (!_dict.ContainsKey(mayBeReplica.Trim()))
                            _dict.Add(mayBeReplica.Trim(), fileInfo.Name);
                    }
                }
            }
        }

        private HashSet<string> _newTransforms = new HashSet<string>();
        public string GetCounterNameReplacement(string from)
        {
            try
            {
                _rwLocker.EnterReadLock();
                if (_dict.ContainsKey(from))
                {

                    if (!_newTransforms.Contains(_dict[from]))
                    {
                        lock (_newTransforms)
                        {
                            if (!_newTransforms.Contains(_dict[from]))
                            {
                                File.AppendAllText("newTransforms", _dict[from] + "\r\n");
                                _newTransforms.Add(_dict[from]);
                            }
                        }
                        
                    }
                return "_"+_dict[from];
                }
                return "";
            }
            finally 
            {
                _rwLocker.ExitReadLock();
            }
            
        }

    }
}