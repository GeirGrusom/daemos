using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Owin.WebSocket;
using Transact.Api.Models;

namespace Transact.Api
{
    public class TransactionWebSocketConnection : WebSocketConnection
    {
        private readonly List<Guid> _subscriptions;
        private readonly SubscriptionService _service;

        public TransactionWebSocketConnection(SubscriptionService service)
        {
            _service = service;
            _subscriptions = new List<Guid>();
        }

        public override async Task OnMessageReceived(ArraySegment<byte> message, WebSocketMessageType type)
        {
            string contents = Encoding.UTF8.GetString(message.Array);

            
            var action = JsonConvert.DeserializeObject<WebSocketEvent>(contents);
            if (action.Action == "subscribe")
            {
                var newSub = _service.Subscribe(action.Filter, Callback);
                _subscriptions.Add(newSub);
                var obj = JsonConvert.SerializeObject(new { type = "subscription", id = newSub});
                await SendText(Encoding.UTF8.GetBytes(obj), true);
            }
            if (action.Action == "list+subscribe")
            {
                var newSub = await _service.ListAndSubscribe(action.Id.Value, Callback);
                _subscriptions.Add(newSub);
                var obj = JsonConvert.SerializeObject(new { type = "subscription", id = newSub });
                await SendText(Encoding.UTF8.GetBytes(obj), true);
            }
            if (action.Action == "unsubscribe")
            {
                if (action.Id == null)
                    return;
                _service.Unsubscribe(action.Id.Value);
                _subscriptions.Remove(action.Id.Value);
            }
        }

        private void Callback(Guid guid, Transaction transaction)
        {
            var model = TransactionMapper.Map(transaction);

            var obj = JsonConvert.SerializeObject(new {type = "transaction", transaction = model});
            SendText(Encoding.UTF8.GetBytes(obj), true);
        }

        public override void OnClose(WebSocketCloseStatus? closeStatus, string closeStatusDescription)
        {
            foreach (var id in _subscriptions)
            {
                _service.Unsubscribe(id);
            }
        }
    }
}