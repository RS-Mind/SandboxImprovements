using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnboundLib;
using UnboundLib.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace SandboxImprovements.Utilities
{
    public class MapSelectMenuHandler : MonoBehaviour
    {
        public static MapSelectMenuHandler instance;

        // lvl canvas GameObject
        public GameObject mapMenuCanvas;

        // Dictionary of scrollView names(category name) compared with the transforms of the scroll views
        internal static readonly Dictionary<string, Transform> ScrollViews = new Dictionary<string, Transform>();

        // Content obj in category scroll view
        internal static Transform CategoryContent;
        // Transform of root scroll views obj
        private Transform scrollViewTrans;

        // guiStyle for waiting text
        private GUIStyle guiStyle;

        // Loaded assets
        private GameObject mapObj;
        private GameObject categoryButton;
        private GameObject scrollView;

        // A list of levelNames that need to redraw their art
        private readonly List<string> levelsThatNeedToRedrawn = new List<string>();
        private readonly List<GameObject> categoryObjs = new List<GameObject>();

        // List of every mapObject
        public readonly List<GameObject> lvlObjs = new List<GameObject>();

        private bool disabled;

        private string CurrentCategory => (from scroll in ScrollViews where scroll.Value.gameObject.activeInHierarchy select scroll.Key).FirstOrDefault();

        // if need to toggle all on or off
        private bool toggledAll;

        private static TextMeshProUGUI mapAmountText;

        public void Start()
        {
            instance = this;

            var mainCamera = GameObject.Find("MainCamera").GetComponent<Camera>();

            // Load assets
            var mapsMenuCanvas = SandboxImprovements.Assets.LoadAsset<GameObject>("MapMenuCanvas");
            mapObj = SandboxImprovements.Assets.LoadAsset<GameObject>("MapObj");
            categoryButton = SandboxImprovements.Assets.LoadAsset<GameObject>("CategoryButton");
            scrollView = SandboxImprovements.Assets.LoadAsset<GameObject>("MapScrollView");

            // Create guiStyle for waiting text
            guiStyle = new GUIStyle { fontSize = 100, normal = { textColor = Color.black } };

            // Create mapMenuCanvas
            mapMenuCanvas = Instantiate(mapsMenuCanvas);
            DontDestroyOnLoad(mapMenuCanvas);

            var canvas = mapMenuCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCamera;
            mapMenuCanvas.SetActive(false);

            // Set important root objects
            CategoryContent = mapMenuCanvas.transform.Find("MapMenu/Top/Categories/ButtonsScroll/Viewport/Content");
            scrollViewTrans = mapMenuCanvas.transform.Find("MapMenu/ScrollViews");

            // Create and set searchbar
            var searchBar = mapMenuCanvas.transform.Find("MapMenu/Top/InputField").gameObject;
            searchBar.GetComponent<TMP_InputField>().onValueChanged.AddListener(value =>
            {
                foreach (var level in ScrollViews.SelectMany(scrollViewPair => scrollViewPair.Value.GetComponentsInChildren<Button>(true)))
                {
                    if (value == "")
                    {
                        level.gameObject.SetActive(true);
                        continue;
                    }

                    level.gameObject.SetActive(level.name.ToUpper().Contains(value.ToUpper()));
                }
            });

            Transform mapAmountObject = mapMenuCanvas.transform.Find("MapMenu/Top/MapAmount");
            mapAmountText = mapAmountObject.GetComponentInChildren<TextMeshProUGUI>();

            var cardAmountSlider = mapAmountObject.GetComponentsInChildren<Slider>();
            foreach (Slider slider in cardAmountSlider)
            {
                slider.onValueChanged.AddListener(amount =>
                {
                    int integerAmount = (int)amount;
                    ChangeMapColumnAmountMenus(integerAmount);
                });
            }

            this.ExecuteAfterSeconds(0.5f, () =>
            {
                mapMenuCanvas.SetActive(true);

                // Create category scrollViews
                Type type = typeof(LevelManager);
                FieldInfo info = type.GetField("categories", BindingFlags.NonPublic | BindingFlags.Static);
                List<string> categories = (List<string>)info.GetValue(null);
                foreach (var category in categories)
                {
                    var newScrollView = Instantiate(scrollView, scrollViewTrans);
                    newScrollView.SetActive(false);
                    newScrollView.name = category;
                    ScrollViews.Add(category, newScrollView.transform);
                    if (category == "Vanilla")
                    {
                        newScrollView.SetActive(true);
                    }

                }
                // Create lvlObjs
                foreach (var level in LevelManager.levels)
                {
                    if (!File.Exists(Path.Combine("./LevelImages", LevelManager.GetVisualName(level.Key) + ".png")))
                    {
                        levelsThatNeedToRedrawn.Add(level.Key);
                    }

                    var parentScroll = ScrollViews[level.Value.category].Find("Viewport/Content");
                    var mapObject = Instantiate(mapObj, parentScroll);
                    mapObject.SetActive(true);

                    mapObject.name = level.Key;

                    mapObject.GetComponentInChildren<TextMeshProUGUI>().text = LevelManager.GetVisualName(level.Value.name);
                    mapObject.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        if (level.Value.enabled)
                        {
                            LevelManager.instance.InvokeMethod("SpawnMap", "/" + level.Key);
                            mapMenuCanvas.SetActive(false);
                            GameObject.Find("Game/UI/UI_Game/Canvas/EscapeMenu").GetComponent<EscapeMenuHandler>().ToggleEsc();
                            SandboxImprovements.GetRidOfSandboxTutorial();
                        }
                    });

                    lvlObjs.Add(mapObject);
                    UpdateVisualsLevelObj(mapObject);
                    UpdateImage(mapObject, Path.Combine("./LevelImages", LevelManager.GetVisualName(level.Key) + ".png"));
                }

                var viewingText = mapMenuCanvas.transform.Find("MapMenu/Top/Viewing").gameObject.GetComponentInChildren<TextMeshProUGUI>();

                // Create category buttons
                List<string> sortedCategories = new[] { "Vanilla", "Default physics" }.Concat(categories.OrderBy(c => c).Except(new[] { "Vanilla", "Default physics" })).ToList();
                foreach (var category in sortedCategories)
                {
                    var categoryObj = Instantiate(categoryButton, CategoryContent);
                    categoryObj.SetActive(true);
                    categoryObj.name = category;
                    categoryObj.GetComponentInChildren<TextMeshProUGUI>().text = category;
                    categoryObj.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        foreach (Animator buttonAnimator in CategoryContent.GetComponentsInChildren<Animator>())
                        {
                            if (!categoryObj.GetComponentsInChildren<Animator>().Contains(buttonAnimator))
                                buttonAnimator.SetTrigger("False");
                        }
                        foreach (Animator buttonAnimator in categoryObj.GetComponentsInChildren<Animator>())
                        {
                            buttonAnimator.SetTrigger("True");
                        }
                        foreach (var scroll in ScrollViews)
                        {
                            scroll.Value.gameObject.SetActive(false);
                        }

                        categoryObjs.Add(categoryObj);

                        ScrollViews[category].GetComponent<ScrollRect>().normalizedPosition = new Vector2(0, 1);
                        ScrollViews[category].gameObject.SetActive(true);

                        viewingText.text = "Viewing: " + category;
                    });
                    var toggle = categoryObj.GetComponentInChildren<Toggle>();
                    toggle.onValueChanged.AddListener((bool State) => { toggle.isOn = LevelManager.IsCategoryActive(category); });

                    UpdateCategoryVisuals(categoryObj, LevelManager.IsCategoryActive(category));
                }

                mapMenuCanvas.GetComponent<Canvas>().sortingOrder = 256;
                mapMenuCanvas.SetActive(false);

                // Detect which levels need to redraw
                //if(levelsThatNeedToRedrawn.Count != 0) StartCoroutine(LoadScenesForRedrawing(levelsThatNeedToRedrawn.ToArray()));
            });
        }

        private void UpdateCategoryVisuals(GameObject categoryObj, bool enabledVisuals)
        {
            foreach (var obj in ScrollViews.Where(obj => obj.Key == categoryObj.name))
            {
                obj.Value.Find("Darken").gameObject.SetActive(!enabledVisuals);
            }

            var toggle = categoryObj.GetComponentInChildren<Toggle>();
            toggle.isOn = enabledVisuals;
        }

        public void UpdateEnabledMaps()
        {
            foreach (var category in categoryObjs)
                UpdateCategoryVisuals(category, LevelManager.IsCategoryActive(category.name));
            foreach (var levelObj in lvlObjs)
                UpdateVisualsLevelObj(levelObj);
        }

        // Update the visuals of a mapObject
        public static void UpdateVisualsLevelObj(GameObject lvlObj)
        {
            if (!LevelManager.levels.ContainsKey(lvlObj.name)) return;
            if (LevelManager.levels[lvlObj.name].enabled)
            {
                lvlObj.SetActive(true);
                lvlObj.transform.Find("Image").GetComponent<Image>().color = Color.white;
                lvlObj.transform.Find("Background").GetComponent<Image>().color = new Color(0.2352941f, 0.2352941f, 0.2352941f, 0.8470588f);
                lvlObj.GetComponentInChildren<TextMeshProUGUI>().color = new Color(0.5372549f, 0.5372549f, 0.5372549f, 1f);
            }
            else
                lvlObj.SetActive(false);
        }

        // Update the image of a mapObject
        private static void UpdateImage(GameObject mapObject, string imagePath)
        {
            if (!File.Exists(imagePath)) return;

            var image = mapObject.transform.Find("Image").gameObject;
            var fileData = File.ReadAllBytes(imagePath);
            var img = new Texture2D(1, 1);
            img.LoadImage(fileData);
            image.GetComponent<Image>().sprite = Sprite.Create(img, new Rect(0, 0, img.width, img.height), new Vector2(0.5f, 0.5f));
        }

        private static void ChangeMapColumnAmountMenus(int amount)
        {
            Vector2 cellSize = new Vector2(164, 112);
            float localScale = 4f / amount;
            cellSize *= localScale;

            mapAmountText.text = "Maps Per Line: " + amount;
            Type type = typeof(LevelManager);
            FieldInfo info = type.GetField("categories", BindingFlags.NonPublic | BindingFlags.Static);
            List<string> categories = (List<string>)info.GetValue(null);
            foreach (GridLayoutGroup gridLayout in from category in categories select ScrollViews[category].Find("Viewport/Content") into categoryMenu where categoryMenu != null select categoryMenu.gameObject.GetComponent<GridLayoutGroup>())
            {
                gridLayout.cellSize = cellSize;
                gridLayout.constraintCount = amount;
                gridLayout.spacing = new Vector2(5f * localScale, 5f * localScale);
            }
        }

        public void SetActive(bool active)
        {
            // Main camera changes when going back to menu and glow disappears if we don't se the camera again to the canvas
            Camera mainCamera = GameObject.Find("MainCamera").GetComponent<Camera>();
            Canvas canvas = mapMenuCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCamera;

            //if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient) return;
            //mapMenuCanvas.SetActive(true);
        }
    }
}