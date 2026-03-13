using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnboundLib;
using UnboundLib.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SandboxImprovements.Utilities
{
    public class CardSpawnMenuHandler : MonoBehaviour
    {
        public static CardSpawnMenuHandler instance;

        internal static readonly Dictionary<string, Transform> ScrollViews = new Dictionary<string, Transform>();

        public static readonly Dictionary<GameObject, Action> cardObjs = new Dictionary<GameObject, Action>();
        public static readonly List<Action> defaultCardActions = new List<Action>();

        private readonly Dictionary<string, List<GameObject>> cardObjectsInCategory = new Dictionary<string, List<GameObject>>();
        private readonly List<GameObject> categoryObjs = new List<GameObject>();

        public static GameObject cardMenuCanvas;

        private GameObject cardObjAsset;
        private GameObject cardScrollViewAsset;
        private GameObject categoryButtonAsset;
        private GameObject playerButtonAsset;

        private Transform scrollViewTrans;
        internal static Transform CategoryContent;
        private static Transform PlayerButtonContent;

        private static bool sortedByName = true;

        private static TextMeshProUGUI cardAmountText;

        private int currentColumnAmount = 5;
        private string currentCategory = "Vanilla";
        private string currentSearch = "";

        // if need to toggle all on or off
        private bool toggledAll;
        private Coroutine cardVisualsCoroutine = null;

        public List<int> selectedPlayers = new List<int>();
        public Dictionary<int, GameObject> playerButtons = new Dictionary<int, GameObject>();

        private void Start()
        {
            instance = this;
            var mainCamera = GameObject.Find("MainCamera").GetComponent<Camera>();

            var cardMenu = SandboxImprovements.Assets.LoadAsset<GameObject>("CardMenuCanvas");

            cardObjAsset = SandboxImprovements.Assets.LoadAsset<GameObject>("CardObj");

            cardScrollViewAsset = SandboxImprovements.Assets.LoadAsset<GameObject>("CardScrollView");
            categoryButtonAsset = SandboxImprovements.Assets.LoadAsset<GameObject>("CategoryButton");
            playerButtonAsset = SandboxImprovements.Assets.LoadAsset<GameObject>("PlayerButton");

            cardMenuCanvas = Instantiate(cardMenu);
            DontDestroyOnLoad(cardMenuCanvas);

            var canvas = cardMenuCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCamera;
            cardMenuCanvas.SetActive(false);

            scrollViewTrans = cardMenuCanvas.transform.Find("CardMenu/ScrollViews");

            CategoryContent = cardMenuCanvas.transform.Find("CardMenu/Top/Categories/ButtonsScroll/Viewport/Content");

            PlayerButtonContent = cardMenuCanvas.transform.Find("CardMenu/Top/Target Players/Viewport/Content");

            // Create and set search bar
            var searchBar = cardMenuCanvas.transform.Find("CardMenu/Top/InputField").gameObject;
            searchBar.GetComponent<TMP_InputField>().onValueChanged.AddListener(value =>
            {
                currentSearch = value;
                foreach (var card in ScrollViews[currentCategory].GetComponentsInChildren<Button>(true))
                {
                    var active = ActiveOnSearch(card.gameObject.name);
                    card.gameObject.SetActive(active);
                    if (active)
                    {
                        UpdateVisualsCardObj(card.gameObject);
                    }
                }
            });

            // create and set sort button (making use of the unused "Switch profile" button)
            cardMenuCanvas.transform.Find("CardMenu/Top/SortBy").GetComponentInChildren<TextMeshProUGUI>().text = "Sort By: " + (sortedByName ? "Name" : "Rarity");
            var sortButton = cardMenuCanvas.transform.Find("CardMenu/Top/SortBy").GetComponent<Button>();
            sortButton.onClick.AddListener(() =>
            {
                sortedByName = !sortedByName;
                cardMenuCanvas.transform.Find("CardMenu/Top/SortBy").GetComponentInChildren<TextMeshProUGUI>().text = "Sort By: " + (sortedByName ? "Name" : "Rarity");

                SortCardMenus(sortedByName);
            });

            Transform cardAmountObject = cardMenuCanvas.transform.Find("CardMenu/Top/CardAmount");
            cardAmountText = cardAmountObject.GetComponentInChildren<TextMeshProUGUI>();

            var cardAmountSlider = cardAmountObject.GetComponentsInChildren<Slider>();
            foreach (Slider slider in cardAmountSlider)
            {
                slider.onValueChanged.AddListener(amount =>
                {
                    int integerAmount = (int)amount;
                    ChangeCardColumnAmountMenus(integerAmount);
                });
            }

            this.ExecuteAfterSeconds(0.5f, () =>
            {
                cardMenuCanvas.SetActive(true);
                // Create category scrollViews
                foreach (var category in CardManager.categories)
                {
                    var scrollView = Instantiate(cardScrollViewAsset, scrollViewTrans);
                    scrollView.SetActive(true);
                    scrollView.name = category;
                    ScrollViews.Add(category, scrollView.transform);
                }

                // Create cardObjects
                foreach (var card in CardManager.cards)
                {
                    Card cardValue = card.Value;
                    if (cardValue == null) continue;
                    var parentScroll = ScrollViews[cardValue.category].Find("Viewport/Content");
                    var cardObject = Instantiate(cardObjAsset, parentScroll);
                    cardObject.name = card.Key;
                    CardInfo cardInfo = cardValue.cardInfo;
                    if (cardInfo == null) continue;
                    SetupCardVisuals(cardInfo, cardObject);
                    cardObject.SetActive(false);
                    if (!cardObjectsInCategory.ContainsKey(cardValue.category))
                    {
                        cardObjectsInCategory.Add(cardValue.category, new List<GameObject>());
                    }
                    cardObjectsInCategory[cardValue.category].Add(cardObject);

                    void CardAction()
                    {
                        if (selectedPlayers.Count == 0)
                        {
                            GameObject obj = CardChoice.instance.AddCard(cardValue.cardInfo);
                            obj.GetComponentInChildren<CardVisuals>().firstValueToSet = true;
                            obj.transform.root.GetComponentInChildren<ApplyCardStats>().shootToPick = true;
                        }
                        else foreach (int playerID in selectedPlayers)
                        {
                            Player player = PlayerManager.instance.players.Find(p => p.playerID == playerID);
                            ModdingUtils.Utils.Cards.instance.AddCardToPlayer(player, cardValue.cardInfo, false, "", 0, 0, true);
                        }
                    }

                    cardObjs[cardObject] = CardAction;
                    defaultCardActions.Add(CardAction);

                    UpdateVisualsCardObj(cardObject);
                }
                UpdateCardColumnAmountMenus();

                foreach (Transform scrollView in ScrollViews.Values)
                {
                    SetActive(scrollView.transform, false);
                    if (scrollView.name == "Vanilla")
                    {
                        SetActive(scrollView.transform, true);
                    }
                }

                var viewingText = cardMenuCanvas.transform.Find("CardMenu/Top/Viewing").gameObject.GetComponentInChildren<TextMeshProUGUI>();

                // Create category buttons
                // sort categories
                // always have Vanilla first, then sort most cards -> least cards, followed by "Modded" at the end (if it exists)
                List<string> sortedCategories = new[] { "Vanilla" }.Concat(CardManager.categories.OrderBy(x => x).Except(new[] { "Vanilla" })).ToList();

                foreach (var category in sortedCategories)
                {
                    var categoryObj = Instantiate(categoryButtonAsset, CategoryContent);
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
                        string categoryText = "Viewing: " + category;
                        if (viewingText.text == categoryText) return;
                        viewingText.text = categoryText;

                        foreach (var scroll in ScrollViews)
                        {
                            DisableCardsInCategory(scroll.Key);
                            SetActive(scroll.Value, false);
                        }

                        ScrollViews[category].GetComponent<ScrollRect>().normalizedPosition = new Vector2(0, 1);
                        SetActive(ScrollViews[category].transform, true);

                        // cardVisualsCoroutine = Unbound.Instance.StartCoroutine(EnableCardsInCategory(category));
                        currentCategory = category;
                    });

                    categoryObjs.Add(categoryObj);

                    var toggle = categoryObj.GetComponentInChildren<Toggle>();
                    toggle.onValueChanged.AddListener(value =>
                    {
                        toggle.isOn = CardManager.IsCategoryActive(category);
                    });

                    UpdateCategoryVisuals(categoryObj, CardManager.IsCategoryActive(category), true);
                }
                for (var i = 0; i < cardObjs.Keys.Count; i++)
                {
                    var buttonEvent = new Button.ButtonClickedEvent();
                    var unityAction = new UnityAction(cardObjs.ElementAt(i).Value);
                    buttonEvent.AddListener(unityAction);
                    cardObjs.ElementAt(i).Key.GetComponent<Button>().onClick = buttonEvent;
                }

                cardMenuCanvas.GetComponent<Canvas>().sortingOrder = 256;
                cardMenuCanvas.SetActive(false);
            });
        }

        public void UpdateEnabledCards()
        {
            foreach (GameObject category in categoryObjs)
                UpdateCategoryVisuals(category, CardManager.IsCategoryActive(category.name), true);

            // Player Buttons
            foreach (var button in playerButtons) // Remove buttons without players
            { 
                foreach (Player player in PlayerManager.instance.players)
                    if (player.playerID == button.Key) continue;
                Destroy(button.Value);
                playerButtons.Remove(button.Key);
            }

            foreach (Player player in PlayerManager.instance.players) // Add buttons for new players
            {
                if (!playerButtons.ContainsKey(player.playerID))
                {
                    GameObject newButton = Instantiate(playerButtonAsset, PlayerButtonContent);
                    newButton.transform.GetChild(1).GetComponent<Image>().color = player.GetTeamColors().color;
                    newButton.GetComponent<PlayerButton>().playerID = player.playerID;
                    playerButtons.Add(player.playerID, newButton);
                }
            }
        }

        private void UpdateCategoryVisuals(GameObject categoryObj, bool enabledVisuals, bool firstTime = false)
        {
            foreach (var cardObject in ScrollViews.Where(obj => obj.Key == categoryObj.name))
            {
                cardObject.Value.Find("Darken").gameObject.SetActive(!enabledVisuals);
            }
            if (!firstTime)
            {
                string[] cardsInCategory = CardManager.GetCardsInCategory(categoryObj.name);
                foreach (GameObject cardObject in cardObjs.Keys.Where(o => cardsInCategory.Contains(o.name)))
                {
                    UpdateVisualsCardObj(cardObject);
                }
            }

            var toggle = categoryObj.GetComponentInChildren<Toggle>();
            toggle.isOn = CardManager.IsCategoryActive(categoryObj.name);
        }

        private bool ActiveOnSearch(string cardName)
        {
            var result = cardName.Contains("__") ? cardName.Split(new[] { "__" }, StringSplitOptions.None) : new[] { cardName };
            var process = result.Length > 2 ? result[2] : result[0];
            return currentSearch == "" || process.ToUpper().Contains(currentSearch.ToUpper());
        }

        private void DisableCards()
        {
            foreach (GameObject cardObject in CardManager.categories.SelectMany(category => cardObjectsInCategory[category]))
            {
                cardObject.SetActive(false);
            }
        }

        private void DisableCardsInCategory(string category)
        {
            if (!cardObjectsInCategory.ContainsKey(category)) return;
            foreach (GameObject cardObject in cardObjectsInCategory[category])
            {
                cardObject.SetActive(false);
            }
        }

        private IEnumerator EnableCardsInCategory(string category)
        {
            if (!cardObjectsInCategory.ContainsKey(category)) yield break;
            foreach (GameObject cardObject in cardObjectsInCategory[category])
            {
                var active = ActiveOnSearch(cardObject.name);
                cardObject.gameObject.SetActive(active);
                UpdateVisualsCardObj(cardObject);
                yield return new WaitForEndOfFrame();
            }
        }

        internal static Color uncommonColor = new Color(0, 0.5f, 1, 1);
        internal static Color rareColor = new Color(1, 0.2f, 1, 1);

        private static void SetupCardVisuals(CardInfo cardInfo, GameObject parent)
        {
            GameObject cardObject = Instantiate(cardInfo.gameObject, parent.gameObject.transform);
            cardObject.AddComponent<MenuCard>();
            cardObject.SetActive(true);

            GameObject cardFrontObject = FindObjectInChildren(cardObject, "Front");
            if (cardFrontObject == null)
            {
                return;
            }

            // cardInfo.gameObject.name = parent.name;
            GameObject back = FindObjectInChildren(cardObject, "Back");
            Destroy(back);

            GameObject damagable = FindObjectInChildren(cardObject, "Damagable");
            Destroy(damagable);

            foreach (CardVisuals componentsInChild in cardObject.GetComponentsInChildren<CardVisuals>())
            {
                componentsInChild.firstValueToSet = true;
            }

            FindObjectInChildren(cardObject, "BlockFront")?.SetActive(false);

            var canvasGroups = cardObject.GetComponentsInChildren<CanvasGroup>();
            foreach (var canvasGroup in canvasGroups)
            {
                canvasGroup.alpha = 1;
            }

            // // Creates problems if it's not in the game scene and also is the main cause of lag
            GameObject uiParticleObject = FindObjectInChildren(cardFrontObject.gameObject, "UI_ParticleSystem");
            if (uiParticleObject != null)
            {
                Destroy(uiParticleObject);
            }

            if (cardInfo.cardArt != null)
            {
                var artObject = FindObjectInChildren(cardFrontObject.gameObject, "Art");
                if (artObject != null)
                {
                    var cardAnimationHandler = cardObject.AddComponent<CardAnimationHandler>();
                    cardAnimationHandler.ToggleAnimation(false);
                }
            }

            var backgroundObj = FindObjectInChildren(cardFrontObject.gameObject, "Background");
            if (backgroundObj == null) return;

            backgroundObj.transform.localScale = new Vector3(1, 1, 1);
            var rectTransform = backgroundObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(1500f, 1500f);

            var imageComponent = backgroundObj.gameObject.GetComponentInChildren<Image>(true);
            if (imageComponent != null)
            {
                imageComponent.preserveAspect = true;
                imageComponent.color = new Color(0.16f, 0.16f, 0.16f, 1f);
            }

            var maskComponent = backgroundObj.gameObject.GetComponentInChildren<Mask>(true);
            if (maskComponent != null)
            {
                maskComponent.showMaskGraphic = true;
            }

            RectTransform rect = cardObject.GetOrAddComponent<RectTransform>();
            rect.localScale = 8f * Vector3.one;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);

            var cardColor = CardChoice.instance.GetCardColor(cardInfo.colorTheme);
            var edgePieces = cardFrontObject.GetComponentsInChildren<Image>(true)
                .Where(x => x.gameObject.transform.name.Contains("FRAME")).ToList();
            foreach (Image edgePiece in edgePieces)
            {
                edgePiece.color = cardColor;
            }

            var textName = cardFrontObject.transform.GetChild(1);
            if (textName != null)
            {
                var textComponent = textName.GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = cardInfo.cardName.ToUpper();
                    textComponent.color = cardColor;
                }
            }

            if (cardInfo.rarity == CardInfo.Rarity.Common) return;

            var colorFromRarity = cardInfo.rarity == CardInfo.Rarity.Uncommon
                ? uncommonColor
                : rareColor;
            foreach (var imageComponentLoop in FindObjectsInChildren(cardFrontObject.gameObject,
                             "Triangle").Select(triangleObject =>
                             triangleObject.GetComponent<Image>())
                         .Where(imageComponentLoop => imageComponent != null))
            {
                imageComponentLoop.color = colorFromRarity;
            }
        }

        private static IEnumerable<GameObject> FindObjectsInChildren(GameObject gameObject, string gameObjectName)
        {
            Transform[] children = gameObject.GetComponentsInChildren<Transform>(true);
            return (from item in children where item.name == gameObjectName select item.gameObject).ToList();
        }

        private static GameObject FindObjectInChildren(GameObject gameObject, string gameObjectName)
        {
            Transform[] children = gameObject.GetComponentsInChildren<Transform>(true);
            return (from item in children where item.name == gameObjectName select item.gameObject).FirstOrDefault();
        }

        public static void UpdateVisualsCardObj(GameObject cardObject, bool? cardEnabled = null)
        {
            if (cardEnabled ?? CardManager.cards[cardObject.name].enabled)
            {
                cardObject.SetActive(true);
                foreach (CurveAnimation curveAnimation in cardObject.GetComponentsInChildren<CurveAnimation>())
                {
                    if (curveAnimation.gameObject.activeInHierarchy)
                    {
                        curveAnimation.PlayIn();
                    }
                }
            }
            else
            {
                cardObject.SetActive(false);
                foreach (CurveAnimation curveAnimation in cardObject.GetComponentsInChildren<CurveAnimation>())
                {
                    if (!curveAnimation.gameObject.activeInHierarchy) continue;
                    curveAnimation.PlayIn();
                    curveAnimation.PlayOut();
                }
            }
        }

        internal static void RestoreCardToggleVisuals()
        {
            foreach (GameObject cardObject in cardObjs.Keys)
            {
                UpdateVisualsCardObj(cardObject);
            }
        }

        internal void RestoreCardToggleVisuals(string category)
        {
            if (!cardObjectsInCategory.ContainsKey(category)) return;
            foreach (GameObject cardObject in cardObjs.Keys)
            {
                UpdateVisualsCardObj(cardObject);
            }
        }

        internal void SortCardMenus(bool alph)
        {
            foreach (string category in CardManager.categories)
            {
                Transform categoryMenu = ScrollViews[category].Find("Viewport/Content");

                List<Transform> cardsInMenu = new List<Transform>() { };
                cardsInMenu.AddRange(categoryMenu.Cast<Transform>());

                List<Transform> sorted = alph ? cardsInMenu.OrderBy(t => t.name).ToList() : cardsInMenu.OrderBy(t => CardManager.cards[t.name].cardInfo.rarity).ThenBy(t => t.name).ToList();

                int i = 0;
                foreach (Transform cardInMenu in sorted)
                {
                    cardInMenu.SetSiblingIndex(i);
                    i++;
                }
            }
        }

        private static void ChangeCardColumnAmountMenus(int amount)
        {
            Vector2 cellSize = new Vector2(220, 300);
            float localScale = 1.5f;

            if (amount > 3)
            {
                switch (amount)
                {
                    case 4:
                        {
                            cellSize = new Vector2(170, 240);
                            localScale = 1.2f;
                            break;
                        }
                    default:
                        {
                            cellSize = new Vector2(136, 192);
                            localScale = 0.9f;
                            break;
                        }
                    case 6:
                        {
                            cellSize = new Vector2(112, 158);
                            localScale = 0.75f;
                            break;
                        }
                    case 7:
                        {
                            cellSize = new Vector2(97, 137);
                            localScale = 0.65f;
                            break;
                        }
                    case 8:
                        {
                            cellSize = new Vector2(85, 120);
                            localScale = 0.55f;
                            break;
                        }
                    case 9:
                        {
                            cellSize = new Vector2(75, 106);
                            localScale = 0.45f;
                            break;
                        }
                    case 10:
                        {
                            cellSize = new Vector2(68, 96);
                            localScale = 0.4f;
                            break;
                        }
                }
            }

            instance.currentColumnAmount = amount;
            cardAmountText.text = "Cards Per Line: " + amount;
            foreach (string category in CardManager.categories)
            {
                Transform categoryMenu = ScrollViews[category].Find("Viewport/Content");
                var gridLayout = categoryMenu.gameObject.GetComponent<GridLayoutGroup>();
                gridLayout.cellSize = cellSize;
                gridLayout.constraintCount = amount;
                gridLayout.childAlignment = TextAnchor.UpperCenter;

                List<Transform> cardsInMenu = new List<Transform>();
                cardsInMenu.AddRange(categoryMenu.Cast<Transform>());

                foreach (var rect in cardsInMenu.Select(cardTransform => cardTransform.GetChild(2).gameObject.GetOrAddComponent<RectTransform>()))
                {
                    rect.localScale = localScale * Vector3.one * 10;
                }
            }
        }

        public static void UpdateCardColumnAmountMenus()
        {
            ChangeCardColumnAmountMenus(instance.currentColumnAmount);
        }

        /// <summary> This is used for opening and closing menus </summary>
        public static void SetActive(Transform trans, bool active)
        {
            if (active)
            {
                // Main camera changes when going back to menu and glow disappears if we don't se the camera again to the canvas
                Camera mainCamera = GameObject.Find("MainCamera").GetComponent<Camera>();
                Canvas canvas = cardMenuCanvas.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = mainCamera;
            }

            if (trans.gameObject != null) trans.gameObject.SetActive(active);

            Unbound.Instance.ExecuteAfterFrames(1, () =>
            {
                if (active)
                {
                    if (instance.cardVisualsCoroutine != null)
                    {
                        Unbound.Instance.StopCoroutine(instance.cardVisualsCoroutine);
                    }

                    instance.cardVisualsCoroutine = Unbound.Instance.StartCoroutine(instance.currentCategory != null ? instance.EnableCardsInCategory(instance.currentCategory) : instance.EnableCardsInCategory("Vanilla"));
                }
                else
                {
                    instance.DisableCardsInCategory(instance.currentCategory ?? "Vanilla");
                }
            });
        }
    }

    internal class MenuCard : MonoBehaviour
    {
        private ScaleShake scaleShake;

        private void Start()
        {
            scaleShake = GetComponentInChildren<ScaleShake>();
        }

        private void Update()
        {
            if (scaleShake == null)
            {
                scaleShake = GetComponentInChildren<ScaleShake>();
                return;
            }

            if (scaleShake.targetScale <= 1f) return;
            scaleShake.targetScale = 1f;
        }
    }

    internal class CardAnimationHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private bool toggled;

        public void OnPointerEnter(PointerEventData eventData)
        {
            ToggleAnimation(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ToggleAnimation(false);
        }

        public void ToggleAnimation(bool value)
        {
            foreach (Animator animatorComponent in gameObject.GetComponentsInChildren<Animator>())
            {
                if (animatorComponent.enabled == value) continue;
                animatorComponent.enabled = value;
            }
            foreach (PositionNoise positionComponent in gameObject.GetComponentsInChildren<PositionNoise>())
            {
                if (positionComponent.enabled == value) continue;
                positionComponent.enabled = value;
            }

            toggled = value;
        }

        private void Update()
        {
            ToggleAnimation(toggled);
        }
    }
}
