﻿// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.Console
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Loader;
    using System.Threading;
    using System.Threading.Tasks;
    using Daemos.Console.Configuration;
    using Daemos.Mute;
    using Daemos.Postgres;
    using Daemos.Scripting;
    using Daemos.WebApi;
    using Daemos.WebApi.Scripting;
    using Newtonsoft.Json;

    public class Program
    {
        private static CancellationToken _cancel;

        public static int Main(string[] args)
        {
            return MainApp(args).GetAwaiter().GetResult();
        }

        private static ITransactionStorage CreateStorageFromSettings(Settings settings)
        {
            switch (settings.DatabaseType)
            {
                case DatabaseType.PostgreSql:
                    return new PostgreSqlTransactionStorage(settings.ConnectionString);
                case DatabaseType.Memory:
                    return new MemoryStorage();
                default:
                    throw new NotSupportedException("Storage mode not supported.");
            }
        }

        public static async Task<int> MainApp(string[] args)
        {
            var parser = new ConfigurationParser();
            var dependencyResolver = new DefaultDependencyResolver();

            Settings settings;
            const string SettingsFilename = "settings.json";
            try
            {
                string contents = File.ReadAllText(SettingsFilename);
                settings = JsonConvert.DeserializeObject<Settings>(contents);
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    case FileNotFoundException fex:
                        System.Console.WriteLine($"{SettingsFilename} could not be found.");
                        break;
                    case UnauthorizedAccessException aux:
                        System.Console.WriteLine($"Access to {SettingsFilename} was denied.");
                        break;
                    case JsonException jex:
                        System.Console.WriteLine($"Could not understand {SettingsFilename}: {jex.Message}");
                        break;
                    default:
                        System.Console.WriteLine($"There was an error loading {SettingsFilename}: {ex.Message}");
                        break;
                }

                settings = new Settings
                {
                    DatabaseType = DatabaseType.Memory,
                    Listening = new ListenSettings { HttpPort = 5000, Host = "*", Scheme = Scheme.Http, WebSocketEnabled = true }
                };
            }

            settings = parser.Parse(settings, args);

            if (settings.Install)
            {
                var installer = new Installer();
                await installer.Run(null);
                return -1;
            }

            ITransactionStorage storage = CreateStorageFromSettings(settings);
            try
            {
                System.Console.WriteLine("Initializing a connection to the database provider...");
                await storage.OpenAsync();
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                System.Console.WriteLine(ex.Message);
                return -1;
            }

            var httpServer = new HttpServer(settings.Listening.BuildUri(), storage);

            var location = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var dir = Directory.CreateDirectory(Path.Combine(location, "Modules"));
            var modules = dir.GetFiles("*.dll", SearchOption.TopDirectoryOnly);
            var muteScriptRunner = new MuteScriptRunner();

            muteScriptRunner.AddImplicitType("Console", typeof(IEchoService));
            muteScriptRunner.AddImplicitType("IPaymentService", typeof(IPaymentService));
            dependencyResolver.Register<IEchoService>(new EchoService(), null);
            dependencyResolver.Register<IPaymentService>(new MockPaymentService(), "mock");

            foreach (var mod in modules)
            {
                try
                {
                    using (var fs = mod.OpenRead())
                    {
                        var asm = AssemblyLoadContext.Default.LoadFromStream(fs);

                        var types = asm.ExportedTypes;
                    }
                }
                catch (Exception)
                {
                }
            }

            ScriptingProvider provider = new ScriptingProvider(storage);
            provider.RegisterLanguageProvider("mute", muteScriptRunner);
            dependencyResolver.Register<IScriptRunner>(provider, null);

            var listeningThread = new Thread(async () => await httpServer.Start(provider))
            {
                Name = "Web Server"
            };

            listeningThread.Start();


            TransactionProcessor processor = new TransactionProcessor(storage, provider, dependencyResolver);

            var cancelSource = new CancellationTokenSource();
            _cancel = cancelSource.Token;
            System.Console.CancelKeyPress += async (sender, ev) =>
            {
                await httpServer.Stop();
            };

            while (!cancelSource.IsCancellationRequested)
            {
                await processor.RunAsync(cancelSource.Token);
            }

            httpServer.Wait();

            return 0;
        }
    }
}
