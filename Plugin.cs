using BepInEx.Unity.IL2CPP;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using Il2CppInterop.Runtime.Injection;
using UnityEngine.SceneManagement;

namespace SFMToyWebsocket
{
    [BepInPlugin("com.Henry1887.SFMToyWebsocket", "Secret Flasher Manaka Toy Websocket", "1.0.0")]
    public class Plugin : BasePlugin
    {
        internal static Plugin Instance { get; private set; }

        private Harmony _harmony;

        public override void Load()
        {
            Instance = this;

            Log.LogInfo("Loading Mod...");

            _harmony = new Harmony("com.Henry1887.SFMToyWebsocket");
            _harmony.PatchAll();

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
            _harmony.UnpatchSelf();
            Log.LogInfo("Mod unloaded!");
            return true;
        }
    }
}
