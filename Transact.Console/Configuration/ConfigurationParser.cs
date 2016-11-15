using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Cons.Configuration
{
    public class ConfigurationParser
    {
        public Settings Parse(Settings settings, string[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                switch (args[i].ToLower())
                {
                    case "-d":
                    case "--database-type":
                    {
                        if (i == args.Length - 1)
                        {
                            throw new ArgumentOutOfRangeException(nameof(args), "Command line ended unexpectedly");
                        }
                        switch (args[++i].Trim().ToLower())
                        {
                                case "postgre":
                                case "postgres":
                                case "postgresql":
                                    settings.DatabaseType = DatabaseType.PostgreSql;
                                break;
                                case "memory":
                                    settings.DatabaseType = DatabaseType.Memory;
                                break;
                                default:
                                    throw new ArgumentException();

                        }
                        break;
                    }
                    case "--connection-string":
                    case "-c":
                    {
                        if (i == args.Length - 1)
                        {
                            throw new ArgumentOutOfRangeException(nameof(args), "Command line ended unexpectedly");
                        }
                        settings.ConnectionString = args[++i].Trim();
                        break;
                    }
                    case "--port":
                    case "-p":
                    {
                        if (i == args.Length - 1)
                        {
                            throw new ArgumentOutOfRangeException(nameof(args), "Command line ended unexpectedly");
                        }
                        int portNum;
                        string portString = args[++i].Trim();
                        if (!int.TryParse(portString, NumberStyles.Integer, CultureInfo.InvariantCulture, out portNum))
                        {
                            throw new ArgumentException($"'{portString} is not a recognized port number.");
                        }
                        settings.Listening.HttpPort = portNum;
                        break;
                    }
                    default:
                        throw new ArgumentException($"Unrecognized option {args[i]}",nameof(args));
                }
            }

            return settings;
        }
    }
}
