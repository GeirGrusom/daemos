﻿using Antlr4.Runtime;

namespace Daemos.Mute.Expressions
{
    public abstract class Expression
    {
        public DataType Type { get; }

        public ParserRuleContext Context { get; }

        protected Expression(DataType resultType, ParserRuleContext parserContext)
        {
            Type = resultType;
            Context = parserContext;
        }
    }
}