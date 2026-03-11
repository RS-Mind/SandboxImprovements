using HarmonyLib;
using InControl;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using TMPro;
using UnboundLib.GameModes;
using UnityEngine;

namespace SandboxImprovements.Utilities // Adds actions to players
{
    [Serializable]
    public class PlayerActionsAdditionalData
    {
        public PlayerAction hookBattleStart;
        public PlayerAction hookPickStart;
        public PlayerAction hookPickEnd;
        public PlayerAction hookPlayerPickStart;
        public PlayerAction hookPlayerPickEnd;
        public PlayerAction hookPointStart;
        public PlayerAction hookPointEnd;
        public PlayerAction hookRoundStart;
        public PlayerAction hookRoundEnd;

        public PlayerActionsAdditionalData()
        {
            hookBattleStart = null;
            hookPickStart = null;
            hookPickEnd = null;
            hookPlayerPickStart = null;
            hookPlayerPickEnd = null;
            hookPointStart = null;
            hookPointEnd = null;
            hookRoundStart = null;
            hookRoundEnd = null;
        }
    }

    public static class PlayerActionsExtension // Magic
    {
        public static readonly ConditionalWeakTable<PlayerActions, PlayerActionsAdditionalData> data =
            new ConditionalWeakTable<PlayerActions, PlayerActionsAdditionalData>();

        public static PlayerActionsAdditionalData GetAdditionalData(this PlayerActions playerActions)
        {
            return data.GetOrCreateValue(playerActions);
        }

        public static void AddData(this PlayerActions playerActions, PlayerActionsAdditionalData value)
        {
            try
            {
                data.Add(playerActions, value);
            }
            catch (Exception) { }
        }
    }

