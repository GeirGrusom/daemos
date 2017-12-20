// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

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
            return this.json;
        }
    }
}
