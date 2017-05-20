﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Markurion.Scripting;

namespace Markurion.Mute.Interperator
{
    public sealed class Compiler
    {
        public NamespaceLookup NamespaceLookup{ get; }

        public Dictionary<string, Type> ImplicitImports { get; }

        public Compiler()
        {
            ImplicitImports = new Dictionary<string, Type>
            {
                ["timespan"] = typeof(TimeSpan),
            };
            NamespaceLookup = new NamespaceLookup();
        }

        private sealed class TokenErrorListener : IAntlrErrorListener<IToken>
        {
            public List<CompilationMessage> Messages { get; set; }
            public void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg,
                RecognitionException e)
            {
                Messages.Add(new CompilationMessage(msg, line, charPositionInLine, MessageSeverity.Error));
            }
        }
       
        public CompilationResult Compile(string code)
        {
            
            var lexer = new MuteGrammarLexer(new AntlrInputStream(code));
            var parser = new MuteGrammarParser(new BufferedTokenStream(lexer));
            
            var errorListener = new TokenErrorListener { Messages = parser.Messages };
                
            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);
            parser.NamespaceLookup = NamespaceLookup;
            parser.UsingTypes = ImplicitImports;
            //parser.ErrorHandler = new BailErrorStrategy();
            
            var ctx = parser.compileUnit();
            
            

            if (parser.Messages.Count(x => x.Severity == MessageSeverity.Error) > 0)
            {
                return new CompilationResult(null, parser.Messages);
            }

            var cmp = new ExpressionCompiler();
            var result = cmp.Compile(ctx.module);

            return new CompilationResult(result, parser.Messages);
        }
    }
}
