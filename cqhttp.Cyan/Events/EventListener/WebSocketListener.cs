using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace cqhttp.Cyan.Events.EventListener {
    /// <summary></summary>
    public class WebSocketListener : CQEventListener {
        private ClientWebSocket client;
        private string dest_url;
        /// <summary></summary>
        public WebSocketListener (string dest_url, string secret) : base (secret) {
            this.dest_url = dest_url;
        }
        /// <summary></summary>
        public override void StartListen (System.Func<CQEvents.Base.CQEvent, CQEvents.CQResponses.Base.CQResponse> callback) {
            listen_callback = callback;
            lock (listen_lock) {
                client.ConnectAsync (
                    new System.Uri (dest_url), new System.Threading.CancellationToken ()
                );
                listen_task = System.Threading.Tasks.Task.Run (() => {
                    byte[] recv_buffer = new byte[2048];
                    List<byte> message = new List<byte> ();
                    while (true) {
                        while (!client.ReceiveAsync (
                                recv_buffer,
                                new System.Threading.CancellationToken ()
                            ).Result.EndOfMessage) {
                            message.AddRange (recv_buffer);
                        }
                        message.AddRange (recv_buffer);
                        Process (message.ToArray ());
                    }
                });
            }
        }
        private async void Process (byte[] message) {
            try {
                await Task.Run (() =>
                    listen_callback (CQEventHandler.HandleEvent (Encoding.UTF8.GetString (message)))
                );//Websocket下不会处理响应！！！！！
            } catch (System.Exception e) {
                System.Console.WriteLine (e.ToString ());
            }
        }
    }
}