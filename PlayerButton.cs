using SandboxImprovements.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SandboxImprovements
{
    internal class PlayerButton : MonoBehaviour
    {
        public int playerID;

        public void SelectPlayer()
        {
            // Clear logic
            if (playerID == -1)
            {
                CardSpawnMenuHandler.instance.selectedPlayers.Clear();
                foreach (GameObject button in CardSpawnMenuHandler.instance.playerButtons.Values)
                    button.transform.GetChild(0).gameObject.SetActive(false);
                return;
            }

            if (CardSpawnMenuHandler.instance.selectedPlayers.Contains(playerID))
            {
                // Disable highlight
                transform.GetChild(0).gameObject.SetActive(false);
                CardSpawnMenuHandler.instance.selectedPlayers.Remove(playerID);
            }
            else
            {
                transform.GetChild(0).gameObject.SetActive(true);
                CardSpawnMenuHandler.instance.selectedPlayers.Add(playerID);
            }
        }
    }
}
