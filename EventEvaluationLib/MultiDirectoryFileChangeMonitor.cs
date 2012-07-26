using System;
using System.Collections.Generic;
using System.IO;
using NLog;

namespace EventEvaluationLib
{
    public class MultiDirectoryFileChangeMonitor:IDisposable
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger(); 

        private List<FileSystemWatcher> _fileWatchers = new List<FileSystemWatcher>();
        private Action<string> _onCreate;
        private Action<string> _onDelete;
        private Action<string> _onChange;
        private Action<string, string> _onRename;

        public MultiDirectoryFileChangeMonitor(string folderPath, Action<string> onCreate, Action<string> onDelete, Action<string> onChange, Action<string, string> onRename) 
            : this(new List<string>() { folderPath }, onCreate, onDelete, onChange, onRename)
        {
        }

        public MultiDirectoryFileChangeMonitor(List<string> folderPaths, Action<string> onCreate, Action<string> onDelete, Action<string> onChange, Action<string, string> onRename)
        {
            _onCreate = onCreate;
            _onDelete = onDelete;
            _onChange = onChange;
            _onRename = onRename;

            foreach (string folderPath in folderPaths)
            {
                FileSystemWatcher fileWatcher = new FileSystemWatcher(folderPath, "*");
                _logger.Debug("Create watcher for " + folderPath + ". Parameters: _onCreate: " + (_onCreate == null) +
                              ", _onDelete: " + (_onDelete == null) +
                              ", _onChange: " + (_onChange == null) + ", _onRename: " + (_onRename == null));
                fileWatcher.NotifyFilter = NotifyFilters.FileName;
                if (_onCreate != null)
                    fileWatcher.Created += FileCreated;
                if (_onDelete != null)
                    fileWatcher.Deleted += FileDeleted;
                if (_onChange != null)
                {
                    fileWatcher.Changed += FileChanged;
                    fileWatcher.NotifyFilter |= NotifyFilters.LastWrite;
                }
                if (_onRename != null)
                    fileWatcher.Renamed += FileRenamed;
                fileWatcher.EnableRaisingEvents = true;
                _fileWatchers.Add(fileWatcher);
            }
        }

        private void FileRenamed(object sender, RenamedEventArgs e)
        {
            _logger.Debug("File renamed from " + e.OldFullPath + " to " + e.FullPath);
            _onRename(e.OldFullPath, e.FullPath);
        }

        private void FileChanged(object sender, FileSystemEventArgs e)
        {
            _logger.Debug("File "+e.FullPath+" changed");
            _onChange(e.FullPath);
        }

        private void FileDeleted(object sender, FileSystemEventArgs e)
        {
            _logger.Debug("File " + e.FullPath + " deleted");
            _onDelete(e.FullPath);
        }

        private void FileCreated(object sender, FileSystemEventArgs e)
        {
            _logger.Debug("File " + e.FullPath + " created");
            _onCreate(e.FullPath);
        }

        public void Dispose()
        {
            foreach (var fileWatcher in _fileWatchers)
            {
                fileWatcher.Dispose();    
            }
            
        }
    }
}