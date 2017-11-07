// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    using Antlr4.Runtime;

    /// <summary>
    /// Base class for expressions
    /// </summary>
    public abstract class Expression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Expression"/> class.
        /// </summary>
        /// <param name="resultType">Specifies the result type of this expression</param>
        /// <param name="parserContext">Specifies the parser context this expression was built from</param>
        protected Expression(DataType resultType, ParserRuleContext parserContext)
        {
            this.Type = resultType;
            this.Context = parserContext;
        }

        /// <summary>
        /// Gets a value indicating the result type of this expression
        /// </summary>
        public DataType Type { get; }

        /// <summary>
        /// Gets the parser context this expression was built from
        /// </summary>
        public ParserRuleContext Context { get; }
    }
}
