using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Threading;

namespace LeobasChat
{
    public class Program
    {
        public static Dictionary<int, TcpClient> clients = new Dictionary<int, TcpClient>();
        public static Dictionary<int, string> userMsgBox = new Dictionary<int, string>();
        public static Thread ReceiveEvent;

        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();

    }
}