    [HarmonyPatch(typeof(PlayerActions))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new Type[] { })]
    class PlayerActionsPatchPlayerActions
    {
        private static void Postfix(PlayerActions __instance) // Sandbox hotkeys
        {
            __instance.GetAdditionalData().hookBattleStart = (PlayerAction)typeof(PlayerActions).InvokeMember("CreatePlayerAction",
                                    BindingFlags.Instance | BindingFlags.InvokeMethod |
                                    BindingFlags.NonPublic, null, __instance, new object[] { "Sandbox Call Battle Start" });
            __instance.GetAdditionalData().hookPickStart = (PlayerAction)typeof(PlayerActions).InvokeMember("CreatePlayerAction",
                                    BindingFlags.Instance | BindingFlags.InvokeMethod |
                                    BindingFlags.NonPublic, null, __instance, new object[] { "Sandbox Call Pick Start" });
            __instance.GetAdditionalData().hookPickEnd = (PlayerAction)typeof(PlayerActions).InvokeMember("CreatePlayerAction",
                                    BindingFlags.Instance | BindingFlags.InvokeMethod |
                                    BindingFlags.NonPublic, null, __instance, new object[] { "Sandbox Call Pick End" });
            __instance.GetAdditionalData().hookPlayerPickStart = (PlayerAction)typeof(PlayerActions).InvokeMember("CreatePlayerAction",
                                    BindingFlags.Instance | BindingFlags.InvokeMethod |
                                    BindingFlags.NonPublic, null, __instance, new object[] { "Sandbox Call Player Pick Start" });
            __instance.GetAdditionalData().hookPlayerPickEnd = (PlayerAction)typeof(PlayerActions).InvokeMember("CreatePlayerAction",
                                    BindingFlags.Instance | BindingFlags.InvokeMethod |
                                    BindingFlags.NonPublic, null, __instance, new object[] { "Sandbox Call Player Pick End" });
            __instance.GetAdditionalData().hookPointStart = (PlayerAction)typeof(PlayerActions).InvokeMember("CreatePlayerAction",
                                    BindingFlags.Instance | BindingFlags.InvokeMethod |
                                    BindingFlags.NonPublic, null, __instance, new object[] { "Sandbox Call Point Start" });
            __instance.GetAdditionalData().hookPointEnd = (PlayerAction)typeof(PlayerActions).InvokeMember("CreatePlayerAction",
                                    BindingFlags.Instance | BindingFlags.InvokeMethod |
                                    BindingFlags.NonPublic, null, __instance, new object[] { "Sandbox Call Point End" });
            __instance.GetAdditionalData().hookRoundStart = (PlayerAction)typeof(PlayerActions).InvokeMember("CreatePlayerAction",
                                    BindingFlags.Instance | BindingFlags.InvokeMethod |
                                    BindingFlags.NonPublic, null, __instance, new object[] { "Sandbox Call Round Start" });
            __instance.GetAdditionalData().hookRoundEnd = (PlayerAction)typeof(PlayerActions).InvokeMember("CreatePlayerAction",
                                    BindingFlags.Instance | BindingFlags.InvokeMethod |
                                    BindingFlags.NonPublic, null, __instance, new object[] { "Sandbox Call Round End" });
        }
    }
    
    [HarmonyPatch(typeof(PlayerActions), "CreateWithKeyboardBindings")] // Voidseer for keyboard
    class PlayerActionsPatchCreateWithKeyboardBindings
    {
        private static void Postfix(ref PlayerActions __result)
        {
            __result.GetAdditionalData().hookBattleStart.AddDefaultBinding(Key.G);
            __result.GetAdditionalData().hookPickStart.AddDefaultBinding(Key.U);
            __result.GetAdditionalData().hookPickEnd.AddDefaultBinding(Key.J);
            __result.GetAdditionalData().hookPlayerPickStart.AddDefaultBinding(Key.Y);
            __result.GetAdditionalData().hookPlayerPickEnd.AddDefaultBinding(Key.H);
            __result.GetAdditionalData().hookPointStart.AddDefaultBinding(Key.I);
            __result.GetAdditionalData().hookPointEnd.AddDefaultBinding(Key.K);
            __result.GetAdditionalData().hookRoundStart.AddDefaultBinding(Key.O);
            __result.GetAdditionalData().hookRoundEnd.AddDefaultBinding(Key.L);
        }
    }

    [HarmonyPatch(typeof(GeneralInput), "Update")]
    class GeneralInputPatchUpdate // Check if the actions happened
    {
        private static void Postfix(GeneralInput __instance)
        {
            if (GameModeManager.CurrentHandlerID != GameModeManager.SandBoxID // Only run in sandbox while not typing in chat
                || GameObject.Find("Game/UI/UI_Game/Canvas/Console").GetComponent<TMP_InputField>().isFocused)
                return;

            var playerActions = __instance.GetComponent<CharacterData>().playerActions.GetAdditionalData();

            if (playerActions.hookBattleStart.WasPressed)
            {
                if (SandboxImprovements.Debug)
                    UnityEngine.Debug.Log("Battle Start");
                SandboxImprovements.instance.StartCoroutine(GameModeManager.TriggerHook(GameModeHooks.HookBattleStart));
            }
            if (playerActions.hookPickStart.WasPressed)
            {
                if (SandboxImprovements.Debug)
                    UnityEngine.Debug.Log("Pick Start");
                SandboxImprovements.instance.StartCoroutine(GameModeManager.TriggerHook(GameModeHooks.HookPickStart));
            }
            if (playerActions.hookPickEnd.WasPressed)
            {
                if (SandboxImprovements.Debug)
                    UnityEngine.Debug.Log("Pick End");
                SandboxImprovements.instance.StartCoroutine(GameModeManager.TriggerHook(GameModeHooks.HookPickEnd));
            }
            if (playerActions.hookPlayerPickStart.WasPressed)
            {
                if (SandboxImprovements.Debug)
                    UnityEngine.Debug.Log("Player Pick Start");
                SandboxImprovements.instance.StartCoroutine(GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickStart));
            }
            if (playerActions.hookPlayerPickEnd.WasPressed)
            {
                if (SandboxImprovements.Debug)
                    UnityEngine.Debug.Log("Player Pick End");
                SandboxImprovements.instance.StartCoroutine(GameModeManager.TriggerHook(GameModeHooks.HookPlayerPickEnd));
            }
            if (playerActions.hookPointStart.WasPressed)
            {
                if (SandboxImprovements.Debug)
                    UnityEngine.Debug.Log("Point Start");
                SandboxImprovements.instance.StartCoroutine(GameModeManager.TriggerHook(GameModeHooks.HookPointStart));
            }
            if (playerActions.hookPointEnd.WasPressed)
            {
                if (SandboxImprovements.Debug)
                    UnityEngine.Debug.Log("Point End");
                SandboxImprovements.instance.StartCoroutine(GameModeManager.TriggerHook(GameModeHooks.HookPointEnd));
            }
            if (playerActions.hookRoundStart.WasPressed)
            {
                if (SandboxImprovements.Debug)
                    UnityEngine.Debug.Log("Round Start");
                SandboxImprovements.instance.StartCoroutine(GameModeManager.TriggerHook(GameModeHooks.HookRoundStart));
            }
            if (playerActions.hookRoundEnd.WasPressed)
            {
                if (SandboxImprovements.Debug)
                    UnityEngine.Debug.Log("Round End");
                SandboxImprovements.instance.StartCoroutine(GameModeManager.TriggerHook(GameModeHooks.HookRoundEnd));
            }

            if (SandboxImprovements.KeyHintText.activeSelf)
            {
                SandboxImprovements.KeyHintText.transform.Find("BattleStart/Key").GetComponent<TextMeshProUGUI>().text
                    = playerActions.hookBattleStart.Bindings.Count < 1 ? " ": playerActions.hookBattleStart.Bindings[0].Name;
                SandboxImprovements.KeyHintText.transform.Find("Pick/Start").GetComponent<TextMeshProUGUI>().text
                    = playerActions.hookPickStart.Bindings.Count < 1 ? " " : playerActions.hookPickStart.Bindings[0].Name;
                SandboxImprovements.KeyHintText.transform.Find("Pick/End").GetComponent<TextMeshProUGUI>().text
                    = playerActions.hookPickEnd.Bindings.Count < 1 ? " " : playerActions.hookPickEnd.Bindings[0].Name;
                SandboxImprovements.KeyHintText.transform.Find("PlayerPick/Start").GetComponent<TextMeshProUGUI>().text
                    = playerActions.hookPlayerPickStart.Bindings.Count < 1 ? " " : playerActions.hookPlayerPickStart.Bindings[0].Name;
                SandboxImprovements.KeyHintText.transform.Find("PlayerPick/End").GetComponent<TextMeshProUGUI>().text
                    = playerActions.hookPlayerPickEnd.Bindings.Count < 1 ? " " : playerActions.hookPlayerPickEnd.Bindings[0].Name;
                SandboxImprovements.KeyHintText.transform.Find("Point/Start").GetComponent<TextMeshProUGUI>().text
                    = playerActions.hookPointStart.Bindings.Count < 1 ? " " : playerActions.hookPointStart.Bindings[0].Name;
                SandboxImprovements.KeyHintText.transform.Find("Point/End").GetComponent<TextMeshProUGUI>().text
                    = playerActions.hookPointEnd.Bindings.Count < 1 ? " " : playerActions.hookPointEnd.Bindings[0].Name;
                SandboxImprovements.KeyHintText.transform.Find("Round/Start").GetComponent<TextMeshProUGUI>().text
                    = playerActions.hookRoundStart.Bindings.Count < 1 ? " " : playerActions.hookRoundStart.Bindings[0].Name;
                SandboxImprovements.KeyHintText.transform.Find("Round/End").GetComponent<TextMeshProUGUI>().text
                    = playerActions.hookRoundEnd.Bindings.Count < 1 ? " " : playerActions.hookRoundEnd.Bindings[0].Name;
            }
        }
    }
}