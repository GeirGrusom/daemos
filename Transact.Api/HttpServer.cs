using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.Http.Tracing;
using Microsoft.Owin.Hosting;
using Microsoft.Practices.Unity;
using Owin;
using Transact.Api.Controllers;
using Newtonsoft.Json;
using Owin.WebSocket.Extensions;


namespace Transact.Api
{
    public class ControllerTypeResolver : IHttpControllerTypeResolver
    {
        public ICollection<Type> GetControllerTypes(IAssembliesResolver assembliesResolver)
        {
            return new List<Type> {typeof (TransactionController), typeof(TransactionRootController), typeof(ErrorController)};
        }
    }

    public class TraceManager : ITraceManager
    {
        public void Initialize(HttpConfiguration configuration)
        {
            Console.WriteLine("Trace config");
        }
    }

    public class TraceWriter : ITraceWriter
    {
        public void Trace(HttpRequestMessage request, string category, TraceLevel level, Action<TraceRecord> traceAction)
        {
            Console.WriteLine(request.RequestUri.ToString());
        }
    }

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
        private readonly ITransactionStorage _storage;
        public  IUnityContainer Container { get; }
        private readonly SubscriptionService _subscriptionService;

        public HttpServer(Uri baseAddress, ITransactionStorage storage, IUnityContainer container)
        {
            Container = container;
            _baseAddress = baseAddress;
            _storage = storage;
            _subscriptionService = new SubscriptionService(_storage);
            
        }

        public void Start()
        {
            var startupOptions = new StartOptions(_baseAddress.ToString());
            _server = WebApp.Start(startupOptions, Configuration);
        }

        public void Configuration(IAppBuilder appBuilder)
        {
            var config = new HttpConfiguration();

            var resolver = new UnityResolver(Container);
            var dep = new DefaultHttpControllerSelector(config);

            Container.RegisterInstance(_storage);
            Container.RegisterInstance(_subscriptionService);
            Container.RegisterType<ITraceManager, TraceManager>();
            Container.RegisterType<ITraceWriter, TraceWriter>();
            config.DependencyResolver = resolver;

            appBuilder.MapWebSocketRoute<TransactionWebSocketConnection>("/api/transaction/filter", new UnityServiceLocator(Container));
            config.Routes.MapHttpRoute("TransactionQuery", "api/transaction", new { controller = "TransactionRoot" });
            config.Routes.MapHttpRoute("SpecificTransaction", "api/transaction/{id}", new { controller = "Transaction" });
            config.Routes.MapHttpRoute("TransactionChain", "api/transaction/{id}/chain/{revision}", new { controller = "TransactionChain", revision = RouteParameter.Optional });
            config.Routes.MapHttpRoute("TransactionHandling", "api/transaction/{id}/{action}", new {controller = "TransactionHandling"});
            
            config.Routes.MapHttpRoute("any", "{*wildcard}", new {Controller = "Error", Action = "Get"});

            config.EnsureInitialized();

            appBuilder.UseWebApi(config);
            

        }



        public void Stop()
        {
            _subscriptionService.Dispose();
            _server.Dispose();
        }
    }
    
}
