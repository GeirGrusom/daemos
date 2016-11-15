using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cons.Configuration;

namespace Cons
{
    public class Settings
    {
        public string ConnectionString { get; set; }
        public DatabaseType DatabaseType { get; set; }
        public ListenSettings Listening { get; set; }   
    }

    public class ListenSettings
    {
        public bool WebSocketEnabled { get; set; }
        public int HttpPort { get; set; }
        public Scheme Scheme { get; set; }
    }
}
