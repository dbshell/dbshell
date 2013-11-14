﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Markup;
using DbShell.Common;
using DbShell.Driver.Common.Structure;
using DbShell.Driver.Common.Utility;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using log4net;

namespace DbShell.Core.Runtime
{
    public class ShellContext : IShellContext, IDisposable
    {
        private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Dictionary<string, DatabaseInfo> _dbCache = new Dictionary<string, DatabaseInfo>();
        private readonly ScriptEngine _engine;
        private readonly List<ScriptScope> _scopeStack = new List<ScriptScope>();
        private readonly List<string> _executingFolderStack = new List<string>();
        private ShellRunner _runner;
        private Dictionary<ResolveFileMode, List<string>> _additionalSearchFolders = new Dictionary<ResolveFileMode, List<string>>();

        public ShellContext(ShellRunner runner)
        {
            _runner = runner;
            _engine = Python.CreateEngine();
            _scopeStack.Add(_engine.CreateScope());
        }

        public DatabaseInfo GetDatabaseStructure(IConnectionProvider connection)
        {
            string key = connection.ProviderString;
            if (!_dbCache.ContainsKey(key))
            {
                _log.InfoFormat("DBSH-00076 Downloading structure for connection {0}", connection);
                OutputMessage(String.Format("Downloading structure for connection {0}", connection));
                var analyser = connection.Factory.CreateAnalyser();
                using (var conn = connection.Connect())
                {
                    analyser.Connection = conn;
                    analyser.FullAnalysis();
                    _dbCache[key] = analyser.Structure;
                }
            }
            return _dbCache[key];
        }

        public void PutDatabaseInfoCache(string providerKey, DatabaseInfo db)
        {
            _dbCache[providerKey] = db;
        }

        private ScriptScope Scope
        {
            get { return _scopeStack.Last(); }
        }

        public void Dispose()
        {
        }

        public object Evaluate(string expression)
        {
            return _engine.Execute(expression, Scope);
        }

        public object GetVariable(string name)
        {
            return Scope.GetVariable(name);
        }

        public void SetVariable(string name, object value)
        {
            Scope.SetVariable(name, value);
        }

        public void EnterScope()
        {
            _scopeStack.Add(_engine.CreateScope(Scope));
        }

        public void LeaveScope()
        {
            _scopeStack.RemoveAt(_scopeStack.Count - 1);
        }

        private string ReplaceMatch(Match m)
        {
            return Evaluate(m.Groups[1].Value).SafeToString();
        }

        public string Replace(string replaceString, string replacePattern = null)
        {
            if (replaceString == null) return null;
            return Regex.Replace(replaceString, replacePattern ?? @"\$\{([^\}]+)\}", ReplaceMatch);
        }

        public void IncludeFile(string file, IShellElement parent)
        {
            using (var fr = new FileInfo(file).OpenRead())
            {
                object obj = XamlReader.Load(fr);
                var runnable = obj as IRunnable;
                if (runnable == null) throw new Exception(String.Format("DBSH-00059 Included file {0} doesn't contain root element implementing IRunnable", file));
                var shellElem = obj as IShellElement;
                if (shellElem != null) ShellRunner.ProcessLoadedElement(shellElem, parent, this);
                try
                {
                    PushExecutingFolder(Path.GetDirectoryName(file));
                    runnable.Run();
                }
                finally
                {
                    PopExecutingFolder();
                }
            }
        }

        private string SearchExistingFile(string file, ResolveFileMode mode, params string[] folders)
        {
            foreach (string folder in folders)
            {
                if (folder == null) continue;
                string fn = Path.Combine(folder, file);
                if (System.IO.File.Exists(fn)) return fn;
            }
            if (_additionalSearchFolders.ContainsKey(mode))
            {
                foreach (string folder in _additionalSearchFolders[mode])
                {
                    string fn = Path.Combine(folder, file);
                    if (System.IO.File.Exists(fn)) return fn;
                }
            }
            if (System.IO.File.Exists(file)) return file;

            var allFolders = new List<string>(folders);
            if (_additionalSearchFolders.ContainsKey(mode))
            {
                allFolders.AddRange(_additionalSearchFolders[mode]);
            }
            throw new Exception(String.Format("DBSH-00063 Could not find file {0}, searched in folders {1}", file, allFolders.CreateDelimitedText(";")));
        }

        public string ResolveFile(string file, ResolveFileMode mode)
        {
            switch (mode)
            {
                case ResolveFileMode.DbShell:
                    return SearchExistingFile(file, mode, GetExecutingFolder());
                case ResolveFileMode.Template:
                    return SearchExistingFile(file, mode, GetTemplatesFolder(), GetExecutingFolder());
                case ResolveFileMode.Input:
                    return SearchExistingFile(file, mode, GetExecutingFolder());
                case ResolveFileMode.Output:
                    if (DefaultOutputFolder != null) return Path.Combine(DefaultOutputFolder, file);
                    return file;
            }
            return file;
        }

        private string GetDbShellFolder()
        {
            var asm = System.Reflection.Assembly.GetEntryAssembly();
            if (asm == null) return null;
            string file = asm.Location;
            return Path.GetDirectoryName(Path.GetDirectoryName(file));
        }

        private string GetTemplatesFolder()
        {
            string folder = GetDbShellFolder();
            if (folder == null) return null;
            return Path.Combine(folder, "Templates");
        }

        //private string GetExecutingFolder()
        //{
        //    string file = GetExecutingFile();
        //    if (file != null) return Path.GetDirectoryName(file);
        //    return null;
        //}

        public void PushExecutingFolder(string folder)
        {
            _executingFolderStack.Add(folder);
        }

        public void PopExecutingFolder()
        {
            _executingFolderStack.RemoveAt(_executingFolderStack.Count - 1);
        }

        public string GetExecutingFolder()
        {
            if (_executingFolderStack.Count > 0) return _executingFolderStack[_executingFolderStack.Count - 1];
            return null;
        }

        public void OutputMessage(string message)
        {
            if (_runner != null) _runner.OnOutputMessage(message);
        }

        public void AddSearchFolder(ResolveFileMode mode, string folder)
        {
            if (!_additionalSearchFolders.ContainsKey(mode))
            {
                _additionalSearchFolders[mode] = new List<string>();
            }
            _additionalSearchFolders[mode].Add(folder);
        }

        public IConnectionProvider DefaultConnection { get; set; }
        public string DefaultOutputFolder { get; set; }
    }
}
