using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Markurion.Api.Models;
using Newtonsoft.Json;

namespace Markurion.Api
{
    public class TransactionWebSocketConnection
    {
        private readonly List<Guid> _subscriptions;
        private readonly SubscriptionService _service;
        private readonly WebSocket _socket;
        private readonly CancellationToken _cancel;

        public TransactionWebSocketConnection(WebSocket socket, SubscriptionService service, CancellationToken cancel)
        {
            _socket = socket;
            _service = service;
            _subscriptions = new List<Guid>();
            _cancel = cancel;
        }

        public Task SendText(byte[] data)
        {
            return _socket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, _cancel);
        }

        public async Task OnMessageReceived(ArraySegment<byte> message, WebSocketMessageType type)
        {
            string contents = Encoding.UTF8.GetString(message.Array);

            
            var action = JsonConvert.DeserializeObject<WebSocketEvent>(contents);
            try
            {
                if (action.Action == "subscribe")
                {
                    var newSub = _service.Subscribe(action.Filter, Callback);
                    _subscriptions.Add(newSub);
                    var obj = JsonConvert.SerializeObject(new { type = "subscription", id = newSub });
                    await SendText(Encoding.UTF8.GetBytes(obj));
                }
                if (action.Action == "list+subscribe")
                {
                    var newSub = await _service.ListAndSubscribe(action.Id.Value, Callback);
                    _subscriptions.Add(newSub);
                    var obj = JsonConvert.SerializeObject(new { type = "subscription", id = newSub });
                    await SendText(Encoding.UTF8.GetBytes(obj));
                }
                if (action.Action == "unsubscribe")
                {
                    if (action.Id == null)
                        return;
                    _service.Unsubscribe(action.Id.Value);
                    _subscriptions.Remove(action.Id.Value);
                }
            }
            catch(Exception ex)
            {
                await SendText(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { type = "exception", exception = ex })));
            }
        }

        private void Callback(Guid guid, Transaction transaction)
        {
            var model = transaction.ToTransactionResult();

            var obj = JsonConvert.SerializeObject(new {type = "transaction", transaction = model});
            SendText(Encoding.UTF8.GetBytes(obj));
        }

        public void OnClose(WebSocketCloseStatus? closeStatus, string closeStatusDescription)
        {
            foreach (var id in _subscriptions)
            {
                _service.Unsubscribe(id);
            }
        }
    }
}