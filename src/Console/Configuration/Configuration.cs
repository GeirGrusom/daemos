// <copyright file="Configuration.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.Console.Configuration
{
    using System;

    public class Settings
    {
        public string ConnectionString { get; set; }

        public DatabaseType DatabaseType { get; set; }

        public ListenSettings Listening { get; set; }

        public bool Install { get; set; }
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
            if (this.Scheme == Scheme.Http)
            {
                return 80;
            }
            return 443;
        }

        public string BuildUri()
        {
            var path = $"{this.Scheme.ToString().ToLower()}://{this.Host}:{this.HttpPort ?? this.GetDefaultPort()}/{this.Path}";
            return path;
        }
    }
}
