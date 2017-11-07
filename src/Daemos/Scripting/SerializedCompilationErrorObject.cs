// <copyright file="SerializedCompilationErrorObject.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;

namespace Daemos.Scripting
{
    public class SerializedCompilationError
    {
        public SerializedCompilationError(string message, IEnumerable<CompilationError> errors)
        {
            Message = message;
            Errors = errors.ToList();
        }

        public string Message { get; }

        public List<CompilationError> Errors { get; }

        public static SerializedCompilationError FromException(CompilationFailedException ex)
        {
            return new SerializedCompilationError(ex.Message, ex.Errors);
        }
    }
}
