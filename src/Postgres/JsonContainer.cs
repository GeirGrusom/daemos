namespace Daemos.Postgres
{
    public sealed class JsonContainer
    {
        private readonly string json;

        public JsonContainer(string json)
        {
            this.json = json;
        }

        public override string ToString()
        {
            return json;
        }
    }
}
