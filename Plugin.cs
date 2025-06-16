using BepInEx.Unity.IL2CPP;
using BepInEx;
using UnityEngine;
using Il2CppInterop.Runtime.Injection;
using UnityEngine.SceneManagement;

namespace SFMToyWebsocket
{
    [BepInPlugin("com.Henry1887.SFMToyWebsocket", "Secret Flasher Manaka Toy Websocket", "1.0.0")]
    public class Plugin : BasePlugin
    {
        internal static Plugin Instance { get; private set; }

        public override void Load()
        {
            Instance = this;

            Log.LogInfo("Loading Mod...");

            ClassInjector.RegisterTypeInIl2Cpp<WebsocketBehaviour>();
            SceneManager.add_sceneLoaded((UnityEngine.Events.UnityAction<Scene, LoadSceneMode>)OnSceneLoaded);

            Log.LogInfo("Mod loaded successfully!");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (GameObject.Find("Websocket") == null)
            {
                var obj = new GameObject("Websocket");
                obj.AddComponent<WebsocketBehaviour>();
                UnityEngine.Object.DontDestroyOnLoad(obj);
            }
        }

        public override bool Unload()
        {
            if (GameObject.Find("Websocket") != null)
            {
                UnityEngine.Object.Destroy(GameObject.Find("Websocket"));
            }
            Log.LogInfo("Mod unloaded!");
            return true;
        }
    }
}
