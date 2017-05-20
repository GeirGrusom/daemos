﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection.Metadata;
using Transact.Api.Controllers;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.WebSockets.Server;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace Transact.Api
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
        private IDisposable _server;
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

            hostBuilder.UseKestrel();
            hostBuilder.ConfigureServices(ConfigureServices);
            hostBuilder.Configure(Configuration);
            
            
            _host = hostBuilder.Build();
          
            _host.Run(cancel.Token);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ITransactionStorage>(Storage);
            services.AddSingleton<SubscriptionService>(_subscriptionService);
            services.AddMvc(x => x.Filters.Add(typeof(ExceptionFilter)));
            //services.AddRouting();
        }

        public void Configuration(IApplicationBuilder appBuilder)
        {
            appBuilder.UseWebSockets();
            appBuilder.UseMvc();
            

            /*appBuilder.Use(async (http, next) => {
                if(http.WebSockets.IsWebSocketRequest)
                {

                }
                else
                {
                    await next();
                }
            });*/
        }
    }
    
}
