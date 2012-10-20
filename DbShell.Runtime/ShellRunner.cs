﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using System.Xml;
using DbShell.Common;
using DbShell.Core;

namespace DbShell.Runtime
{
    public class ShellRunner : IDisposable
    {
        private IRunnable _main;
        private ShellContext _context;

        public void LoadFile(string file)
        {
            CoreLoader.Load();
            using (var fr = new FileInfo(file).OpenRead())
            {
                object obj = XamlReader.Load(fr);
                LoadObject(obj);
            }
        }

        public void LoadString(string content)
        {
            using (var fr = new StringReader(content))
            {
                using (var reader = XmlReader.Create(fr))
                {
                    object obj = XamlReader.Load(reader);
                    LoadObject(obj);
                }
            }
        }

        public void LoadObject(object obj)
        {
            if (_context != null) throw new Exception("Load function already called");
            _main = (IRunnable)obj;
            _context = new ShellContext();
            var element = _main as IShellElement;
            if (element != null) AfterLoad(element, null);
        }

        private void AfterLoad(IShellElement element, IShellElement parent)
        {
            element.Context = _context;
            if (element.Connection == null && parent != null && parent.Connection != null)
            {
                element.Connection = parent.Connection;
            }
            element.EnumChildren(child => AfterLoad(child, element));
        }

        public void Run()
        {
            if (_context == null) throw new Exception("Load function not called");
            _main.Run();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
