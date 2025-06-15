using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Threading;
using ExposureUnnoticed2.Object3D.Player.Scripts;
using ExposureUnnoticed2.Object3D.AdultGoods;

namespace SFMToyWebsocket
{
    internal class WebsocketBehaviour : MonoBehaviour
    {
        private HttpListener httpListener;
        private List<WebSocket> clients = new List<WebSocket>();
        private CancellationTokenSource cts = new CancellationTokenSource();

        private float timer = 0f;
        private const float BroadcastInterval = 0.1f;

        // Modes: 0= Off, 1 = Low, 2 = High
        public static int vibeMode = 0;
        // Piston modes: 0 = Off, 1 = Low, 2 = Medium, 3 = High
        public static int pistonMode = 0;

        private void Start()
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://localhost:11451/ws/");
            httpListener.Start();

            Plugin.Instance.Log.LogInfo("WebSocket server started on ws://localhost:11451/ws/");
            ListenLoop();
        }

        private async void ListenLoop()
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    HttpListenerContext context = await httpListener.GetContextAsync();

                    if (context.Request.IsWebSocketRequest)
                    {
                        HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
                        WebSocket socket = wsContext.WebSocket;
                        lock (clients) clients.Add(socket);
                        _ = HandleClient(socket);
                        Plugin.Instance.Log.LogInfo("Client connected.");
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 995)
            {
                Plugin.Instance.Log.LogInfo("HttpListener shut down cleanly.");
            }
            catch (ObjectDisposedException)
            {
                Plugin.Instance.Log.LogInfo("HttpListener disposed during shutdown.");
            }
            catch (Exception ex)
            {
                Plugin.Instance.Log.LogError($"Unexpected error in ListenLoop: {ex}");
            }
        }


        private async Task HandleClient(WebSocket socket)
        {
            var buffer = new byte[1024];
            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                        break;
                }
            }
            catch { }
            finally
            {
                lock (clients) clients.Remove(socket);
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                socket.Dispose();
                Plugin.Instance.Log.LogInfo("Client disconnected.");
            }
        }

        private void Update()
        {
            SyncToyStatus();
            timer += Time.deltaTime;
            if (timer >= BroadcastInterval)
            {
                timer = 0f;
                Task.Run(BroadcastToyStatus);
            }
        }

        private void SyncToyStatus()
        {
            pistonMode = PlayerController.Instance?.pca.PistonMachineController.CurrentSpeedType ?? 0;
            vibeMode = (int)CommonVibratorController.VibrationStrength;
        }

        private async void BroadcastToyStatus()
        {
            string json = $"{{\"vibe\":{vibeMode},\"piston\":{pistonMode}}}";
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            lock (clients)
            {
                foreach (var client in clients.ToArray())
                {
                    if (client.State == WebSocketState.Open)
                    {
                        try
                        {
                            client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token);
                        }
                        catch
                        {
                            clients.Remove(client);
                        }
                    }
                }
            }
        }

        private void OnDestroy()
        {
            cts.Cancel();
            httpListener?.Close();
            foreach (var socket in clients)
            {
                try { socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closing", CancellationToken.None).Wait(); } catch { }
            }
            Plugin.Instance.Log.LogInfo("WebSocket server shut down.");
        }
    }
}
