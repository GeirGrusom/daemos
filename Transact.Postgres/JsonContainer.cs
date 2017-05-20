using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transact.Postgres
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
