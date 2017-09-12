﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Daemos.Scripting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Formatters.Json.Internal;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Swagger;

namespace Daemos.WebApi
{
   
    public class WebSocketEvent
    {
        [JsonProperty("action")]
        public string Action { get; set; }
        [JsonProperty("id")]
        public Guid? Id { get; set; }

        [JsonProperty("filter")]
        public string Filter { get; set; }
    }

    public class HttpServer
    {
        private readonly string _baseAddress;
        private readonly SubscriptionService _subscriptionService;
        private IWebHost _host;

        public ITransactionStorage Storage { get; }

        private readonly CancellationTokenSource cancel;

        public HttpServer()
        {
            cancel = new CancellationTokenSource();
        }

        public Task Stop()
        {
            cancel.Cancel();
            return _host.StopAsync();
        }

        public void Wait()
        {
            cancel.Token.WaitHandle.WaitOne();
        }

        public HttpServer(string baseAddress, ITransactionStorage storage)
            : this()
        {
            _baseAddress = baseAddress;
            Storage = storage;
            _subscriptionService = new SubscriptionService(storage);
            
        }

        public Task Start(IScriptRunner scriptRunner)
        {

            WebHostBuilder hostBuilder = new WebHostBuilder();

            var loggerFactory = new LoggerFactory();

            hostBuilder.UseKestrel(x =>
            {
                x.AddServerHeader = false;
            });
            hostBuilder.UseUrls(_baseAddress);
            hostBuilder.ConfigureServices(x => ConfigureServices(loggerFactory, scriptRunner, x));
            hostBuilder.Configure(Configuration);
            
            _host = hostBuilder.Build();
            return _host.RunAsync(cancel.Token);
        }

        public void ConfigureServices(ILoggerFactory loggerFactory, IScriptRunner scriptRunner, IServiceCollection services)
        {
            services.AddCors(x => x.AddPolicy("Default", b => b.AllowAnyOrigin().Build()));
            services.AddSingleton(Storage);
            services.AddSingleton(_subscriptionService);
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
            .AddApiExplorer()
            .AddFormatterMappings()
            .AddJsonFormatters();

            services.AddSwaggerGen(x => x.SwaggerDoc("v1",
                new Info {Title = "Daemos", License = new License {Name = "MIT"}, Version = "v1"}));
        }

        public void Configuration(IApplicationBuilder appBuilder)
        {
            appBuilder.UseWebSockets();
            appBuilder.UseMvc();
            appBuilder.UseCors("Default");
            appBuilder.UseSwagger();
            appBuilder.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Daemos API");
            });

            appBuilder.Use(async (http, next) => {
                if(http.WebSockets.IsWebSocketRequest)
                {
                    var socket = await http.WebSockets.AcceptWebSocketAsync();
                    var conn = new TransactionWebSocketConnection(socket, _subscriptionService, cancel.Token);
                    byte[] arr = new byte[8192];
                    while (socket.State == System.Net.WebSockets.WebSocketState.Open && !cancel.IsCancellationRequested)
                    {
                        var buffer = new ArraySegment<byte>(arr);
                        var rec = await socket.ReceiveAsync(buffer, cancel.Token);

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
