// This file is licensed under the MIT open source license
// https://opensource.org/licenses/MIT

namespace Daemos
{
    using System;

    public struct NewChildTransactionData
    {
        public Guid Id { get; set; }

        public DateTime? Expires { get; set; }

        public object Payload { get; set; }

        public string Script { get; set; }

        public object Error { get; set; }
    }
}
