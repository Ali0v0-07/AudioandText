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
    public class XFapi
    {
        private static string apiId = "*";
        private static string apiKey = "*";
        private static string apiSecret = "*";
        private static string filePath = @"D:\C#Code\ConsoleApplication1\xfyunAPI\test.pcm";
        public void Run()
        {
            Init();
            UseTts();
        }

        private void UseIat()
        {
            WsIat wsIat = new WsIat(apiId, apiKey, apiSecret, filePath);
            var url= wsIat.create_url("iat");
            //Console.WriteLine(url);
            WebSocket ws = wsIat.GetWebSocket(url);
            ws.Connect();
        }

        private void UseTts()
        {
            WsTts wsTts = new WsTts(apiId, apiKey, apiSecret, "今天是2023年3月17日");
            var url= wsTts.create_url("tts");
            //Console.WriteLine(url);
            WebSocket ws = wsTts.GetWebSocket(url);
            ws.Connect();
        }

        private void Init()
        {
            try
            {
                string apiPath = @"D:\C#Code\apiinfo";
                string[] lines = File.ReadAllLines(apiPath);
                if (lines.Length >= 1)
                {
                    apiId = lines[0];
                }
                if (lines.Length >= 2)
                {
                    apiKey = lines[1];
                }
                if (lines.Length >= 3)
                {
                    apiSecret = lines[2];
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading API credentials from file: {0}",ex.Message);
            }
        }
        
    }
    
}