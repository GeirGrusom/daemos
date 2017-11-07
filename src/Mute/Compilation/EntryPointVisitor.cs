// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Compilation
{
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using Daemos.Mute.Expressions;

    /// <summary>
    /// Visitor that finds every await statement in the code in order to produce a label for that await's continuation
    /// </summary>
    public class EntryPointVisitor : Visitor
    {
        private readonly ILGenerator il;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntryPointVisitor"/> class.
        /// </summary>
        /// <param name="il">The CIL generator to produce labels with</param>
        public EntryPointVisitor(ILGenerator il)
        {
            this.Awaits = new List<KeyValuePair<UnaryAwaitExpression, Label>>();
            this.il = il;
        }

        /// <summary>
        /// Gets all the await continuation labels produced by the visitor
        /// </summary>
        public List<KeyValuePair<UnaryAwaitExpression, Label>> Awaits { get; }

        /// <summary>
        /// Visits the await expression and produces a label
        /// </summary>
        /// <param name="exp">Expression to visit</param>
        public override void OnVisit(UnaryAwaitExpression exp)
        {
            var label = this.il.DefineLabel();
            this.Awaits.Add(new KeyValuePair<UnaryAwaitExpression, Label>(exp, label));
            base.OnVisit(exp);
        }
    }
}
