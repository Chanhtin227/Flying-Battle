using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopCategoryManager : MonoBehaviour
{
    // ==========================================
    // CATEGORY
    // ==========================================

    public enum ShopCategory
    {
        Helmet,
        Balo,
        Armor,
        Skin
    }

    [Serializable]
    public class ShopItem
    {
        public string itemID;
        public string itemName;
        public ShopCategory category;

        [Min(0)]
        public int price = 500;

        [Header("Button trong Panel")]
        public Button itemButton;

        [Header("Hinh anh vat pham")]
        public Sprite itemIcon;

        [Header("Gia tren o vat pham")]
        public TMP_Text itemGoldText;

        [Header("Ten hien tren o vat pham (tuy chon - co the tu tim)")]
        [Tooltip("Keo ArmorText / SkinText / HelmetText / BaloText vao day. Neu bo trong, Shop se tu tim trong ItemButton.")]
        public TMP_Text itemNameText;

        [Header("UI - Golden Border khi chon vat pham")]
        [Tooltip("Keo GameObject GoldenBorder nam trong Item Button vao day")]
        public GameObject goldenBorder;

        [Header("3D Preview - New Prefab")]
        public GameObject equipmentPrefab;

        [Header("3D Preview - Existing C01 Object")]
        public GameObject existingEquipmentObject;

        [Header("Skin Material")]
        public Material skinMaterial;
    }

    // ==========================================
    // SHOP UI
    // ==========================================

    [Header("Shop Open / Close")]
    public GameObject shopPanel;
    public Button openShopButton;
    public Button closeShopButton;

    [Header("Hide Main UI While Shop Is Open")]
    public GameObject shoppppp;
    public GameObject settingBtn;
    public GameObject gameModeBtn;
    public GameObject characterView;

    private readonly Dictionary<GameObject, bool> previousMainUIStates =
        new Dictionary<GameObject, bool>();
    private bool mainUIHiddenByShop;

    [Header("Settings")]
    public bool closeWithEscape = true;

    [Header("Category Buttons")]
    public Button helmetButton;
    public Button baloButton;
    public Button armorButton;
    public Button skinButton;

    [Header("Reset Shop")]
    public Button resetShopButton;
    public bool resetGoldWithShop = false;

    [Header("Reset Purchases For New Build Version")]
    [Tooltip("Only in standalone builds. Resets purchases once when Shop Data Version changes.")]
    public bool resetPurchasesOnNewBuild = true;

    [Min(1)]
    [Tooltip("Increase this number before building a new version to reset purchases once.")]
    public int shopDataVersion = 1;

    private const string ShopVersionKey = "FlyingBattle_ShopDataVersion";

    [Header("Purchased Items")]
    public Button purchasedItemsButton;
    public GameObject purchasedItemsPanel;
    public Transform purchasedItemsContainer;

    [Header("Category Panels")]
    public GameObject helmetPanel;
    public GameObject baloPanel;
    public GameObject armorPanel;
    public GameObject skinPanel;

    // ==========================================
    // GOLD + PREVIEW
    // ==========================================

    [Header("Gold UI")]
    public MainGoldUI goldUI;

    [Header("3D Player Preview")]
    public ShopEquipmentPreview equipmentPreview;

    // ==========================================
    // SELECTED ITEM
    // ==========================================

    [Header("Selected Item UI")]
    public TMP_Text selectedItemText;
    public TMP_Text priceText;
    public TMP_Text notificationText;
    public Image selectedItemImage;

    [Header("Notification - Not Enough Gold")]
    [Min(0.1f)] public float insufficientGoldNotificationSeconds = 2f;
    private Coroutine insufficientGoldNotificationCoroutine;

    // LobbyController uses this exact key when saving the player name in Settings.
    private const string PlayerNamePrefsKey = "MyPlayerName";

    [Header("Shared Buy / Equip Button")]
    public Button buyButton;
    public TMP_Text buyButtonText;

    [Header("All Shop Items")]
    public List<ShopItem> shopItems =
        new List<ShopItem>();

    private ShopItem selectedItem;
    private ShopCategory? currentCategory;
    private bool showingPurchasedItems;

    [Header("Golden Border Glow")]
    public bool animateGoldenBorder = true;
    [Min(0.1f)] public float goldenGlowSpeed = 2.5f;
    [Range(0f, 1f)] public float goldenMinAlpha = 0.65f;
    private readonly Dictionary<string, GameObject> purchasedGoldenBorders =
        new Dictionary<string, GameObject>();

    private const string OwnedPrefix =
        "FlyingBattle_ShopOwned_";

    private const string EquipPrefix =
        "FlyingBattle_ShopEquipped_";

    // Gia tri rieng de ghi nho nguoi choi da THAO trang bi.
    private const string UnequippedValue = "__NONE__";

    public Button shopBtn;

    // ==========================================
    // UNITY
    // ==========================================

    private void Start()
    {
        // Only standalone builds perform automatic version-based reset.
        // Must run before reading owned/equipped state or applying 3D previews.
#if !UNITY_EDITOR
        ResetShopForNewBuildVersion();
#endif

        if (openShopButton != null)
            openShopButton.onClick.AddListener(OpenShop);

        if (closeShopButton != null)
            closeShopButton.onClick.AddListener(CloseShop);

        if (helmetButton != null)
            helmetButton.onClick.AddListener(OpenHelmet);

        if (baloButton != null)
            baloButton.onClick.AddListener(OpenBalo);

        if (armorButton != null)
            armorButton.onClick.AddListener(OpenArmor);

        if (skinButton != null)
            skinButton.onClick.AddListener(OpenSkin);

        if (purchasedItemsButton != null)
            purchasedItemsButton.onClick.AddListener(
                OpenPurchasedItems);

        if (buyButton != null)
            buyButton.onClick.AddListener(UseSelectedItem);

        if (resetShopButton != null)
            resetShopButton.onClick.AddListener(ResetShopData);

        // Dang ky trang bi co san tren C01
        if (equipmentPreview != null)
        {
            foreach (ShopItem item in shopItems)
            {
                if (item == null ||
                    item.existingEquipmentObject == null)
                    continue;

                if (item.category != ShopCategory.Skin)
                {
                    equipmentPreview.RegisterExistingEquipment(
                        (int)item.category,
                        item.existingEquipmentObject
                    );
                }
            }
        }

        // Gan su kien cho tung Button vat pham
        foreach (ShopItem item in shopItems)
        {
            if (item == null || item.itemButton == null)
                continue;

            ShopItem capturedItem = item;

            // Canh bao neu dang dung nut tab lam nut item
            if (item.itemButton == helmetButton ||
                item.itemButton == baloButton ||
                item.itemButton == armorButton ||
                item.itemButton == skinButton)
            {
                Debug.LogWarning(
                    "[SHOP] Item Button cua " +
                    item.itemName +
                    " dang trung voi Category Button. " +
                    "Hay dung Button rieng cua o vat pham!",
                    this
                );

                continue;
            }

            item.itemButton.onClick.AddListener(
                () => SelectItem(capturedItem)
            );
        }

        HideAllPanels();
        ClearSelection();
        RefreshItemPrices();

        if (shopPanel != null)
            shopPanel.SetActive(false);

        // Khi vao Scene Main, mac lai do da EQUIP tu lan truoc.
        ApplySavedEquipment();
    }

    private void Update()
    {
        if (!IsShopOpen())
            return;

        UpdateGoldenBorderGlow();
        if (!closeWithEscape)
            return;

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current
                .escapeKey.wasPressedThisFrame)
        {
            CloseShop();
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseShop();
        }
#endif
    }

    // ==========================================
    // OPEN / CLOSE SHOP
    // ==========================================

    // Hide the specified main-screen elements only while Shop is open.
    // Keep their previous activeSelf values so a previously hidden element
    // does not accidentally become visible when closing Shop.
    private void HideMainUIForShop()
    {
        if (mainUIHiddenByShop) return;

        previousMainUIStates.Clear();
        GameObject[] objects = { shoppppp, settingBtn, gameModeBtn, characterView };

        foreach (GameObject obj in objects)
        {
            if (obj == null || previousMainUIStates.ContainsKey(obj))
                continue;

            // ShopBtn is managed by OpenShop/CloseShop, not by the state snapshot.
            if (shopBtn != null && obj == shopBtn.gameObject)
                continue;

            // Never disable the Shop or its ancestors (including this manager).
            if (obj == shopPanel ||
                (shopPanel != null && shopPanel.transform.IsChildOf(obj.transform)) ||
                obj == gameObject || transform.IsChildOf(obj.transform))
            {
                Debug.LogWarning("[SHOP] Cannot hide " + obj.name +
                    " because it contains the Shop or ShopCategoryManager.", this);
                continue;
            }

            previousMainUIStates.Add(obj, obj.activeSelf);
            obj.SetActive(false);
        }

        mainUIHiddenByShop = true;
    }

    private void RestoreMainUIAfterShop()
    {
        if (!mainUIHiddenByShop) return;

        foreach (KeyValuePair<GameObject, bool> entry in previousMainUIStates)
        {
            if (entry.Key != null)
                entry.Key.SetActive(entry.Value);
        }

        previousMainUIStates.Clear();
        mainUIHiddenByShop = false;
    }

    public void OpenShop()
    {
        if (shopPanel == null)
            return;

        shopPanel.SetActive(true);
        // Luu trang thai UI truoc khi an ShopBtn.
        HideMainUIForShop();

        if (shopBtn != null)
            shopBtn.gameObject.SetActive(false);

        // Mo Shop voi bo trang bi dang mac, khong reset ve do goc.
        ApplySavedEquipment();

        currentCategory = null;
        showingPurchasedItems = false;

        HideAllPanels();
        ClearSelection();
        RefreshItemPrices();
        RefreshShopGold();
    }

    public void CloseShop()
    {
        // Huy do dang thu va chi giu trang bi da EQUIP tren C01.
        ApplySavedEquipment();

        HideAllPanels();
        ClearSelection();

        currentCategory = null;
        showingPurchasedItems = false;

        if (shopPanel != null)
            shopPanel.SetActive(false);

        // Khoi phuc UI truoc, roi bat ShopBtn CUOI CUNG
        // de khong bi RestoreMainUIAfterShop() tat lai.
        RestoreMainUIAfterShop();

        if (shopBtn != null)
            shopBtn.gameObject.SetActive(true);
    }

    public void ToggleShop()
    {
        if (IsShopOpen())
            CloseShop();
        else
            OpenShop();
    }

    public bool IsShopOpen()
    {
        return shopPanel != null &&
               shopPanel.activeInHierarchy;
    }

    // ==========================================
    // CATEGORY PANELS
    // ==========================================

    private void HideAllPanels()
    {
        if (helmetPanel != null)
            helmetPanel.SetActive(false);

        if (baloPanel != null)
            baloPanel.SetActive(false);

        if (armorPanel != null)
            armorPanel.SetActive(false);

        if (skinPanel != null)
            skinPanel.SetActive(false);

        if (purchasedItemsPanel != null)
            purchasedItemsPanel.SetActive(false);
    }

    private void ShowCategory(
        ShopCategory category,
        GameObject panel)
    {
        HideAllPanels();

        currentCategory = category;
        showingPurchasedItems = false;

        if (panel != null)
            panel.SetActive(true);

        ClearSelection();
        RefreshItemPrices();
        RefreshShopGold();

        Debug.Log("[SHOP] Open category: " + category);
    }

    public void OpenHelmet()
    {
        ShowCategory(ShopCategory.Helmet, helmetPanel);
    }

    public void OpenBalo()
    {
        ShowCategory(ShopCategory.Balo, baloPanel);
    }

    public void OpenArmor()
    {
        ShowCategory(ShopCategory.Armor, armorPanel);
    }

    public void OpenSkin()
    {
        ShowCategory(ShopCategory.Skin, skinPanel);
    }

    // ==========================================
    // PURCHASED ITEMS
    // ==========================================

    public void OpenPurchasedItems()
    {
        if (!IsShopOpen())
            return;

        HideAllPanels();

        currentCategory = null;
        showingPurchasedItems = true;

        ClearSelection();

        if (purchasedItemsPanel != null)
            purchasedItemsPanel.SetActive(true);

        RebuildPurchasedItems();
        RefreshGoldenBorders();
        RefreshShopGold();
    }

    private void RebuildPurchasedItems()
    {
        if (purchasedItemsContainer == null)
        {
            Debug.LogWarning(
                "[SHOP] Purchased Items Container is NULL!",
                this
            );
            return;
        }

        // Xoa cac Button clone cu
        for (int i = purchasedItemsContainer.childCount - 1;
             i >= 0;
             i--)
        {
            GameObject child =
                purchasedItemsContainer.GetChild(i).gameObject;

            child.SetActive(false);
            Destroy(child);
        }

        purchasedGoldenBorders.Clear();
        HashSet<string> shownIDs = new HashSet<string>();

        foreach (ShopItem item in shopItems)
        {
            if (item == null ||
                item.itemButton == null ||
                string.IsNullOrWhiteSpace(item.itemID) ||
                !IsOwned(item.itemID) ||
                !shownIDs.Add(item.itemID))
            {
                continue;
            }

            Button copy = Instantiate(
                item.itemButton,
                purchasedItemsContainer
            );

            copy.gameObject.SetActive(true);
            copy.interactable = true;

            copy.onClick =
                new Button.ButtonClickedEvent();

            ShopItem captured = item;

            copy.onClick.AddListener(
                () => SelectItem(captured)
            );

            // Viền của Bought PHẢI nằm trong nút copy, không dùng viền gốc.
            // Tìm cả tên GoldenBorder (1), GoldenBorder(Clone), ...
            GameObject cloneBorder = FindGoldenBorderChild(copy.transform);

            // Nếu nút gốc không có viền nằm bên trong, tạo bản sao viền
            // được cấu hình từ Inspector rồi gắn vào nút Bought.
            if (cloneBorder == null)
            {
                GameObject sourceBorder = ResolveItemGoldenBorder(item);
                if (sourceBorder != null)
                {
                    cloneBorder = Instantiate(sourceBorder, copy.transform, false);
                    cloneBorder.name = "GoldenBorder";
                    cloneBorder.transform.SetAsLastSibling();
                }
            }

            ItemSelectionHighlight copyHighlight =
                copy.GetComponent<ItemSelectionHighlight>();

            if (cloneBorder != null)
            {
                // Không để script trong bản copy trỏ ngược đến viền gốc.
                if (copyHighlight != null)
                    copyHighlight.goldenBorder = cloneBorder;

                cloneBorder.transform.SetAsLastSibling();
                SetGoldenBorderVisible(cloneBorder, false);
                purchasedGoldenBorders[GoldenKey(item)] = cloneBorder;
            }
            else
            {
                Debug.LogWarning("[SHOP] Bought item '" + item.itemName +
                    "' chưa có GoldenBorder. Hãy thêm viền vào ItemButton gốc.", this);
            }

            // Dong bo ten va trang thai DA MUA tren chinh nut copy.
            // Khong sua vao TMP_Text cua nut goc.
            RefreshItemLabel(item, copy);

        }
        RefreshGoldenBorders();
    }

    // ==========================================
    // SELECT ITEM
    // ==========================================

    public void SelectItem(ShopItem item)
    {
        if (!IsShopOpen() || item == null)
            return;

        if (showingPurchasedItems)
        {
            if (!IsOwned(item.itemID))
                return;
        }
        else if (!currentCategory.HasValue ||
                 currentCategory.Value != item.category)
        {
            return;
        }

        // Bam lai dung vat pham CHUA MUA: bo chon va tat preview.
        // Khong anh huong du lieu mua hang hoac trang thai EQUIPPED.
        bool sameItemSelected = selectedItem != null &&
            selectedItem.category == item.category &&
            selectedItem.itemID == item.itemID;

        if (sameItemSelected && !IsOwned(item.itemID))
        {
            // Bo xem thu, mac lai do da EQUIP (khong xoa do dang mac).
            ApplySavedEquipment();

            ClearSelection();
            Debug.Log("[SHOP] Deselected unowned item: " + item.itemName, this);
            return;
        }

        selectedItem = item;

        // ======================================
        // 3D PREVIEW
        // ======================================

        if (equipmentPreview == null)
        {
            Debug.LogError(
                "[SHOP] Equipment Preview is NULL!",
                this
            );
        }
        else
        {
            // UU TIEN MODEL DA CO TREN C01.
            // Neu co Existing thi KHONG Instantiate Prefab.
            GameObject existing =
                item.existingEquipmentObject;

            GameObject prefab =
                existing != null
                    ? null
                    : item.equipmentPrefab;

            switch (item.category)
            {
                case ShopCategory.Helmet:
                    equipmentPreview.PreviewHelmet(
                        prefab,
                        existing
                    );
                    break;

                case ShopCategory.Balo:
                    equipmentPreview.PreviewBalo(
                        prefab,
                        existing
                    );
                    break;

                case ShopCategory.Armor:
                    equipmentPreview.PreviewArmor(
                        prefab,
                        existing
                    );
                    break;

                case ShopCategory.Skin:
                    equipmentPreview.PreviewSkin(
                        item.skinMaterial
                    );
                    break;
            }

            Debug.Log(
                "[SHOP] Selected: " + item.itemName +
                " | Category: " + item.category +
                " | Existing: " +
                (existing != null
                    ? existing.name : "None") +
                " | Prefab: " +
                (prefab != null
                    ? prefab.name : "None"),
                this
            );
        }

        HideNotification();
        RefreshSelectedItem();
        RefreshGoldenBorders();
    }

    public void SelectItemByID(string itemID)
    {
        ShopItem item = shopItems.Find(
            x => x != null && x.itemID == itemID
        );

        if (item != null)
            SelectItem(item);
        else
            Debug.LogWarning(
                "[SHOP] Item not found: " + itemID
            );
    }

    private void ClearSelection()
    {
        selectedItem = null;

        HideNotification();
        RefreshSelectedItem();
        RefreshGoldenBorders();
    }

    // ==========================================
    // GOLDEN BORDER - UI ITEM SELECTED
    // ==========================================

    private static string GoldenKey(ShopItem item)
    {
        return item.category + ":" + item.itemID;
    }

    private static GameObject FindGoldenBorderChild(Transform root)
    {
        if (root == null) return null;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child == root) continue;

            // Unity tự thêm (1), (2), (Clone) khi Duplicate/Instantiate.
            if (child.name.StartsWith("GoldenBorder", StringComparison.OrdinalIgnoreCase) ||
                child.name.StartsWith("Golden Border", StringComparison.OrdinalIgnoreCase))
                return child.gameObject;
        }
        return null;
    }

    // Tu tim GoldenBorder trong ItemButton, ke ca khi child dang inactive.
    // Uu tien child thuc te thay vi tham chieu Golden Border keo nham.
    private GameObject ResolveItemGoldenBorder(ShopItem item)
    {
        if (item == null || item.itemButton == null)
            return null;

        GameObject childBorder = FindGoldenBorderChild(item.itemButton.transform);
        if (childBorder != null)
        {
            item.goldenBorder = childBorder;
            return childBorder;
        }

        // Hỗ trợ kéo riêng trong Inspector hoặc từ ItemSelectionHighlight.
        if (item.goldenBorder != null) return item.goldenBorder;
        ItemSelectionHighlight highlight =
            item.itemButton.GetComponent<ItemSelectionHighlight>();
        return highlight != null ? highlight.goldenBorder : null;
    }

    private void RefreshGoldenBorders()
    {
        if (shopItems == null) return;

        foreach (ShopItem item in shopItems)
        {
            if (item == null) continue;

            bool isSelected = selectedItem != null &&
                item.category == selectedItem.category &&
                item.itemID == selectedItem.itemID;

            bool selectOriginal = isSelected && !showingPurchasedItems;
            bool selectBought = isSelected && showingPurchasedItems;

            // Tren nut vat pham goc: neu co ItemSelectionHighlight thi
            // de script nay quan ly bat/tat va glow cua chinh no.
            ItemSelectionHighlight highlight = item.itemButton != null
                ? item.itemButton.GetComponent<ItemSelectionHighlight>()
                : null;

            GameObject originalBorder = ResolveItemGoldenBorder(item);
            if (highlight != null)
            {
                // Neu field tren highlight chua gan, lien ket tu dong.
                if (highlight.goldenBorder == null)
                    highlight.goldenBorder = originalBorder;
                highlight.SetSelected(selectOriginal);
            }
            else if (originalBorder != null)
            {
                SetGoldenBorderVisible(originalBorder, selectOriginal);
            }

            if (isSelected && !showingPurchasedItems && originalBorder == null)
                Debug.LogWarning("[SHOP] Khong tim thay GoldenBorder trong " +
                    (item.itemButton != null ? item.itemButton.name : item.itemName), this);

            // Nut copy trong Bought: su dung khung thuoc clone,
            // khong bat nham khung o panel danh muc.
            if (purchasedGoldenBorders.TryGetValue(GoldenKey(item), out GameObject cloneBorder)
                && cloneBorder != null)
            {
                ItemSelectionHighlight cloneHighlight =
                    cloneBorder.GetComponentInParent<ItemSelectionHighlight>(true);
                if (cloneHighlight != null)
                {
                    if (cloneHighlight.goldenBorder == null ||
                        cloneHighlight.goldenBorder != cloneBorder)
                        cloneHighlight.goldenBorder = cloneBorder;
                    cloneHighlight.SetSelected(selectBought);
                }
                else
                {
                    SetGoldenBorderVisible(cloneBorder, selectBought);
                }
            }
        }
    }

    private static void SetGoldenBorderVisible(GameObject border, bool show)
    {
        if (border == null) return;

        Image image = border.GetComponent<Image>();
        if (image != null)
            image.raycastTarget = false;

        CanvasGroup group = border.GetComponent<CanvasGroup>();
        if (group == null) group = border.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = 1f;
        border.SetActive(show);
    }

    private void UpdateGoldenBorderGlow()
    {
        if (!animateGoldenBorder || selectedItem == null) return;

        GameObject border = null;
        if (showingPurchasedItems)
            purchasedGoldenBorders.TryGetValue(GoldenKey(selectedItem), out border);
        else
            border = ResolveItemGoldenBorder(selectedItem);

        if (border == null || !border.activeInHierarchy) return;

        // ItemSelectionHighlight da tu dieu khien glow, khong ghi de alpha.
        ItemSelectionHighlight highlight =
            border.GetComponentInParent<ItemSelectionHighlight>(true);
        if (highlight != null) return;

        CanvasGroup group = border.GetComponent<CanvasGroup>();
        if (group != null)
        {
            float minAlpha = Mathf.Clamp01(goldenMinAlpha);
            group.alpha = Mathf.Lerp(minAlpha, 1f,
                0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * goldenGlowSpeed));
        }
    }

    // ==========================================
    // OWNED / EQUIPPED
    // ==========================================

    public bool IsOwned(string itemID)
    {
        return PlayerPrefs.GetInt(
            OwnedPrefix + itemID,
            0
        ) == 1;
    }

    public bool IsEquipped(ShopItem item)
    {
        if (item == null)
            return false;

        string equippedID = PlayerPrefs.GetString(
            EquipPrefix + item.category,
            ""
        );

        return IsOwned(item.itemID) &&
               equippedID == item.itemID;
    }

    // ==========================================
    // SELECTED ITEM UI
    // ==========================================

    private void RefreshPlayerNameText()
    {
        if (selectedItemText == null) return;

        string playerName = PlayerPrefs.GetString(PlayerNamePrefsKey, "").Trim();
        selectedItemText.text = string.IsNullOrEmpty(playerName)
            ? "Player"
            : playerName;
    }

    private void RefreshSelectedItem()
    {
        // This label is permanently reserved for the name saved in Settings,
        // even when the selected Shop item changes.
        RefreshPlayerNameText();

        if (selectedItem == null)
        {

            if (priceText != null)
                priceText.text = "";

            if (buyButtonText != null)
                buyButtonText.text = "SELECT ITEM";

            if (buyButton != null)
                buyButton.interactable = false;

            if (selectedItemImage != null)
                selectedItemImage.enabled = false;

            return;
        }

        if (selectedItemImage != null)
        {
            selectedItemImage.sprite =
                selectedItem.itemIcon;

            selectedItemImage.enabled =
                selectedItem.itemIcon != null;
        }

        bool owned = IsOwned(selectedItem.itemID);
        bool equipped = IsEquipped(selectedItem);

        if (priceText != null)
        {
            priceText.text = owned
                ? "OWNED"
                : selectedItem.price.ToString("N0") + " Gold";
        }

        if (buyButtonText != null)
        {
            if (equipped)
                buyButtonText.text = "UNEQUIP";
            else if (owned)
                buyButtonText.text = "EQUIP";
            else
                buyButtonText.text = "BUY";
        }

        if (buyButton != null)
            buyButton.interactable = true;
    }

    // ==========================================
    // BUY / EQUIP
    // ==========================================

    public void UseSelectedItem()
    {
        if (!IsShopOpen())
        {
            Debug.LogWarning(
                "[SHOP] Shop is closed!"
            );
            return;
        }

        if (selectedItem == null)
        {
            Debug.LogWarning("[SHOP] Please select an item first.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(selectedItem.itemID) ||
            selectedItem.price < 0)
        {
            Debug.LogWarning("[SHOP] Invalid item data.", this);
            return;
        }

        // Da mua: bam EQUIP de mac, bam UNEQUIP de thao.
        // Khong tru vang va khong xoa quyen so huu.
        if (IsOwned(selectedItem.itemID))
        {
            if (IsEquipped(selectedItem))
            {
                UnequipCategory(selectedItem.category);
                HideNotification();
            }
            else
            {
                PlayerPrefs.SetString(
                    EquipPrefix + selectedItem.category,
                    selectedItem.itemID
                );
                PlayerPrefs.Save();
                ApplySavedEquipment();
                HideNotification();
            }

            RefreshSelectedItem();
            return;
        }

        int currentGold = GoldWallet.GetGold();
        int price = selectedItem.price;

        if (currentGold < price)
        {
            ShowInsufficientGoldNotification(currentGold, price);

            RefreshShopGold();
            return;
        }

        if (!GoldWallet.SpendGold(price))
        {
            // Gold may have changed since the check above.
            // Only show a UI message when there is actually not enough Gold.
            int updatedGold = GoldWallet.GetGold();
            if (updatedGold < price)
                ShowInsufficientGoldNotification(updatedGold, price);
            else
                Debug.LogWarning("[SHOP] Payment failed (not due to insufficient Gold).", this);

            RefreshShopGold();
            return;
        }

        PlayerPrefs.SetInt(
            OwnedPrefix + selectedItem.itemID,
            1
        );

        PlayerPrefs.Save();

        HideNotification();

        RefreshShopGold();
        RefreshItemPrices(); // Gia tren o vua mua chuyen thanh DA MUA.
        RefreshSelectedItem();

        if (showingPurchasedItems)
            RebuildPurchasedItems();
    }

    // ==========================================
    // RESET PURCHASES ON NEW BUILD VERSION
    // ==========================================

    private void ResetShopForNewBuildVersion()
    {
        if (!resetPurchasesOnNewBuild)
            return;

        int targetVersion = Mathf.Max(1, shopDataVersion);
        int previousVersion = PlayerPrefs.GetInt(ShopVersionKey, 0);

        // Same version => preserve purchases on subsequent game launches.
        if (previousVersion == targetVersion)
            return;

        HashSet<string> removedIDs = new HashSet<string>();
        if (shopItems != null)
        {
            foreach (ShopItem item in shopItems)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.itemID))
                    continue;

                if (removedIDs.Add(item.itemID))
                    PlayerPrefs.DeleteKey(OwnedPrefix + item.itemID);
            }
        }

        // Explicitly unequip all categories so preview does not restore
        // existing Helmet/Balo/Armor objects on the C01 character.
        foreach (ShopCategory category in Enum.GetValues(typeof(ShopCategory)))
        {
            PlayerPrefs.SetString(EquipPrefix + category, UnequippedValue);
        }

        // Record version only after shop keys have been updated.
        PlayerPrefs.SetInt(ShopVersionKey, targetVersion);
        PlayerPrefs.Save();

        // Never touch Gold, player name, or unrelated PlayerPrefs here.
        Debug.Log("[SHOP] Build shop data version " + previousVersion +
                  " -> " + targetVersion + ". Purchases reset once.", this);
    }

    // ==========================================
    // RESET SHOP
    // ==========================================

    // Can reset trong Play Mode hoac tu Inspector (Edit Mode).
    // Chi xoa du lieu so huu + trang bi cua nhung item duoc khai bao trong Shop Items.
    // Khong xoa cac PlayerPrefs khac nhu ten Player, cai dat va Gold.
    [ContextMenu("RESET ALL SHOP PURCHASES (Keep Gold)")]
    public void ResetShopData()
    {
        HashSet<string> ids = new HashSet<string>();
        int removedCount = 0;

        if (shopItems != null)
        {
            foreach (ShopItem item in shopItems)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.itemID))
                    continue;

                string id = item.itemID;
                if (!ids.Add(id))
                    continue;

                string ownedKey = OwnedPrefix + id;
                if (PlayerPrefs.HasKey(ownedKey))
                {
                    PlayerPrefs.DeleteKey(ownedKey);
                    removedCount++;
                }
            }
        }

        // Ghi ro UNEQUIP cho moi danh muc, tranh ResetPreview bat lai
        // mu / balo / giap mac dinh khi nguoi choi chua mua gi.
        foreach (ShopCategory category in Enum.GetValues(typeof(ShopCategory)))
        {
            PlayerPrefs.SetString(EquipPrefix + category, UnequippedValue);
        }

        PlayerPrefs.Save();

        // Tuy chon, mac dinh la false de giu nguyen Gold.
        if (resetGoldWithShop)
            GoldWallet.SetGold(0);

        // Khong thao tac UI/3D preview khi Edit Mode, de tranh thay doi Scene.
        if (Application.isPlaying)
        {
            ApplySavedEquipment();
            ClearSelection();
            RefreshItemPrices();
            RefreshShopGold();

            if (showingPurchasedItems)
                RebuildPurchasedItems();

            HideNotification();
        }

        Debug.Log(
            "[SHOP RESET] Da xoa " + removedCount +
            " trang thai mua. Tat ca danh muc da UNEQUIP. " +
            (resetGoldWithShop ? "Gold = 0." : "Gold duoc giu nguyen."),
            this
        );
    }

    // ==========================================
    // ITEM PRICES
    // ==========================================

    // =====================================================
    // TEN VAT PHAM + GIA / DA MUA TREN TUNG O
    // =====================================================

    // Tim dung TMP_Text ben trong ItemButton, ke ca nut clone o Bought.
    // Neu Inspector da gan TMP_Text thi tra theo duong dan trong hierarchy.
    private static TMP_Text FindMatchingLabel(Button sourceButton,
        Button targetButton, TMP_Text sourceLabel)
    {
        if (targetButton == null || sourceLabel == null) return null;
        if (sourceButton == targetButton) return sourceLabel;
        if (sourceButton == null) return null;

        Transform root = sourceButton.transform;
        Transform cursor = sourceLabel.transform;
        List<string> parts = new List<string>();
        while (cursor != null && cursor != root)
        {
            parts.Insert(0, cursor.name);
            cursor = cursor.parent;
        }
        if (cursor != root) return null;

        Transform target = targetButton.transform;
        foreach (string part in parts)
        {
            target = target.Find(part);
            if (target == null) return null;
        }
        return target.GetComponent<TMP_Text>();
    }

    private static TMP_Text AutoFindNameLabel(Button button, TMP_Text priceLabel)
    {
        if (button == null) return null;
        TMP_Text[] labels = button.GetComponentsInChildren<TMP_Text>(true);
        TMP_Text fallback = null;
        foreach (TMP_Text label in labels)
        {
            if (label == null || label == priceLabel) continue;
            string name = label.name.ToLowerInvariant();
            if (name.Contains("gold") || name.Contains("price") ||
                name.Contains("cost") || name.Contains("coin")) continue;
            if (name.Contains("name") || name.Contains("itemtext") ||
                name.Contains("armortext") || name.Contains("helmettext") ||
                name.Contains("balotext") || name.Contains("skintext"))
                return label;
            if (name.Contains("text") && fallback == null) fallback = label;
        }
        return fallback;
    }

    private void RefreshItemLabel(ShopItem item, Button targetButton)
    {
        if (item == null || targetButton == null) return;

        TMP_Text priceLabel = FindMatchingLabel(
            item.itemButton, targetButton, item.itemGoldText);
        if (priceLabel == null)
        {
            // Truong hop chua keo Item Gold Text trong Inspector.
            foreach (TMP_Text label in targetButton.GetComponentsInChildren<TMP_Text>(true))
            {
                string n = label.name.ToLowerInvariant();
                if (n.Contains("gold") || n.Contains("price"))
                {
                    priceLabel = label;
                    break;
                }
            }
        }

        if (priceLabel != null)
            priceLabel.text = IsOwned(item.itemID)
                ? "ĐÃ MUA" : item.price.ToString("N0");

        TMP_Text nameLabel = FindMatchingLabel(
            item.itemButton, targetButton, item.itemNameText);
        if (nameLabel == null)
            nameLabel = AutoFindNameLabel(targetButton, priceLabel);

        if (nameLabel != null && !string.IsNullOrWhiteSpace(item.itemName))
            nameLabel.text = item.itemName;
    }

    public void RefreshItemPrices()
    {
        if (shopItems == null) return;
        foreach (ShopItem item in shopItems)
        {
            if (item == null || item.itemButton == null) continue;
            RefreshItemLabel(item, item.itemButton);
        }

        if (Application.isPlaying && selectedItem != null)
            RefreshSelectedItem();
    }

    public void SetItemPrice(
        string itemID,
        int newPrice)
    {
        if (string.IsNullOrWhiteSpace(itemID) ||
            newPrice < 0)
            return;

        ShopItem item = shopItems.Find(
            x => x != null && x.itemID == itemID
        );

        if (item == null)
        {
            Debug.LogWarning(
                "[SHOP] Item not found: " + itemID
            );
            return;
        }

        item.price = newPrice;
        RefreshItemPrices();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            RefreshItemPrices();
    }
