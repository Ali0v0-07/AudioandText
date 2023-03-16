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
    public class WsParam
    {
        // APPID
        public string APPID { get; set; }
        // API Key
        public string APIKey { get; set; }
        // API Secret
        public string APISecret { get; set; }

        public static string result;

        protected WebSocket ws;
        
         // 第一帧的标识 
        protected int STATUS_FIRST_FRAME = 0; 
        //中间帧标识
        protected int STATUS_CONTINUE_FRAME = 1;  
        // 最后一帧的标识
        protected int STATUS_LAST_FRAME = 2;
        // 公共参数(common)
        public Dictionary<string, object> CommonArgs { get; set; }
        // 业务参数(business)
        public Dictionary<string, object> BusinessArgs { get; set; }

        // 构造函数
        public WsParam(string appid, string apikey, string apisecret)
        {
            this.APPID = appid;
            this.APIKey = apikey;
            this.APISecret = apisecret;
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

        public string create_url(string sign)
        {
            string url = "wss://ws-api.xfyun.cn/v2/"+sign;
            
            string date = GetTs();
            
            
            string signature_origin = "host: " + "ws-api.xfyun.cn" + "\n";
            signature_origin += "date: " + date + "\n";
            signature_origin += "GET " + "/v2/" + sign + " HTTP/1.1";
            
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

        
        public static void OnError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine("### error:{0} ###",e.Message);
        }

        public void OnClose(object sender, CloseEventArgs e)
        {
            Console.WriteLine("### closed ###");
        }
        

        public string GetResult()
        {
            return result;
        }
        
    }
}