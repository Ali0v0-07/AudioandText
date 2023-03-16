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
    internal class Program
    {
        public static void Main(string[] args)
        {
            XFapi xFapi = new XFapi();
            xFapi.Run();
            Console.ReadLine();
        }
    }
}