using BepInEx;
using HarmonyLib;
using Jotunn.Utils;
using SandboxImprovements.Utilities;
using TMPro;
using UnboundLib;
using UnboundLib.GameModes;
using UnboundLib.Utils.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

namespace SandboxImprovements
{
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(ModId, ModName, Version)]
    [BepInProcess("Rounds.exe")]
    public class SandboxImprovements : BaseUnityPlugin
    {
        private const string ModId = "com.rsmind.rounds.sandboximprovements";
        private const string ModName = "Sandbox Improvements";
        private const string CompatibilityModName = "SandboxImprovements";
        public const string Version = "1.1.0";
        public const string ModInitials = "SI";
        public static SandboxImprovements instance { get; private set; }
        public static bool Debug = true;
        internal static AssetBundle Assets;
        internal static GameObject KeyHintText;
        internal static GameObject MapSelectButton;
        internal static GameObject CardSpawnButton;

        void Awake()
        {
            Unbound.RegisterClientSideMod(ModId);

            Assets = AssetUtils.LoadAssetBundleFromResources("sandbox", typeof(SandboxImprovements).Assembly);

            if (Assets == null)
            {
                UnityEngine.Debug.Log("Failed to load Fancy Card Bar asset bundle");
            }

            // Add menu handlers
            gameObject.AddComponent<MapSelectMenuHandler>();
            gameObject.AddComponent<CardSpawnMenuHandler>();

            var harmony = new Harmony(ModId);
            harmony.PatchAll();
            SceneManager.sceneLoaded += CreateButtons;
        }


        void Start()
        {
            instance = this;
            Unbound.RegisterMenu(ModName, () => { }, OptionGUI, null, true);
        }

        private static void CreateButtons(Scene scene, LoadSceneMode mode)
        {
            if (KeyHintText == null)
            {
                KeyHintText = Instantiate(Assets.LoadAsset<GameObject>("Hotkey Visualizer"), GameObject.Find("Game/UI/UI_Game/Canvas").transform);
                KeyHintText.GetComponent<Canvas>().sortingLayerName = "MostFront";
                KeyHintText.GetComponent<Canvas>().overrideSorting = true;
            }
            if (MapSelectButton == null)
            {
                MapSelectButton = MenuHandler.CreateButton("Select Map", GameObject.Find("Game/UI/UI_Game/Canvas/EscapeMenu/Main/Group").gameObject,
                    () => {
                        MapSelectMenuHandler.instance.mapMenuCanvas.SetActive(true);
                        MapSelectMenuHandler.instance.UpdateEnabledMaps();
                        foreach (Animator tabAnimator in MapSelectMenuHandler.CategoryContent.GetComponentsInChildren<Animator>())
                            tabAnimator.SetTrigger(MapSelectMenuHandler.ScrollViews[tabAnimator.gameObject.GetComponentInParent<Button>().gameObject.name].gameObject.activeSelf.ToString());
                    });

                CopyComponent(GameObject.Find("Game/UI/UI_Game/Canvas/EscapeMenu/Main/Group/Menu").GetComponent<ProceduralImage>(), MapSelectButton);
                CopyComponent(GameObject.Find("Game/UI/UI_Game/Canvas/EscapeMenu/Main/Group/Menu").GetComponent<FreeModifier>(), MapSelectButton);
                MapSelectButton.GetComponent<RectTransform>().sizeDelta = new Vector2(2050.3f, 90);
                MapSelectButton.GetComponent<Selectable>().transition = Selectable.Transition.ColorTint;
                MapSelectButton.GetComponent<Selectable>().colors = GameObject.Find("Game/UI/UI_Game/Canvas/EscapeMenu/Main/Group/Menu").GetComponent<Selectable>().colors;
                MapSelectButton.transform.SetSiblingIndex(MapSelectButton.transform.parent.childCount - 2);
            }
            if (CardSpawnButton == null)
            {
                CardSpawnButton = MenuHandler.CreateButton("Spawn Cards", GameObject.Find("Game/UI/UI_Game/Canvas/EscapeMenu/Main/Group").gameObject,
                    () => {
                        GameObject.Find("Game/UI/UI_Game/Canvas/EscapeMenu").GetComponent<EscapeMenuHandler>().ToggleEsc();
                        CardSpawnMenuHandler.cardMenuCanvas.SetActive(true);
                        CardSpawnMenuHandler.instance.UpdateEnabledCards();
                        foreach (Animator tabAnimator in CardSpawnMenuHandler.CategoryContent.GetComponentsInChildren<Animator>())
                        {
                            tabAnimator.SetTrigger(CardSpawnMenuHandler.ScrollViews[tabAnimator.gameObject.GetComponentInParent<Button>().gameObject.name].gameObject.activeSelf.ToString());
                        }
                    });

                CopyComponent(GameObject.Find("Game/UI/UI_Game/Canvas/EscapeMenu/Main/Group/Menu").GetComponent<ProceduralImage>(), CardSpawnButton);
                CopyComponent(GameObject.Find("Game/UI/UI_Game/Canvas/EscapeMenu/Main/Group/Menu").GetComponent<FreeModifier>(), CardSpawnButton);
                CardSpawnButton.GetComponent<RectTransform>().sizeDelta = new Vector2(2050.3f, 90);
                CardSpawnButton.GetComponent<Selectable>().transition = Selectable.Transition.ColorTint;
                CardSpawnButton.GetComponent<Selectable>().colors = GameObject.Find("Game/UI/UI_Game/Canvas/EscapeMenu/Main/Group/Menu").GetComponent<Selectable>().colors;
                CardSpawnButton.transform.SetSiblingIndex(CardSpawnButton.transform.parent.childCount - 2);
            }
        }

        static T CopyComponent<T>(T original, GameObject destination) where T : Component
        {
            System.Type type = original.GetType();
            Component copy = destination.AddComponent(type);
            System.Reflection.FieldInfo[] fields = type.GetFields();
            foreach (System.Reflection.FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }
            return copy as T;
        }

        void Update()
        {
            bool sandboxActive = GameModeManager.CurrentHandlerID == GameModeManager.SandBoxID;
            KeyHintText.SetActive(sandboxActive && showKeys);
            MapSelectButton.SetActive(sandboxActive);
            CardSpawnButton.SetActive(sandboxActive);
        }

        public static void GetRidOfSandboxTutorial()
        {
            CurveAnimation sandboxTutorial = GameObject.Find("Game/Code/Game Modes/[GameMode] Sandbox/Canvas/Image").GetComponent<CurveAnimation>();
            sandboxTutorial.Stop();
            sandboxTutorial.InvokeMethod("ResetAnimationState");
        }

        internal static string GetConfigKey(string name)
        {
            return $"{SandboxImprovements.CompatibilityModName}_{name.ToLower()}";
        }

        public static bool showKeys
        {
            get
            {
                return PlayerPrefs.GetInt(GetConfigKey("showKeys"), 1) == 1;
            }
            internal set
            {
                PlayerPrefs.SetInt(GetConfigKey("showKeys"), value ? 1 : 0);
            }
        }

        private static void OptionGUI(GameObject menu)
        {
            MenuHandler.CreateText(ModName + " Options", menu, out TextMeshProUGUI _, 60);
            MenuHandler.CreateText(" ", menu, out TextMeshProUGUI _, 30);
            MenuHandler.CreateToggle(showKeys, "Show Key Prompts", menu, (bool val) => { showKeys = val; });
        }

    }
}