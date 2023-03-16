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
    public class WsIat:WsParam
    {
        public string AudioFile { get; set; }
        
        public WsIat(string appid, string apikey, string apisecret, string audiofile):base(appid,apikey,apisecret)
        {
            AudioFile = audiofile;
            // 业务参数(business)，更多个性化参数可在官网查看
            this.BusinessArgs = new Dictionary<string, object>
            {
                { "domain", "iat" },
                { "language", "zh_cn" },
                { "accent", "mandarin" },
                { "vinfo", 1 },
                { "vad_eos", 10000 }
            };
        } 
        private void OnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                string message = e.Data;
                string code = JsonConvert.DeserializeObject<dynamic>(message)["code"].ToString();
                string sid = JsonConvert.DeserializeObject<dynamic>(message)["sid"].ToString();

                if (code != "0")
                {
                    string errMsg = JsonConvert.DeserializeObject<dynamic>(message)["message"].ToString();
                    Console.WriteLine("sid:{0} call error:{1} code is:{2}",sid,errMsg,code);
                }
                else
                {
                    var data = JsonConvert.DeserializeObject<dynamic>(message)["data"]["result"]["ws"];
                    var status2 = JsonConvert.DeserializeObject<dynamic>(message)["data"]["status"];
                    //不要最后一帧的数据
                    if (!status2.ToString().Equals("2"))
                    {
                        foreach (var i in data)
                        {
                            foreach (var w in i["cw"])
                            {
                                result += w["w"].ToString();
                            }
                        }
                        //Console.WriteLine("sid:{0} call success!,data is:{1}",sid,JsonConvert.SerializeObject(data, Formatting.None));
                        Console.WriteLine(result);
                    }
                        
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("receive msg,but parse exception: {0}",ex);
            }
        }
        private void OnOpen(object sender, EventArgs e){
            Thread t = new Thread(() =>
            {
                int frameSize = 8000; // 每一帧的音频大小
                double intervel = 0.04; // 发送音频间隔(单位:s)
                int status = STATUS_FIRST_FRAME; // 音频的状态信息，标识音频是第一帧，还是中间帧、最后一帧

                using (FileStream fp = new FileStream(AudioFile, FileMode.Open, FileAccess.Read))
                {
                    while (true)
                    {
                        byte[] buf = new byte[frameSize];
                        int len = fp.Read(buf, 0, buf.Length);
                        // 文件结束
                        if (len == 0)
                        {
                            status = STATUS_LAST_FRAME;
                        }
                        // 第一帧处理
                        // 发送第一帧音频，带business 参数
                        // appid 必须带上，只需第一帧发送
                        if (status == STATUS_FIRST_FRAME)
                        {
                            Dictionary<string, object> data = new Dictionary<string, object>()
                            {
                                {"common", CommonArgs},
                                {"business", BusinessArgs},
                                {"data", new Dictionary<string, object>()
                                {
                                    {"status", 0},
                                    {"format", "audio/L16;rate=16000"},
                                    {"audio", Convert.ToBase64String(buf)},
                                    {"encoding", "raw"}
                                }}
                            };
                            string json = JsonConvert.SerializeObject(data);
                            byte[] bytes = Encoding.UTF8.GetBytes(json);
                            ws.Send(bytes);
                            status = STATUS_CONTINUE_FRAME;
                        }
                        // 中间帧处理
                        else if (status == STATUS_CONTINUE_FRAME)
                        {
                            Dictionary<string, object> data = new Dictionary<string, object>()
                            {
                                {"data", new Dictionary<string, object>()
                                {
                                    {"status", 1},
                                    {"format", "audio/L16;rate=16000"},
                                    {"audio", Convert.ToBase64String(buf)},
                                    {"encoding", "raw"}
                                }}
                            };
                            string json = JsonConvert.SerializeObject(data);
                            byte[] bytes = Encoding.UTF8.GetBytes(json);
                            ws.Send(bytes);
                        }
                        // 最后一帧处理
                        else if (status == STATUS_LAST_FRAME)
                        {
                            Dictionary<string, object> data = new Dictionary<string, object>()
                            {
                                {"data", new Dictionary<string, object>()
                                {
                                    {"status", 2},
                                    {"format", "audio/L16;rate=16000"},
                                    {"audio", Convert.ToBase64String(buf)},
                                    {"encoding", "raw"}
                                }}
                            };
                            string json = JsonConvert.SerializeObject(data);
                            byte[] bytes = Encoding.UTF8.GetBytes(json);
                            ws.Send(bytes);
                            Thread.Sleep(1000);
                            break;
                        }
                        // 模拟音频采样间隔
                        Thread.Sleep((int)(intervel * 1000));
                    }
                    ws.Close();
                }
            });
            t.Start();
            
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
        
    }
}