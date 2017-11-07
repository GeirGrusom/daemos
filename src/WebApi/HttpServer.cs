// <copyright file="HttpServer.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.WebApi
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Daemos.Scripting;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Swashbuckle.AspNetCore.Swagger;

    /// <summary>
    /// The Daemos HTTP web server
    /// </summary>
    public class HttpServer
    {
        private readonly string baseAddress;
        private readonly SubscriptionService subscriptionService;
        private readonly CancellationTokenSource cancel;

        private IWebHost host;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpServer"/> class.
        /// </summary>
        /// <param name="baseAddress">Base listening address of HTTP server</param>
        /// <param name="storage">Storage engine used</param>
        public HttpServer(string baseAddress, ITransactionStorage storage)
            : this()
        {
            this.baseAddress = baseAddress;
            this.Storage = storage;
            this.subscriptionService = new SubscriptionService(storage);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpServer"/> class.
        /// </summary>
        public HttpServer()
        {
            this.cancel = new CancellationTokenSource();
        }

        /// <summary>
        /// Gets the current storage engine
        /// </summary>
        public ITransactionStorage Storage { get; }

        /// <summary>
        /// Stops the web server
        /// </summary>
        /// <returns>Task</returns>
        public Task Stop()
        {
            this.cancel.Cancel();
            return this.host.StopAsync();
        }

        /// <summary>
        /// Waits for the web server to stop
        /// </summary>
        public void Wait()
        {
            this.cancel.Token.WaitHandle.WaitOne();
        }

        /// <summary>
        /// Starts the webserver using the specified scriptrunner
        /// </summary>
        /// <param name="scriptRunner">Script runner used</param>
        /// <returns>Task</returns>
        public Task Start(IScriptRunner scriptRunner)
        {
            WebHostBuilder hostBuilder = new WebHostBuilder();

            var loggerFactory = new LoggerFactory();

            hostBuilder.UseKestrel(x =>
            {
                x.AddServerHeader = false;
            });
            hostBuilder.UseUrls(this.baseAddress);
            hostBuilder.ConfigureServices(x => this.ConfigureServices(loggerFactory, scriptRunner, x));
            hostBuilder.Configure(this.Configuration);

            this.host = hostBuilder.Build();
            return this.host.RunAsync(this.cancel.Token);
        }

        /// <summary>
        /// Configures OWIN services
        /// </summary>
        /// <param name="loggerFactory">Logger factory used</param>
        /// <param name="scriptRunner">Scriptrunner used</param>
        /// <param name="services">Services to initialize</param>
        public void ConfigureServices(ILoggerFactory loggerFactory, IScriptRunner scriptRunner, IServiceCollection services)
        {
            services.AddSingleton(this.Storage);
            services.AddSingleton(this.subscriptionService);
            services.AddWebEncoders();
            services.AddLogging();
            services.AddSingleton(scriptRunner);
            var jsonSerializerSettings = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateParseHandling = DateParseHandling.DateTime,
                FloatFormatHandling = FloatFormatHandling.String,
                Formatting = Formatting.None,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            };

            services.AddMvcCore(x =>
            {
                x.Filters.Add(typeof(ExceptionFilter));
            })
            .AddCors()
            .AddApiExplorer()
            .AddFormatterMappings()
            .AddJsonFormatters();

            services.AddSwaggerGen(x =>
            {
                x.SwaggerDoc("v1", new Info { Title = "Daemos", License = new License { Name = "MIT" }, Version = "v1" });
                x.DescribeAllEnumsAsStrings();
            });
        }

        /// <summary>
        /// Configures the HTTP application
        /// </summary>
        /// <param name="appBuilder">AppBuilder used to configure the HTTP application</param>
        public void Configuration(IApplicationBuilder appBuilder)
        {
            appBuilder.UseCors(c => c.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
            appBuilder.UseWebSockets();
            appBuilder.UseSwagger(o =>
            {
                o.RouteTemplate = "swagger/{documentName}";
            });
            appBuilder.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("v1/swagger.json", "Daemos API");
            });
            appBuilder.UseMvc();

            appBuilder.Use(async (http, next) =>
            {
                if (http.WebSockets.IsWebSocketRequest)
                {
                    var socket = await http.WebSockets.AcceptWebSocketAsync();
                    var conn = new TransactionWebSocketConnection(socket, this.subscriptionService, this.cancel.Token);
                    byte[] arr = new byte[8192];
                    while (socket.State == System.Net.WebSockets.WebSocketState.Open && !this.cancel.IsCancellationRequested)
                    {
                        var buffer = new ArraySegment<byte>(arr);
                        var rec = await socket.ReceiveAsync(buffer, this.cancel.Token);

                        await conn.OnMessageReceived(buffer, rec.MessageType);
                    }
                }
                else
                {
                    await next();
                }
            });
        }
    }
}
