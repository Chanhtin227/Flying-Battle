
using UnityEngine;
using TMPro;

public class MainGoldUI : MonoBehaviour
{
    // =====================================================
    // GOLD UI
    // =====================================================

    [Header("Gold Text - Main UI")]
    public TMP_Text mainGoldText;

    [Header("Gold Text - Shop UI")]
    public TMP_Text shopGoldText;

    [Header("Display")]
    public string prefix = "";

    private int lastGold = -1;

    // =====================================================
    // TEST GOLD SETTINGS
    // =====================================================

    [Header("Test Gold Settings")]
    [Min(0)]
    public int testGoldAmount = 100000;

    [Header("Reset Gold Settings")]
    public bool resetGoldOnStart = false;

    // =====================================================
    // BUILD TEST SETTINGS
    // =====================================================

    [Header("Gold Test - Build")]
    [Tooltip("Bat de cho phep test vang trong ban Build.")]
    public bool enableGoldTestInBuild = true;

    [Tooltip("Chi cho phep test khi giu Ctrl + Shift.")]
    public bool requireCtrlShift = true;

    [Header("Keyboard Shortcuts")]
    public KeyCode addGoldKey = KeyCode.F6;
    public KeyCode setGoldKey = KeyCode.F7;
    public KeyCode resetGoldKey = KeyCode.F8;

    // =====================================================
    // UNITY
    // =====================================================

    private void OnEnable()
    {
        GoldWallet.OnGoldChanged += RefreshGold;
        RefreshGold();
    }

    private void OnDisable()
    {
        GoldWallet.OnGoldChanged -= RefreshGold;
    }

    private void Start()
    {
        if (resetGoldOnStart)
        {
            GoldWallet.SetGold(0);
            Debug.Log("[GoldWallet] Gold reset on Start.");
        }

        RefreshGold();
    }

    private void Update()
    {
        int currentGold = GoldWallet.GetGold();

        if (currentGold != lastGold)
            RefreshGold();

        HandleBuildGoldTest();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            RefreshGold();
    }

    // =====================================================
    // BUILD GOLD TEST INPUT
    // =====================================================

    private void HandleBuildGoldTest()
    {
        if (!enableGoldTestInBuild)
            return;

        // Ho tro ca Legacy Input va New Input System
#if ENABLE_INPUT_SYSTEM

        var keyboard = UnityEngine.InputSystem.Keyboard.current;

        if (keyboard == null)
            return;

        bool ctrl =
            keyboard.leftCtrlKey.isPressed ||
            keyboard.rightCtrlKey.isPressed;

        bool shift =
            keyboard.leftShiftKey.isPressed ||
            keyboard.rightShiftKey.isPressed;

        if (requireCtrlShift && !(ctrl && shift))
            return;

        if (IsNewInputKeyPressed(keyboard, addGoldKey))
            AddTestGold();

        if (IsNewInputKeyPressed(keyboard, setGoldKey))
            SetTestGold();

        if (IsNewInputKeyPressed(keyboard, resetGoldKey))
            ResetGold();

#elif ENABLE_LEGACY_INPUT_MANAGER

        bool ctrl =
            Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);

        bool shift =
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);

        if (requireCtrlShift && !(ctrl && shift))
            return;

        if (Input.GetKeyDown(addGoldKey))
            AddTestGold();

        if (Input.GetKeyDown(setGoldKey))
            SetTestGold();

        if (Input.GetKeyDown(resetGoldKey))
            ResetGold();

#endif
    }

#if ENABLE_INPUT_SYSTEM

    private bool IsNewInputKeyPressed(
        UnityEngine.InputSystem.Keyboard keyboard,
        KeyCode key)
    {
        switch (key)
        {
            case KeyCode.F6:
                return keyboard.f6Key.wasPressedThisFrame;

            case KeyCode.F7:
                return keyboard.f7Key.wasPressedThisFrame;

            case KeyCode.F8:
                return keyboard.f8Key.wasPressedThisFrame;

            default:
                return false;
        }
    }

#endif

    // =====================================================
    // REFRESH GOLD
    // =====================================================

    public void RefreshGold()
    {
        int goldAmount = GoldWallet.GetGold();

        lastGold = goldAmount;

        string displayPrefix =
            prefix == "0" ? "" : prefix;

        string formattedGold =
            displayPrefix + goldAmount.ToString("N0");

        if (mainGoldText != null)
            mainGoldText.text = formattedGold;

        if (shopGoldText != null)
            shopGoldText.text = formattedGold;
    }

    // =====================================================
    // SET EXACT TEST GOLD
    // =====================================================

    [ContextMenu("TEST - Set Gold")]
    public void SetTestGold()
    {
        if (!Application.isPlaying)
            return;

        GoldWallet.SetGold(testGoldAmount);
        RefreshGold();

        Debug.Log(
            "[Gold Test] Set Gold = " +
            GoldWallet.GetGold()
        );
    }

    // =====================================================
    // ADD TEST GOLD
    // =====================================================

    [ContextMenu("TEST - Add Gold")]
    public void AddTestGold()
    {
        if (!Application.isPlaying)
            return;

        GoldWallet.AddGold(testGoldAmount);
        RefreshGold();

        Debug.Log(
            "[Gold Test] Current Gold = " +
            GoldWallet.GetGold()
        );
    }

    // =====================================================
    // RESET GOLD TO ZERO
    // =====================================================

    [ContextMenu("TEST - Reset Gold To Zero")]
    public void ResetGold()
    {
        if (!Application.isPlaying)
            return;

        GoldWallet.SetGold(0);
        RefreshGold();

        Debug.Log("[GoldWallet] Gold reset to 0");
    }

    // =====================================================
    // CHECK GOLD
    // =====================================================

    [ContextMenu("TEST - Check Gold")]
    public void CheckGold()
    {
        Debug.Log(
            "[GoldWallet] Real Gold = " +
            GoldWallet.GetGold()
        );
    }
}
