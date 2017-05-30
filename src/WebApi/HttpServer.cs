using System;
using System.Threading;
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
        private readonly Uri _baseAddress;
        public IServiceProvider Container { get; }
        private readonly SubscriptionService _subscriptionService;
        private IWebHost _host;

        public ITransactionStorage Storage { get; }

        private readonly CancellationTokenSource cancel;

        public HttpServer()
        {
            cancel = new CancellationTokenSource();
        }

        public void Stop()
        {
            cancel.Cancel();
        }

        public void Wait()
        {
            cancel.Token.WaitHandle.WaitOne();
        }

        public HttpServer(Uri baseAddress, ITransactionStorage storage)
            : this()
        {
            _baseAddress = baseAddress;
            Storage = storage;
            _subscriptionService = new SubscriptionService(storage);
            
        }

        public void Start()
        {

            WebHostBuilder hostBuilder = new WebHostBuilder();

            var loggerFactory = new LoggerFactory();

            hostBuilder.UseKestrel();
            hostBuilder.UseLoggerFactory(loggerFactory);
            hostBuilder.ConfigureServices(x => ConfigureServices(loggerFactory, x));
            hostBuilder.Configure(Configuration);
            
            
            _host = hostBuilder.Build();
          
            _host.Run(cancel.Token);
        }

        public void ConfigureServices(ILoggerFactory loggerFactory, IServiceCollection services)
        {
            services.AddCors(x => x.AddPolicy("Default", b => b.AllowAnyOrigin().Build()));
            services.AddSingleton(Storage);
            services.AddSingleton(_subscriptionService);
            services.AddWebEncoders();
            services.AddLogging();
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
