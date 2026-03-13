using HarmonyLib;
using SandboxImprovements.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnboundLib.GameModes;
using UnityEngine;

namespace SandboxImprovements
{
    internal class aiToggle
    {
        internal static bool aiEnabled = true;
        internal static void ToggleAI()
        {
            aiEnabled = !aiEnabled;
            foreach (Player player in PlayerManager.instance.players)
            {
                PlayerAI ai = player.GetComponentInChildren<PlayerAI>();
                if (ai != null)
                    ai.enabled = aiEnabled;

                PlayerAIPhilip aiPhilip = player.GetComponentInChildren<PlayerAIPhilip>();
                if (aiPhilip != null)
                    aiPhilip.enabled = aiEnabled;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerAI))]
    public class PlayerAIPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void Start(PlayerAI __instance)
        {
            __instance.enabled = aiToggle.aiEnabled;
        }
    }
}
