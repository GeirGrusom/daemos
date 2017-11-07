// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Antlr4.Runtime;

    public sealed class UsingExpression : Expression
    {
        public string Namespace { get; }

        public ReadOnlyCollection<string> Types { get; }

        public UsingExpression(IList<string> ns, IList<string> typeNames, ParserRuleContext parserContext) : base(DataType.Void, parserContext)
        {
            this.Namespace = string.Join(".", ns);
            this.Types = new ReadOnlyCollection<string>(typeNames);
        }
    }
}
