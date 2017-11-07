// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Compilation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Antlr4.Runtime;
    using Daemos.Scripting;

    /// <summary>
    /// This class supports compilation of MuteScript code
    /// </summary>
    public sealed class Compiler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Compiler"/> class.
        /// </summary>
        public Compiler()
        {
            this.ImplicitImports = new Dictionary<string, Type>
            {
                ["timespan"] = typeof(TimeSpan),
            };
            this.NamespaceLookup = new NamespaceLookup();
        }

        /// <summary>
        /// Gets the <see cref="NamespaceLookup"/> used to lookup types from
        /// </summary>
        public NamespaceLookup NamespaceLookup { get; }

        /// <summary>
        /// Gets a list of implicit type aliases
        /// </summary>
        public Dictionary<string, Type> ImplicitImports { get; }

        /// <summary>
        /// Compiles the specified code
        /// </summary>
        /// <param name="code">MuteScript code to compile</param>
        /// <returns>Returns a <see cref="CompilationResult" /></returns>
        public CompilationResult Compile(string code)
        {
            var lexer = new MuteGrammarLexer(new AntlrInputStream(code));
            var parser = new MuteGrammarParser(new BufferedTokenStream(lexer));

            var errorListener = new TokenErrorListener { Messages = parser.Messages };

            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);
            parser.NamespaceLookup = this.NamespaceLookup;
            parser.UsingTypes = this.ImplicitImports;

            var ctx = parser.compileUnit();

            if (parser.Messages.Count(x => x.Severity == MessageSeverity.Error) > 0)
            {
                return new CompilationResult(null, parser.Messages);
            }

            var cmp = new ExpressionCompiler();
            var result = cmp.Compile(ctx.module);

            return new CompilationResult(result, parser.Messages);
        }

        private sealed class TokenErrorListener : IAntlrErrorListener<IToken>
        {
            public List<CompilationMessage> Messages { get; set; }

            public void SyntaxError(
                IRecognizer recognizer,
                IToken offendingSymbol,
                int line,
                int charPositionInLine,
                string msg,
                RecognitionException e)
            {
                this.Messages.Add(new CompilationMessage(msg, line, charPositionInLine, MessageSeverity.Error));
            }
        }
    }
}
