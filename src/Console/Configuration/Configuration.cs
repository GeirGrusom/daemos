using System;

namespace Daemos.Console.Configuration
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
        public int? HttpPort { get; set; }
        public Scheme Scheme { get; set; }
        public string Host { get; set; }
        public string Path { get; set; }

        private int GetDefaultPort()
        {
            if (Scheme == Scheme.Http)
            {
                return 80;
            }
            return 443;
        }

        public Uri BuildUri()
        {
            var path = $"{Scheme.ToString().ToLower()}://{Host}:{HttpPort ?? GetDefaultPort()}/{Path}";
            return new Uri(path, UriKind.Absolute);
        }
    }
}
