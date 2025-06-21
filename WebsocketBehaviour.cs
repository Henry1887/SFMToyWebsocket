using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ExposureUnnoticed2.Object3D.Player.Scripts;
using ExposureUnnoticed2.Object3D.AdultGoods;
using WebSocketSharp.Server;
using WebSocketSharp;
using System.Threading;

namespace SFMToyWebsocket
{
    internal class WebsocketBehaviour : MonoBehaviour
    {
        private WebSocketServer wssv;
        private const float BroadcastInterval = 0.1f;
        private CancellationTokenSource cancellationTokenSource;

        private void Start()
        {
            try
            {
                wssv = new WebSocketServer(11451);
                wssv.AddWebSocketService<StatusService>("/ws");
                wssv.Start();
                cancellationTokenSource = new CancellationTokenSource();
                Task.Run(() => SyncToyWorker(cancellationTokenSource.Token), cancellationTokenSource.Token);
                Plugin.Instance.Log.LogInfo("WebSocketSharp server started on ws://localhost:11451/ws/");
            }
            catch (Exception ex)
            {
                Plugin.Instance.Log.LogError($"WebSocket server failed to start: {ex}");
            }
        }

        private void SyncToyWorker(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Vibe Modes: 0= Off, 1 = Low, 2 = High
                int vibeMode = (int)CommonVibratorController.VibrationStrength;
                // Piston Modes: 0 = Off, 1 = Low, 2 = Medium, 3 = High
                int pistonMode = PlayerController.Instance?.pca.PistonMachineController.CurrentSpeedType ?? 0;

                BroadcastToyStatus(vibeMode, pistonMode);
                Task.Delay(TimeSpan.FromSeconds(BroadcastInterval), cancellationToken).Wait(cancellationToken);
            }
        }

        private void BroadcastToyStatus(int vibeMode, int pistonMode)
        {
            string json = $"{{\"vibe\":{vibeMode},\"piston\":{pistonMode}}}";
            StatusService.Broadcast(json);
        }

        private void OnDestroy()
        {
            if (wssv != null)
            {
                wssv.Stop();
                cancellationTokenSource?.Cancel();
                Plugin.Instance.Log.LogInfo("WebSocket server shut down.");
            }
        }

        private class StatusService : WebSocketBehavior
        {
            private static readonly List<StatusService> clients = new();

            protected override void OnOpen()
            {
                lock (clients) clients.Add(this);
                Plugin.Instance.Log.LogInfo("WebSocket client connected.");
            }

            protected override void OnClose(CloseEventArgs e)
            {
                lock (clients) clients.Remove(this);
                Plugin.Instance.Log.LogInfo("WebSocket client disconnected.");
            }

            public static void Broadcast(string message)
            {
                lock (clients)
                {
                    foreach (var client in clients.ToArray())
                    {
                        if (client.Context.WebSocket.IsAlive == false)
                        {
                            clients.Remove(client);
                            continue;
                        }
                        try
                        {
                            client.Send(message);
                        }
                        catch (Exception ex)
                        {
                            Plugin.Instance.Log.LogWarning($"Failed to send to client: {ex.Message}");
                            clients.Remove(client);
                        }
                    }
                }
            }
        }
    }
}
