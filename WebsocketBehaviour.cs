using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ExposureUnnoticed2.Object3D.Player.Scripts;
using ExposureUnnoticed2.Object3D.AdultGoods;
using WebSocketSharp.Server;
using WebSocketSharp;

namespace SFMToyWebsocket
{
    internal class WebsocketBehaviour : MonoBehaviour
    {
        private WebSocketServer wssv;
        private float timer = 0f;
        private const float BroadcastInterval = 0.1f;

        // Modes: 0= Off, 1 = Low, 2 = High
        public static int vibeMode = 0;
        // Piston modes: 0 = Off, 1 = Low, 2 = Medium, 3 = High
        public static int pistonMode = 0;

        private void Start()
        {
            try
            {
                wssv = new WebSocketServer(11451);
                wssv.AddWebSocketService<StatusService>("/ws");
                wssv.Start();
                Plugin.Instance.Log.LogInfo("WebSocketSharp server started on ws://localhost:11451/ws/");
            }
            catch (Exception ex)
            {
                Plugin.Instance.Log.LogError($"WebSocket server failed to start: {ex}");
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

        private void BroadcastToyStatus()
        {
            string json = $"{{\"vibe\":{vibeMode},\"piston\":{pistonMode}}}";
            StatusService.Broadcast(json);
        }

        private void OnDestroy()
        {
            if (wssv != null)
            {
                wssv.Stop();
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
