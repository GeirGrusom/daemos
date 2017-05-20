using System.Collections.Generic;
using System.Reflection.Emit;
using Daemos.Mute.Expressions;

namespace Daemos.Mute.Compilation
{
    public class EntryPointVisitor : Visitor
    {
        private readonly ILGenerator il;
        public List<KeyValuePair<UnaryAwaitExpression, Label>> Awaits;

        public EntryPointVisitor(ILGenerator il)
        {
            Awaits = new List<KeyValuePair<UnaryAwaitExpression, Label>>();
            this.il = il;
        }

        public override void OnVisit(UnaryAwaitExpression exp)
        {
            var label = il.DefineLabel();
            Awaits.Add(new KeyValuePair<UnaryAwaitExpression, Label>(exp, label));
            base.OnVisit(exp);
        }
    }
}
