// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos.Mute.Expressions
{
    public struct FunctionCallArgument
    {
        public string Name { get; }

        public Expression Value { get; }

        public FunctionCallArgument(string name, Expression value)
        {
            this.Name = name;
            this.Value = value;
        }

        public FunctionCallArgument(Expression value)
        {
            this.Name = null;
            this.Value = value;
        }

        public override string ToString()
        {
            if (this.Name != null)
            {
                return $"{this.Name}: {this.Value}";
            }
            return this.Value.ToString();
        }
    }
}
