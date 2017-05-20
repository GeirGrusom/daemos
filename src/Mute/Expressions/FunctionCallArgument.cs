namespace Daemos.Mute.Expressions
{
    public struct FunctionCallArgument
    {
        public string Name { get; }
        public Expression Value { get; }

        public FunctionCallArgument(string name, Expression value)
        {
            Name = name;
            Value = value;
        }

        public FunctionCallArgument(Expression value)
        {
            Name = null;
            Value = value;
        }

        public override string ToString()
        {
            if (Name != null)
            {
                return $"{Name}: {Value}";
            }
            return Value.ToString();
        }
    }
}
