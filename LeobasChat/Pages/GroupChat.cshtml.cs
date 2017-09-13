using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LeobasChat.Pages
{
    public class GroupChatModel : PageModel
    {
        [BindProperty]
        public string SendMessage { get; set; }

        public string ReceivedMessage { get; set; }

        [BindProperty]
        public string SendMsgToHtml { get; set; }

        public string ReceivedToHtml { get; set; }

        public void OnGet()
        {
            SendMsgToHtml = "<div class='answer right'>" +
                                "<div class='avatar'>" +
                                    "<img src = 'https://bootdey.com/img/Content/avatar/avatar2.png' alt='User name'>" +
                                        "<div class='status offline'></div>" +
                                "</div>" +
                                "<div class='name'>Alexander Herthic</div>" +
                                    "<div class='text'>" +
                                        SendMessage +
                                    "</div>" +
                                "<div class='time'>5 min ago</div>" +
                            "</div>";

            ReceivedToHtml = "<div class='answer left'>" +
                            "< div class='avatar'>" +
                                "<img src = 'https://bootdey.com/img/Content/avatar/avatar2.png' alt='User name'>" +
                                    "<div class='status offline'></div>" +
                            "</div>" +
                            "<div class='name'>Alexander Herthic</div>" +
                                "<div class='text'>" +
                                    "Lorem ipsum dolor amet, consectetur adipisicing elit Lorem ipsum dolor amet, consectetur adipisicing elit Lorem ipsum dolor amet, consectetur adiping elit" +
                                "</div>" +
                            "<div class='time'>5 min ago</div>" +
                        "</div>";
        }

        public IActionResult OnPost(string returnUrl = null)
        {
            return LocalRedirect(Url.GetLocalUrl(returnUrl));
        }
    }

    //public class ChatWebSocketMiddleware
    //{
    //    private static ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();

    //    private readonly RequestDelegate _next;

    //    public ChatWebSocketMiddleware(RequestDelegate next)
    //    {
    //        _next = next;
    //    }

    //    public async Task Invoke(HttpContext context)
    //    {
    //        if (!context.WebSockets.IsWebSocketRequest)
    //        {
    //            await _next.Invoke(context);
    //            return;
    //        }

    //        CancellationToken ct = context.RequestAborted;
    //        WebSocket currentSocket = await context.WebSockets.AcceptWebSocketAsync();
    //        var socketId = Guid.NewGuid().ToString();

    //        _sockets.TryAdd(socketId, currentSocket);

    //        while (true)
    //        {
    //            if (ct.IsCancellationRequested)
    //            {
    //                break;
    //            }

    //            var response = await ReceiveStringAsync(currentSocket, ct);
    //            if (string.IsNullOrEmpty(response))
    //            {
    //                if (currentSocket.State != WebSocketState.Open)
    //                {
    //                    break;
    //                }

    //                continue;
    //            }

    //            foreach (var socket in _sockets)
    //            {
    //                if (socket.Value.State != WebSocketState.Open)
    //                {
    //                    continue;
    //                }

    //                await SendStringAsync(socket.Value, response, ct);
    //            }
    //        }

    //        WebSocket dummy;
    //        _sockets.TryRemove(socketId, out dummy);

    //        await currentSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct);
    //        currentSocket.Dispose();
    //    }

    //    private static Task SendStringAsync(WebSocket socket, string data, CancellationToken ct = default(CancellationToken))
    //    {
    //        var buffer = Encoding.UTF8.GetBytes(data);
    //        var segment = new ArraySegment<byte>(buffer);
    //        return socket.SendAsync(segment, WebSocketMessageType.Text, true, ct);
    //    }

    //    private static async Task<string> ReceiveStringAsync(WebSocket socket, CancellationToken ct = default(CancellationToken))
    //    {
    //        var buffer = new ArraySegment<byte>(new byte[8192]);
    //        using (var ms = new MemoryStream())
    //        {
    //            WebSocketReceiveResult result;
    //            do
    //            {
    //                ct.ThrowIfCancellationRequested();

    //                result = await socket.ReceiveAsync(buffer, ct);
    //                ms.Write(buffer.Array, buffer.Offset, result.Count);
    //            }
    //            while (!result.EndOfMessage);

    //            ms.Seek(0, SeekOrigin.Begin);
    //            if (result.MessageType != WebSocketMessageType.Text)
    //            {
    //                return null;
    //            }

    //            // Encoding UTF8: https://tools.ietf.org/html/rfc6455#section-5.6
    //            using (var reader = new StreamReader(ms, Encoding.UTF8))
    //            {
    //                return await reader.ReadToEndAsync();
    //            }
    //        }
    //    }
    //}
}