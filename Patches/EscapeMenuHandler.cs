using HarmonyLib;
using SandboxImprovements.Utilities;
using UnboundLib.GameModes;
using UnityEngine;

namespace SandboxImprovements.Patches
{
    [HarmonyPatch(typeof(EscapeMenuHandler))]
    public class EscapeMenuHandlerPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        private static bool Update(EscapeMenuHandler __instance)
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return true;

            if (GameModeManager.CurrentHandlerID == GameModeManager.SandBoxID)
                SandboxImprovements.GetRidOfSandboxTutorial();

            if (MapSelectMenuHandler.instance.mapMenuCanvas.activeInHierarchy)
            {
                MapSelectMenuHandler.instance.mapMenuCanvas.SetActive(false);
                return false;
            }
            if (CardSpawnMenuHandler.cardMenuCanvas.activeInHierarchy)
            {
                CardSpawnMenuHandler.cardMenuCanvas.SetActive(false);
                return false;
            }

            return true;
        }
    }
}
