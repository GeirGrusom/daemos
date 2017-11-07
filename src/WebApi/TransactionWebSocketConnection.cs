// <copyright file="TransactionWebSocketConnection.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.WebApi
{
    using System;
    using System.Collections.Generic;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Daemos.WebApi.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Provides a web socket abstraction
    /// </summary>
    public class TransactionWebSocketConnection
    {
        private readonly List<Guid> subscriptions;
        private readonly SubscriptionService service;
        private readonly WebSocket socket;
        private readonly CancellationToken cancel;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionWebSocketConnection"/> class.
        /// </summary>
        /// <param name="socket">Socket used for Web socket connection</param>
        /// <param name="service">Service abstraction instance</param>
        /// <param name="cancel">Cancellation token used to dispose the websocket</param>
        public TransactionWebSocketConnection(WebSocket socket, SubscriptionService service, CancellationToken cancel)
        {
            this.socket = socket;
            this.service = service;
            this.subscriptions = new List<Guid>();
            this.cancel = cancel;
        }

        /// <summary>
        /// Sends a byte array as UTF-8 text to the client
        /// </summary>
        /// <param name="data">UTF-8 text array</param>
        /// <returns>Task</returns>
        public Task SendText(byte[] data)
        {
            return this.socket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, this.cancel);
        }

        /// <summary>
        /// Message recieved method
        /// </summary>
        /// <param name="message">Message recieved</param>
        /// <param name="type">Type of websocket message</param>
        /// <returns>Task</returns>
        public async Task OnMessageReceived(ArraySegment<byte> message, WebSocketMessageType type)
        {
            string contents = Encoding.UTF8.GetString(message.Array, message.Offset, message.Count);

            var action = JsonConvert.DeserializeObject<WebSocketEvent>(contents);
            try
            {
                if (action == null)
                {
                    return;
                }

                if (action.Type == "subscribe")
                {
                    var newSub = this.service.Subscribe(action.Filter, action.Id, this.Callback);
                    this.subscriptions.Add(newSub);
                    var obj = JsonConvert.SerializeObject(new { type = "subscription", id = newSub });
                    await this.SendText(Encoding.UTF8.GetBytes(obj));
                }

                if (action.Type == "list+subscribe")
                {
                    var newSub = await this.service.ListAndSubscribe(action.Filter, action.Id, this.Callback);
                    this.subscriptions.Add(newSub);
                    var obj = JsonConvert.SerializeObject(new { type = "subscription", id = newSub });
                    await this.SendText(Encoding.UTF8.GetBytes(obj));
                }

                if (action.Type == "unsubscribe")
                {
                    if (action.Id == null)
                    {
                        return;
                    }

                    this.service.Unsubscribe(action.Id);
                    this.subscriptions.Remove(action.Id);
                }
            }
            catch (Exception ex)
            {
                await this.SendText(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { type = "exception", exception = ex })));
            }
            finally
            {
                Array.Clear(message.Array, 0, message.Count);
            }
        }

        /// <summary>
        /// Called when the websocket is closed
        /// </summary>
        /// <param name="closeStatus">Status of connection</param>
        /// <param name="closeStatusDescription">Description of status</param>
        public void OnClose(WebSocketCloseStatus? closeStatus, string closeStatusDescription)
        {
            foreach (var id in this.subscriptions)
            {
                this.service.Unsubscribe(id);
            }
        }

        private void Callback(Guid guid, Transaction transaction)
        {
            var model = transaction.ToTransactionResult();

            var obj = JsonConvert.SerializeObject(new { type = "transaction", id = guid, transaction = model });
            this.SendText(Encoding.UTF8.GetBytes(obj));
        }
    }
}