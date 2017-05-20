using System.Collections.Generic;
using System.Collections.ObjectModel;
using Antlr4.Runtime;

namespace Daemos.Mute.Expressions
{
    public sealed class UsingExpression : Expression
    {
        public string Namespace { get; }
        public ReadOnlyCollection<string> Types { get; }
        public UsingExpression(IList<string> ns, IList<string> typeNames, ParserRuleContext parserContext) : base(DataType.Void, parserContext)
        {
            Namespace = string.Join(".", ns);
            Types = new ReadOnlyCollection<string>(typeNames);
        }
    }
}
