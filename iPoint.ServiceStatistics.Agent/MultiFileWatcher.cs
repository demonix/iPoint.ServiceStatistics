using System;
using System.Collections.Generic;
using System.IO;

namespace iPoint.ServiceStatistics.Agent
{
    internal class MultiFileWatcher : IDisposable
    {
        private List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();

        /// <summary>
        /// Occurs when a change is detected in one of the monitored files.
        /// </summary>
        public event EventHandler OnChange;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.StopWatching();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Stops the watching.
        /// </summary>
        public void StopWatching()
        {
            lock (this)
            {
                foreach (FileSystemWatcher watcher in this.watchers)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                }

                this.watchers.Clear();
            }
        }

        /// <summary>
        /// Watches the specified files for changes.
        /// </summary>
        /// <param name="fileNames">The file names.</param>
        public void Watch(IEnumerable<string> fileNames)
        {
            if (fileNames == null)
            {
                return;
            }

            foreach (string s in fileNames)
            {
                this.Watch(s);
            }
        }

     
        internal void Watch(string fileName)
        {
            var watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(fileName),
                Filter = Path.GetFileName(fileName),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size | NotifyFilters.Security | NotifyFilters.Attributes
            };

            watcher.Created += this.OnWatcherChanged;
            watcher.Changed += this.OnWatcherChanged;
            watcher.Deleted += this.OnWatcherChanged;
            watcher.EnableRaisingEvents = true;

            lock (this)
            {
                this.watchers.Add(watcher);
            }
        }

        private void OnWatcherChanged(object source, FileSystemEventArgs e)
        {
            lock (this)
            {
                if (this.OnChange != null)
                {
                    this.OnChange(source, e);
                }
            }
        }
    }
}