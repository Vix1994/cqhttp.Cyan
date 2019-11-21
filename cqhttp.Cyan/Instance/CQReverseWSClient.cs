using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using cqhttp.Cyan.ApiCall.Requests.Base;
using cqhttp.Cyan.ApiCall.Results.Base;
using cqhttp.Cyan.Events.EventListener;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace cqhttp.Cyan.Instance {
    /// <summary>
    /// 反向websocket连接方式
    /// </summary>
    public class CQReverseWSClient : CQApiClient {

        WebsocketDaemon.WebsocketServerInstance server;
        Dictionary<long, JToken> buffer = new Dictionary<long, JToken> ();
        object bufferLock = new object ();
        /// <summary>
        /// 当cqhttp中use_ws_reverse配置项为true时使用
        /// </summary>
        /// <param name="bind_port">端口</param>
        /// <param name="apiPath">即ws_reverse_api_url</param>
        /// <param name="eventPath">即ws_reverse_event_url</param>
        /// <param name="accessToken"></param>
        /// <param name="use_group_table"></param>
        /// <param name="use_message_table"></param>
        /// <returns></returns>
        public CQReverseWSClient (
            int bind_port,
            string apiPath,
            string eventPath,
            string accessToken = "",
            bool use_group_table = false,
            bool use_message_table = false
        ) : base (accessToken, use_group_table, use_message_table) {
            eventPath = eventPath.Trim ('/');
            apiPath = apiPath.Trim ('/');
            this.__eventListener = new ReverseWSListener (bind_port, eventPath, accessToken);
            server = new WebsocketDaemon.WebsocketServerInstance(bind_port, apiPath);
            (this.__eventListener as ReverseWSListener).api_call_func = SendRequestAsync;
            this.__eventListener.StartListen (this.__HandleEvent);
            Task.Run (() => {
                JToken t;
                if (server.socket.ConnectionInfo.Headers["Authorization"].Contains (accessToken) == false)
                    throw new Exceptions.ErrorApicallException ("身份验证失败");
                server.socket.OnMessage = (m) => {
                    lock (bufferLock) {
                        Logger.Debug ($"[reverse websocket received API response]:\n{m}");
                        t = JToken.Parse (m);
                        buffer[t["echo"].ToObject<long> ()] = t;
                    }
                };
                if (base.Initiate ().Result == false)
                    throw new Exceptions.ErrorApicallException ("初始化失败");
                Logger.Info ($"成功连接");
            });
        }
        ///
        public override async Task<ApiResult> SendRequestAsync (ApiRequest request) {
            JObject constructor = new JObject ();
            long echo = DateTime.Now.ToBinary ();
            constructor["action"] = request.apiPath.Substring (1);
            constructor["params"] = JObject.Parse (request.content);
            constructor["echo"] = echo;
            await server.socket.Send (constructor.ToString (Formatting.None));
            await Config.TimeOut (
                () => buffer.ContainsKey (echo),
                "API调用超时"
            );
            lock (bufferLock) {
                request.response.Parse (buffer[echo]);
                buffer.Remove (echo);
            }
            RequestPreprocess (request);
            return request.response;
        }
    }
}