#endif

    // ==========================================
    // GOLD UI
    // ==========================================

    public void RefreshShopGold()
    {
        if (goldUI != null)
            goldUI.RefreshGold();
    }

    public void OnShopOpened()
    {
        RefreshShopGold();
    }

    // ==========================================
    // UNEQUIP - THAO RIENG TUNG LOAI TRANG BI
    // ==========================================

    public void UnequipCategory(ShopCategory category)
    {
        // Luu ro trang thai khong mac; khong xoa Purchased Items.
        PlayerPrefs.SetString(EquipPrefix + category, UnequippedValue);
        PlayerPrefs.Save();
        ApplySavedEquipment();
        RefreshSelectedItem();
    }

    // An cac trang bi goc sau ResetPreview, neu danh muc da UNEQUIP.
    // Chon dung GameObject tren C01, khong tat C01 hay Attach Point.
    private void HideUnequippedCategoryObjects(ShopCategory category)
    {
        if (equipmentPreview == null) return;

        GameObject[] originals = null;
        Transform attachPoint = null;

        switch (category)
        {
            case ShopCategory.Helmet:
                originals = equipmentPreview.originalHelmets;
                attachPoint = equipmentPreview.helmetPoint;
                break;
            case ShopCategory.Balo:
                originals = equipmentPreview.originalBalos;
                attachPoint = equipmentPreview.baloPoint;
                break;
            case ShopCategory.Armor:
                originals = equipmentPreview.originalArmors;
                attachPoint = equipmentPreview.armorPoint;
                break;
            case ShopCategory.Skin:
                // ResetPreview() da khoi phuc Material goc.
                return;
        }

        if (originals != null)
            foreach (GameObject obj in originals)
                HideEquipmentObjectSafely(obj, attachPoint);

        // Bao gom ca cac Existing Equipment Object chua duoc cho vao Original Arrays.
        if (shopItems != null)
            foreach (ShopItem item in shopItems)
                if (item != null && item.category == category)
                    HideEquipmentObjectSafely(item.existingEquipmentObject, attachPoint);
    }

    private void HideEquipmentObjectSafely(GameObject obj, Transform attachPoint)
    {
        if (obj == null || equipmentPreview == null) return;

        Transform player = equipmentPreview.previewPlayer;
        Transform t = obj.transform;
        if (player == null || t == player || !t.IsChildOf(player)) return;
        if (t == attachPoint || (attachPoint != null && attachPoint.IsChildOf(t))) return;

        obj.SetActive(false);
    }

    // ==========================================
    // EQUIPPED ITEMS - KEEP ON MAIN C01
    // ==========================================

    /// <summary>
    /// Khoi phuc trang bi da EQUIP tu PlayerPrefs len C01.
    /// Goi khi vao Main, dong/mo Shop, huy mac thu va nhan EQUIP.
    /// </summary>
    public void ApplySavedEquipment()
    {
        if (equipmentPreview == null)
        {
            Debug.LogWarning("[SHOP] Equipment Preview is missing!", this);
            return;
        }

        // Xoa cac model mac thu va khoi phuc cac model goc.
        equipmentPreview.ResetPreview();

        if (shopItems == null)
            return;

        foreach (ShopCategory category in Enum.GetValues(typeof(ShopCategory)))
        {
            string equippedID = PlayerPrefs.GetString(
                EquipPrefix + category, "");

            if (string.IsNullOrEmpty(equippedID) || !IsOwned(equippedID))
                continue;

            ShopItem item = shopItems.Find(x =>
                x != null && x.category == category && x.itemID == equippedID);

            if (item == null)
            {
                Debug.LogWarning(
                    "[SHOP] Equipped item not configured in Shop Items: " + equippedID,
                    this);
                continue;
            }

            GameObject existing = item.existingEquipmentObject;
            GameObject prefab = existing != null ? null : item.equipmentPrefab;

            switch (category)
            {
                case ShopCategory.Helmet:
                    equipmentPreview.PreviewHelmet(prefab, existing);
                    break;
                case ShopCategory.Balo:
                    equipmentPreview.PreviewBalo(prefab, existing);
                    break;
                case ShopCategory.Armor:
                    equipmentPreview.PreviewArmor(prefab, existing);
                    break;
                case ShopCategory.Skin:
                    equipmentPreview.PreviewSkin(item.skinMaterial);
                    break;
            }
        }

        // ResetPreview() khoi phuc model goc, nen can an lai nhung
        // danh muc da duoc nguoi choi chon UNEQUIP.
        foreach (ShopCategory category in Enum.GetValues(typeof(ShopCategory)))
        {
            if (PlayerPrefs.GetString(EquipPrefix + category, "") == UnequippedValue)
                HideUnequippedCategoryObjects(category);
        }
    }

    // ==========================================
    // NOTIFICATION
    // ==========================================

    // NotificationText is exclusively for purchase attempts with insufficient Gold.
    private void ShowInsufficientGoldNotification(int availableGold, int requiredGold)
    {
        if (notificationText == null) return;

        HideNotification();
        notificationText.text =
            "Not enough Gold! (" + availableGold.ToString("N0") +
            "/" + requiredGold.ToString("N0") + ")";
        notificationText.gameObject.SetActive(true);

        insufficientGoldNotificationCoroutine =
            StartCoroutine(HideInsufficientGoldNotificationAfterDelay());
    }

    private IEnumerator HideInsufficientGoldNotificationAfterDelay()
    {
        yield return new WaitForSecondsRealtime(insufficientGoldNotificationSeconds);
        insufficientGoldNotificationCoroutine = null;

        if (notificationText != null)
        {
            notificationText.text = string.Empty;
            notificationText.gameObject.SetActive(false);
        }
    }

    private void HideNotification()
    {
        if (insufficientGoldNotificationCoroutine != null)
        {
            StopCoroutine(insufficientGoldNotificationCoroutine);
            insufficientGoldNotificationCoroutine = null;
        }

        if (notificationText != null)
        {
            notificationText.text = string.Empty;
            notificationText.gameObject.SetActive(false);
        }
    }
}
