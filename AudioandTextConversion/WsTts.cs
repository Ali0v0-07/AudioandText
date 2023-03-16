using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using WebSocketSharp;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;
namespace AudioandTextConversion
{
    public class WsTts:WsParam
    {
        public string Text { get; set; }
        public string OutputFile { get; set; }
        public Dictionary<string, object> Data { get; set; }
        public WsTts(string appid, string apikey, string apisecret, string text):base(appid,apikey,apisecret)
        {
            Text = text;
            OutputFile = @"D:\C#Code\AudioandTextConversion\AudioandTextConversion\demo.pcm";
            // 业务参数(business)，更多个性化参数可在官网查看
            this.BusinessArgs = new Dictionary<string, object>
            {
                {"aue", "raw"},
                {"auf", "audio/L16;rate=16000"},
                {"vcn", "xiaoyan"},
                {"tte", "utf8"}
            };
            byte[] textBytes = Encoding.UTF8.GetBytes(Text);
            string base64Text = Convert.ToBase64String(textBytes);
            this.Data = new Dictionary<string, object>
            {
                { "status", 2},
                {"text", base64Text}
            };
        }
        public WebSocket GetWebSocket(string wsUrl)
        {
            // 创建 WebSocket 实例
            ws = new WebSocket(wsUrl);
            // 设置回调函数
            ws.OnMessage += OnMessage;
            ws.OnError += OnError;
            ws.OnClose += OnClose;
            ws.OnOpen += OnOpen;
            // 连接 WebSocket
            return ws;
        }
        private void OnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                string message = e.Data;
                string code = JsonConvert.DeserializeObject<dynamic>(message)["code"].ToString();
                string sid = JsonConvert.DeserializeObject<dynamic>(message)["sid"].ToString();
                var status = JsonConvert.DeserializeObject<dynamic>(message)["data"]["status"];
                
                if (status.ToString().Equals("2"))
                {
                    Console.WriteLine("ws is closed");
                    ws.Close();
                }
                if (!code.Equals("0"))
                {
                    string errMsg = JsonConvert.DeserializeObject<dynamic>(message)["message"].ToString();
                    Console.WriteLine("sid:{0} call error:{1} code is:{2}",sid,errMsg,code);
                }
                else
                {
                    
                    string audio = JsonConvert.DeserializeObject<dynamic>(message)["data"]["audio"];
                    byte[] base64Audio = Convert.FromBase64String(audio);
                    using (FileStream stream = new FileStream(OutputFile, FileMode.Append))
                    {
                        stream.Write(base64Audio, 0, base64Audio.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("receive msg,but parse exception: {0}",ex);
            }
        }

        private void OnOpen(object sender, EventArgs e)
        {
            Thread t = new Thread(() =>
            {
                Dictionary<string, object> data = new Dictionary<string, object>()
                {
                    {"common", CommonArgs},
                    {"business", BusinessArgs},
                    {"data", Data}
                };
                string json = JsonConvert.SerializeObject(data);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                ws.Send(bytes);
                string filePath = OutputFile;
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            });
            t.Start();
            
        }
        
    }
}