﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cons
{
    public class HelpScreen
    {
        private const string HelpMessage = @"
Transact engine

Usage:

transact.exe 
    [--database postgres|memory] 
    [--connection-string string]
    [--listen-port number]
    [--listen-scheme http|https]

Note that --connection-string is not required for in-memory sessions.
The default database is memory.
";
        public void Show()
        {
            Console.WriteLine(HelpMessage);
        }
    }
}
