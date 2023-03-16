using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using WebSocketSharp;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;
namespace AudioandTextConversion
{
    public class XFapi
    {
        private static string apiId = "*";
        private static string apiKey = "*";
        private static string apiSecret = "*";
        private static string filePath = @"*";
        public static void Run()
        {
            Ws_Param wsParam = new Ws_Param(apiId, apiKey, apiSecret,filePath);
            var url= wsParam.create_url();
            //Console.WriteLine(url);
            WebSocket ws = wsParam.GetWebSocket(url);
            ws.Connect();
        }
        
    }
    public class Ws_Param
    {
        // APPID
        public string APPID { get; set; }
        // API Key
        public string APIKey { get; set; }
        // API Secret
        public string APISecret { get; set; }
        // 音频文件路径
        public string AudioFile { get; set; }

        public static string result;

        private WebSocket ws;
         // 第一帧的标识 
        private int STATUS_FIRST_FRAME = 0; 
        //中间帧标识
        private int STATUS_CONTINUE_FRAME = 1;  
        // 最后一帧的标识
        private int STATUS_LAST_FRAME = 2;
        // 公共参数(common)
        public Dictionary<string, object> CommonArgs { get; set; }
        // 业务参数(business)
        public Dictionary<string, object> BusinessArgs { get; set; }

        // 构造函数
        public Ws_Param(string appid, string apikey, string apisecret, string audiofile)
        {
            this.APPID = appid;
            this.APIKey = apikey;
            this.APISecret = apisecret;
            this.AudioFile = audiofile;
            result = "";
            // 公共参数(common)
            this.CommonArgs = new Dictionary<string, object>
            {
                { "app_id", this.APPID }
            };
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

        public string create_url()
        {
            string url = "wss://ws-api.xfyun.cn/v2/iat";
            
            string date = GetTs();
            
            
            string signature_origin = "host: " + "ws-api.xfyun.cn" + "\n";
            signature_origin += "date: " + date + "\n";
            signature_origin += "GET " + "/v2/iat " + "HTTP/1.1";
            
            // 进行hmac-sha256进行加密
            byte[] secretBytes = Encoding.UTF8.GetBytes(APISecret);
            byte[] signatureBytes = new HMACSHA256(secretBytes).ComputeHash(Encoding.UTF8.GetBytes(signature_origin));
            string signatureSha = Convert.ToBase64String(signatureBytes);
            string authorizationOrigin = string.Format("api_key=\"{0}\", algorithm=\"{1}\", headers=\"{2}\", signature=\"{3}\"",
                APIKey, "hmac-sha256", "host date request-line", signatureSha);
            byte[] authorizationBytes = Encoding.UTF8.GetBytes(authorizationOrigin);
            string authorization = Convert.ToBase64String(authorizationBytes);
            
            var urlParams = new Dictionary<string, string>()
            {
                { "authorization", authorization },
                { "date", date },
                { "host", "ws-api.xfyun.cn" }
            };
            // Construct query string with parameters
            var urlBuilder = new StringBuilder(url);
            if (urlBuilder.ToString().Contains("?"))
            {
                urlBuilder.Append("&");
            }
            else
            {
                urlBuilder.Append("?");
            }
            urlBuilder.Append(BuildQueryString(urlParams));
            return urlBuilder.ToString();
        }
        
        public static string GetTs()
        {
            DateTime localDateTime = DateTime.Now;
            DateTime utcDateTime = localDateTime.ToUniversalTime();
            string rfc1123DateTime = utcDateTime.ToString("R");
            Console.WriteLine(rfc1123DateTime);
            return rfc1123DateTime;
        }
        private string BuildQueryString(Dictionary<string, string> parameters)
        {
            var queryBuilder = new StringBuilder();
            foreach (var kvp in parameters)
            {
                queryBuilder.AppendFormat("{0}={1}&", WebUtility.UrlEncode(kvp.Key), WebUtility.UrlEncode(kvp.Value));
            }
            queryBuilder.Remove(queryBuilder.Length - 1, 1); // Remove trailing '&'

            return queryBuilder.ToString();
        }

        public static void OnMessage(object sender, MessageEventArgs e)
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

        public static void OnError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine("### error:{0} ###",e.Message);
        }

        public void OnClose(object sender, CloseEventArgs e)
        {
            Console.WriteLine("### closed ###");
        }
        public void OnOpen(object sender, EventArgs e)
        {
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

        public string GetResult()
        {
            return result;
        }
        
    }
}