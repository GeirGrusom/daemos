// <copyright file="CompilationFailedException.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;

namespace Daemos.Scripting
{
    public class CompilationError
    {
        public int Line { get; }

        public int Column { get; }

        public string Message { get; }

        public CompilationError(int line, int column, string message)
        {
            Line = line;
            Column = column;
            Message = message;
        }
    }

    public class CompilationFailedException : Exception
    {
        public IReadOnlyList<CompilationError> Errors { get; }

        public CompilationFailedException(string message, IEnumerable<CompilationError> errors)
            : base(message)
        {
            Errors = errors.ToList().AsReadOnly();
        }
    }
}
