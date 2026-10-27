using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;

public class LobbyController : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Fusion Network Runner")]
    public NetworkRunner runnerPrefab;
    private NetworkRunner currentRunner;
    private NetworkSceneManagerDefault networkSceneManager;

    [Header("Room Data Prefab (Kéo thả Prefab RoomData vào đây)")]
    public NetworkPrefabRef roomDataPrefab;

    [Header("UI Canvas Elements")]
    public Button gameModeBtn;

    [Header("Panel Game Mode")]
    public GameObject panelGameMode;
    public Button btnBack;
    public Button btn1vs1;
    public Button btn2vs2;
    public TMP_InputField inputID;
    public TextMeshProUGUI errorText;

    [Header("Panel Room 1vs1")]
    public GameObject panelRoom1vs1;
    public TextMeshProUGUI roomID_1vs1;
    public TextMeshProUGUI namePlayer1_1v1;
    public TextMeshProUGUI namePlayer2_1v1;
    public Button startBtn_1vs1;
    public Button quitBtn_1vs1;

    [Header("Panel Room 2vs2")]
    public GameObject panelRoom2vs2;
    public TextMeshProUGUI roomID_2vs2;
    public TextMeshProUGUI namePlayer1_2v2;
    public TextMeshProUGUI namePlayer2_2v2;
    public TextMeshProUGUI namePlayer3_2v2;
    public TextMeshProUGUI namePlayer4_2v2;
    public Button startBtn_2vs2;
    public Button quitBtn_2vs2;

    [Header("Settings Panel")]
    public Button settingBtn;
    public Button closeSettingBtn;
    public GameObject panelSetting;
    public TMP_InputField settingNameInput;

    [Header("Settings - Default General Tab")]
    [Tooltip("Keo Panel General (Chung) ben trong PanelSetting vao day.")]
    public GameObject generalSettingsPanel;

    [Tooltip("Keo cac tab Settings khac (Audio, Graphics, Controls...) vao day. Khong keo panelSetting cha.")]
    public GameObject[] otherSettingsPanels;

    [Header("Settings - Auto Select Chung Button")]
    [Tooltip("Keo ChungBtn vao day. Neu de trong, script tu tim ChungBtn trong PanelSetting.")]
    public Button chungBtn;


    [Header("Transition Effect")]
    public Image fadeImage;

    [Header("Game Mode Animation")]
    [SerializeField] private float gameModeSlideDistance = 700f;
    [SerializeField] private float gameModeSlideDuration = 0.4f;

    private RectTransform btn1vs1Rect;
    private RectTransform btn2vs2Rect;

    private Vector2 btn1vs1OriginalPos;
    private Vector2 btn2vs2OriginalPos;

    private CanvasGroup btn1vs1CanvasGroup;
    private CanvasGroup btn2vs2CanvasGroup;

    private Coroutine gameModeAnimationCoroutine;
    private bool isGameModeAnimating = false;

    private int maxPlayers = 2;
    private Coroutine errorNotificationCoroutine;

    [Header("Waiting Text Animation")]
    [SerializeField] private float waitingTextSpeed = 0.35f;

    private float waitingTextTimer = 0f;
    private int waitingDotCount = 0;

    public Button shopBtn;

    [Header("Button Bounce Effect")]
    [SerializeField, Range(1.02f, 1.2f)] private float buttonBounceScale = 1.07f;
    [SerializeField, Min(0.03f)] private float buttonBounceDuration = 0.16f;

    private readonly Dictionary<Button, Coroutine> buttonBounceCoroutines = new Dictionary<Button, Coroutine>();
    private readonly Dictionary<Button, Vector3> buttonOriginalScales = new Dictionary<Button, Vector3>();

    private void RegisterButtonBounce(Button button)
    {
        if (button == null) return;

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect == null) return;

        if (!buttonOriginalScales.ContainsKey(button))
            buttonOriginalScales.Add(button, rect.localScale);

        button.onClick.AddListener(() => PlayButtonBounce(button));
    }

    private void PlayButtonBounce(Button button)
    {
        if (button == null) return;

        if (buttonBounceCoroutines.TryGetValue(button, out Coroutine running) && running != null)
            StopCoroutine(running);

        buttonBounceCoroutines[button] = StartCoroutine(ButtonBounceCoroutine(button));
    }

    private System.Collections.IEnumerator ButtonBounceCoroutine(Button button)
    {
        if (button == null) yield break;

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect == null) yield break;

        if (!buttonOriginalScales.TryGetValue(button, out Vector3 originalScale))
            originalScale = rect.localScale;

        float halfDuration = Mathf.Max(0.015f, buttonBounceDuration * 0.5f);
        Vector3 peakScale = originalScale * buttonBounceScale;
        float time = 0f;

        while (time < halfDuration)
        {
            if (rect == null) yield break;
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / halfDuration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            rect.localScale = Vector3.LerpUnclamped(originalScale, peakScale, t);
            yield return null;
        }

        time = 0f;
        while (time < halfDuration)
        {
            if (rect == null) yield break;
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / halfDuration);
            t = t * t * (3f - 2f * t);
            rect.localScale = Vector3.LerpUnclamped(peakScale, originalScale, t);
            yield return null;
        }

        if (rect != null) rect.localScale = originalScale;
        buttonBounceCoroutines.Remove(button);
    }

    private System.Collections.IEnumerator DelayAction(float delay, Action action)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, delay));
        action?.Invoke();
    }

    private void OpenSettingsPanel()
    {
        // Moi lan mo Settings: luon hien tab General (Chung) dau tien.
        if (panelSetting != null)
            panelSetting.SetActive(true);

        // Hien tab Chung va goi dung su kien cua nut ChungBtn,
        // de cac script Settings dang dung duoc cap nhat nhu khi bam tay.
        ShowGeneralSettingsTab();
        SelectChungButtonOnOpen();

        if (settingBtn != null) settingBtn.gameObject.SetActive(false);
        if (gameModeBtn != null) gameModeBtn.gameObject.SetActive(false);
        if (shopBtn != null) shopBtn.gameObject.SetActive(false);
    }

    private void SelectChungButtonOnOpen()
    {
        if (chungBtn == null && panelSetting != null)
        {
            // Scene hien tai co child ten "ChungBtn" trong PanelSetting.
            foreach (Button button in panelSetting.GetComponentsInChildren<Button>(true))
            {
                if (button != null &&
                    string.Equals(button.gameObject.name, "ChungBtn", StringComparison.OrdinalIgnoreCase))
                {
                    chungBtn = button;
                    break;
                }
            }
        }

        if (chungBtn != null)
        {
            // Goi OnClick cua ChungBtn: dung luong logic tab co san.
            chungBtn.onClick.Invoke();
        }
        else if (generalSettingsPanel == null)
        {
            Debug.LogWarning("[Settings] Khong tim thay ChungBtn. Gan ChungBtn hoac General Settings Panel trong Inspector.", this);
        }
    }

    public void ShowGeneralSettingsTab()
    {
        if (otherSettingsPanels != null)
        {
            foreach (GameObject tab in otherSettingsPanels)
            {
                if (tab != null && tab != panelSetting && tab != generalSettingsPanel)
                    tab.SetActive(false);
            }
        }

        if (generalSettingsPanel != null)
            generalSettingsPanel.SetActive(true);
        // Neu khong gan General Settings Panel, ChungBtn se xu ly viec hien tab.
    }

    private void Start()
    {
        if (fadeImage != null) fadeImage.gameObject.SetActive(false);
        ResetUI();
        btn1vs1Rect = btn1vs1.GetComponent<RectTransform>();
        btn2vs2Rect = btn2vs2.GetComponent<RectTransform>();

        btn1vs1OriginalPos = btn1vs1Rect.anchoredPosition;
        btn2vs2OriginalPos = btn2vs2Rect.anchoredPosition;

        btn1vs1CanvasGroup = btn1vs1.GetComponent<CanvasGroup>();
        if (btn1vs1CanvasGroup == null)
            btn1vs1CanvasGroup = btn1vs1.gameObject.AddComponent<CanvasGroup>();

        btn2vs2CanvasGroup = btn2vs2.GetComponent<CanvasGroup>();
        if (btn2vs2CanvasGroup == null)
            btn2vs2CanvasGroup = btn2vs2.gameObject.AddComponent<CanvasGroup>();

        btn1vs1CanvasGroup.alpha = 1f;
        btn2vs2CanvasGroup.alpha = 1f;

        if (settingNameInput != null)
        {
            settingNameInput.text = PlayerPrefs.GetString("MyPlayerName", "");
        }

        RegisterButtonBounce(settingBtn);
        RegisterButtonBounce(shopBtn);
        RegisterButtonBounce(gameModeBtn);
        RegisterButtonBounce(btnBack);
        RegisterButtonBounce(btn1vs1);
        RegisterButtonBounce(btn2vs2);
        RegisterButtonBounce(quitBtn_1vs1);
        RegisterButtonBounce(quitBtn_2vs2);
        RegisterButtonBounce(startBtn_1vs1);
        RegisterButtonBounce(startBtn_2vs2);

        gameModeBtn.onClick.AddListener(() => StartCoroutine(DelayAction(buttonBounceDuration, OpenGameModePanel)));
        btnBack.onClick.AddListener(CloseGameModePanel);

        settingBtn.onClick.AddListener(() => StartCoroutine(DelayAction(buttonBounceDuration, OpenSettingsPanel)));

        closeSettingBtn.onClick.AddListener(() =>
        {
            SavePlayerSettingName();
            panelSetting.SetActive(false);
            settingBtn.gameObject.SetActive(true);
            gameModeBtn.gameObject.SetActive(true);
            if (shopBtn != null) shopBtn.gameObject.SetActive(true);
        });

        btn1vs1.onClick.AddListener(() => CreateRoom(2, panelRoom1vs1, roomID_1vs1));
        btn2vs2.onClick.AddListener(() => CreateRoom(4, panelRoom2vs2, roomID_2vs2));

        inputID.onSubmit.AddListener((text) => JoinRoom());

        startBtn_1vs1.onClick.AddListener(StartMatch);
        startBtn_2vs2.onClick.AddListener(StartMatch);

        if (quitBtn_1vs1 != null) quitBtn_1vs1.onClick.AddListener(QuitRoomAndReturnToGameMode);
        if (quitBtn_2vs2 != null) quitBtn_2vs2.onClick.AddListener(QuitRoomAndReturnToGameMode);
    }

    private void Update()
    {
        UpdateRoomUI();
    }

    private void UpdateRoomUI()
    {
        if (RoomData.Instance == null) return;

        var syncNames = RoomData.Instance.SyncPlayerNames;

        waitingTextTimer += Time.unscaledDeltaTime;
        if (waitingTextTimer >= waitingTextSpeed)
        {
            waitingTextTimer = 0f;
            waitingDotCount++;
            if (waitingDotCount > 3) waitingDotCount = 0;
        }

        string waitingText = "Waiting" + new string('.', waitingDotCount);

        if (panelRoom1vs1.activeSelf)
        {
            namePlayer1_1v1.text = syncNames.ContainsKey(0) ? syncNames.Get(0).ToString() : waitingText;
            namePlayer2_1v1.text = syncNames.ContainsKey(1) ? syncNames.Get(1).ToString() : waitingText;
        }
        else if (panelRoom2vs2.activeSelf)
        {
            namePlayer1_2v2.text = syncNames.ContainsKey(0) ? syncNames.Get(0).ToString() : waitingText;
            namePlayer2_2v2.text = syncNames.ContainsKey(1) ? syncNames.Get(1).ToString() : waitingText;
            namePlayer3_2v2.text = syncNames.ContainsKey(2) ? syncNames.Get(2).ToString() : waitingText;
            namePlayer4_2v2.text = syncNames.ContainsKey(3) ? syncNames.Get(3).ToString() : waitingText;
        }
    }

    private void SavePlayerSettingName()
    {
        if (settingNameInput != null && !string.IsNullOrEmpty(settingNameInput.text))
        {
            PlayerPrefs.SetString("MyPlayerName", settingNameInput.text.Trim());
            PlayerPrefs.Save();
            Debug.Log("[Settings] Đã lưu tên: " + settingNameInput.text.Trim());
        }
    }

    private void OpenGameModePanel()
    {
        if (isGameModeAnimating) return;

        panelGameMode.SetActive(true);
        shopBtn.gameObject.SetActive(false);
        gameModeBtn.gameObject.SetActive(false);
        settingBtn.gameObject.SetActive(false);

        btn1vs1Rect.anchoredPosition = btn1vs1OriginalPos + Vector2.left * gameModeSlideDistance;
        btn2vs2Rect.anchoredPosition = btn2vs2OriginalPos + Vector2.right * gameModeSlideDistance;

        btn1vs1CanvasGroup.alpha = 1f;
        btn2vs2CanvasGroup.alpha = 1f;

        gameModeAnimationCoroutine = StartCoroutine(AnimateGameModeButtonsOpen());
    }

    private System.Collections.IEnumerator AnimateGameModeButtonsOpen()
    {
        isGameModeAnimating = true;
        btn1vs1.interactable = false;
        btn2vs2.interactable = false;

        Vector2 startPos1 = btn1vs1OriginalPos + Vector2.left * gameModeSlideDistance;
        Vector2 startPos2 = btn2vs2OriginalPos + Vector2.right * gameModeSlideDistance;

        float time = 0f;
        while (time < gameModeSlideDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / gameModeSlideDuration);
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);

            btn1vs1Rect.anchoredPosition = Vector2.LerpUnclamped(startPos1, btn1vs1OriginalPos, smoothT);
            btn2vs2Rect.anchoredPosition = Vector2.LerpUnclamped(startPos2, btn2vs2OriginalPos, smoothT);

            yield return null;
        }

        btn1vs1Rect.anchoredPosition = btn1vs1OriginalPos;
        btn2vs2Rect.anchoredPosition = btn2vs2OriginalPos;

        btn1vs1.interactable = true;
        btn2vs2.interactable = true;

        isGameModeAnimating = false;
        gameModeAnimationCoroutine = null;
    }

    private void CloseGameModePanel()
    {
        if (isGameModeAnimating) return;
        gameModeAnimationCoroutine = StartCoroutine(AnimateGameModeButtonsClose());
    }

    private System.Collections.IEnumerator AnimateGameModeButtonsClose()
    {
        isGameModeAnimating = true;
        btn1vs1.interactable = false;
        btn2vs2.interactable = false;

        Vector2 startPos1 = btn1vs1Rect.anchoredPosition;
        Vector2 startPos2 = btn2vs2Rect.anchoredPosition;

        Vector2 targetPos1 = btn1vs1OriginalPos + Vector2.left * gameModeSlideDistance;
        Vector2 targetPos2 = btn2vs2OriginalPos + Vector2.right * gameModeSlideDistance;

        float startAlpha1 = btn1vs1CanvasGroup.alpha;
        float startAlpha2 = btn2vs2CanvasGroup.alpha;

        float time = 0f;
        while (time < gameModeSlideDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / gameModeSlideDuration);
            float smoothT = t * t * t;

            btn1vs1Rect.anchoredPosition = Vector2.LerpUnclamped(startPos1, targetPos1, smoothT);
            btn2vs2Rect.anchoredPosition = Vector2.LerpUnclamped(startPos2, targetPos2, smoothT);

            btn1vs1CanvasGroup.alpha = Mathf.Lerp(startAlpha1, 0f, t);
            btn2vs2CanvasGroup.alpha = Mathf.Lerp(startAlpha2, 0f, t);

            yield return null;
        }

        btn1vs1Rect.anchoredPosition = targetPos1;
        btn2vs2Rect.anchoredPosition = targetPos2;

        btn1vs1CanvasGroup.alpha = 0f;
        btn2vs2CanvasGroup.alpha = 0f;

        panelGameMode.SetActive(false);

        btn1vs1Rect.anchoredPosition = btn1vs1OriginalPos;
        btn2vs2Rect.anchoredPosition = btn2vs2OriginalPos;

        btn1vs1CanvasGroup.alpha = 1f;
        btn2vs2CanvasGroup.alpha = 1f;

        btn1vs1.interactable = true;
        btn2vs2.interactable = true;

        gameModeBtn.gameObject.SetActive(true);
        settingBtn.gameObject.SetActive(true);
        if (shopBtn != null) shopBtn.gameObject.SetActive(true);

        isGameModeAnimating = false;
        gameModeAnimationCoroutine = null;
    }

    private void ResetUI()
    {
        shopBtn.gameObject.SetActive(true);
        gameModeBtn.gameObject.SetActive(true);
        settingBtn.gameObject.SetActive(true);
        panelGameMode.SetActive(false);
        panelRoom1vs1.SetActive(false);
        panelRoom2vs2.SetActive(false);
        startBtn_1vs1.gameObject.SetActive(false);
        startBtn_2vs2.gameObject.SetActive(false);
        panelSetting.SetActive(false);
    }

    private async void CreateRoom(int players, GameObject roomPanelToOpen, TextMeshProUGUI roomIDText)
    {
        maxPlayers = players;
        string randomID = UnityEngine.Random.Range(1000, 10000).ToString();
        await Fade(1f, 0.5f);
        panelGameMode.SetActive(false);
        roomIDText.text = "ID: " + randomID;
        Debug.Log($"[Lobby] Create Room\nRoom ID: {randomID}\nMax Players: {maxPlayers}");
        StartSession(GameMode.AutoHostOrClient, randomID);
    }

    private async void JoinRoom()
    {
        string roomID = inputID.text.Trim();
        if (string.IsNullOrEmpty(roomID) || roomID.Length != 4)
        {
            Debug.LogWarning("[Lobby] ID phòng không hợp lệ!");
            ShowErrorNotification();
            return;
        }

        HideErrorNotification();
        Debug.Log($"[Lobby] Joining Room: {roomID}");
        await Fade(1f, 0.5f);
        panelGameMode.SetActive(false);
        StartSession(GameMode.AutoHostOrClient, roomID);
    }

    private async void StartSession(GameMode mode, string sessionName)
    {
        if (currentRunner == null)
        {
            currentRunner = Instantiate(runnerPrefab);
            currentRunner.AddCallbacks(this);
            networkSceneManager = currentRunner.gameObject.AddComponent<NetworkSceneManagerDefault>();
        }

        currentRunner.ProvideInput = true;
        GameInputManager.EnsureForRunner(currentRunner);

        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = sessionName,
            SceneManager = networkSceneManager
        };

        // Nếu người chơi nhấn Create, ưu tiên cấp số lượng max player cho phòng mới tạo
        if (maxPlayers > 0)
        {
            startGameArgs.PlayerCount = maxPlayers;
        }

        var result = await currentRunner.StartGame(startGameArgs);

        if (result.Ok)
        {
            Debug.Log($"[Fusion] StartGame SUCCESS\nSession: {sessionName}\nMode: {mode}");
            await System.Threading.Tasks.Task.Delay(1000);

            if (currentRunner.IsServer)
            {
                if (roomDataPrefab != null)
                {
                    currentRunner.Spawn(roomDataPrefab);
                }
                startBtn_1vs1.gameObject.SetActive(maxPlayers == 2);
                startBtn_2vs2.gameObject.SetActive(maxPlayers == 4);
                if (maxPlayers == 2) panelRoom1vs1.SetActive(true);
                else if (maxPlayers == 4) panelRoom2vs2.SetActive(true);
            }
            else
            {
                startBtn_1vs1.gameObject.SetActive(false);
                startBtn_2vs2.gameObject.SetActive(false);

                int roomMaxPlayers = currentRunner.SessionInfo.MaxPlayers;
                if (roomMaxPlayers == 2)
                {
                    panelRoom1vs1.SetActive(true);
                    roomID_1vs1.text = "ID: " + sessionName;
                }
                else if (roomMaxPlayers == 4)
                {
                    panelRoom2vs2.SetActive(true);
                    roomID_2vs2.text = "ID: " + sessionName;
                }
            }
            await Fade(0f, 0.5f);
        }
        else
        {
            Debug.LogError($"[Fusion] StartGame FAILED\nSession: {sessionName}\nMode: {mode}\nReason: {result.ShutdownReason}");
            if (mode == GameMode.Client || mode == GameMode.AutoHostOrClient)
            {
                panelGameMode.SetActive(true);
                await Fade(0f, 0.5f);
                ShowErrorNotification();
            }
        }
    }

    private async void QuitRoomAndReturnToGameMode()
    {
        if (isGameModeAnimating) return;

        HideErrorNotification();
        await Fade(1f, 0.35f);

        if (panelRoom1vs1 != null) panelRoom1vs1.SetActive(false);
        if (panelRoom2vs2 != null) panelRoom2vs2.SetActive(false);
        if (startBtn_1vs1 != null) startBtn_1vs1.gameObject.SetActive(false);
        if (startBtn_2vs2 != null) startBtn_2vs2.gameObject.SetActive(false);

        // [FIX] Cắt đứt liên kết Runner ngay lập tức để không dùng lại Runner đang bị lỗi
        NetworkRunner runnerToShutdown = currentRunner;
        currentRunner = null;
        networkSceneManager = null;

        if (runnerToShutdown != null)
        {
            try
            {
                await runnerToShutdown.Shutdown();
                Destroy(runnerToShutdown.gameObject); // Phá hủy hoàn toàn object cũ
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Lobby] Lỗi khi thoát phòng Fusion: " + e.Message);
            }
        }

        if (panelGameMode != null) panelGameMode.SetActive(false);
        await Fade(0f, 0.35f);

        OpenGameModePanel();
        Debug.Log("[Lobby] Đã thoát phòng và dọn dẹp hệ thống mạng.");
    }

    private void StartMatch()
    {
        if (currentRunner == null)
        {
            Debug.LogError("[Lobby] NetworkRunner chưa tồn tại!");
            return;
        }

        if (!currentRunner.IsServer)
        {
            Debug.LogWarning("[Lobby] Chỉ Host mới được Start Match!");
            return;
        }

        int currentPlayers = currentRunner.SessionInfo.PlayerCount;

        if (currentPlayers < maxPlayers)
        {
            Debug.LogWarning($"[Lobby] Chưa đủ người chơi!\nCurrent: {currentPlayers}\nRequired: {maxPlayers}");
            ShowErrorNotification("Not enough players, please wait...");
            return;
        }

        Debug.Log("[Lobby] Đủ người chơi. Starting Match...");

        if (currentRunner.SessionInfo.IsValid)
        {
            currentRunner.SessionInfo.IsOpen = false;
            Debug.Log("[Lobby] ROOM LOCKED BEFORE MATCH | ID = " + currentRunner.SessionInfo.Name);
        }

        currentRunner.LoadScene(SceneRef.FromIndex(3));
    }

    private void ShowErrorNotification(string message = "Room ID not correct! Again please")
    {
        HideErrorNotification();
        errorNotificationCoroutine = StartCoroutine(ShowErrorNotificationCoroutine(message));
    }

    private void HideErrorNotification()
    {
        if (errorNotificationCoroutine != null)
        {
            StopCoroutine(errorNotificationCoroutine);
            errorNotificationCoroutine = null;
        }

        if (errorText != null) errorText.gameObject.SetActive(false);
    }

    private System.Collections.IEnumerator ShowErrorNotificationCoroutine(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);
            yield return new WaitForSeconds(1.5f);
            errorText.gameObject.SetActive(false);
        }
        errorNotificationCoroutine = null;
    }

    private async System.Threading.Tasks.Task Fade(float targetAlpha, float duration)
    {
        if (fadeImage == null) return;

        fadeImage.gameObject.SetActive(true);

        float startAlpha = fadeImage.color.a;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            float a = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            fadeImage.color = new Color(0, 0, 0, a);
            await System.Threading.Tasks.Task.Yield();
        }

        fadeImage.color = new Color(0, 0, 0, targetAlpha);

        if (targetAlpha == 0)
        {
            fadeImage.gameObject.SetActive(false);
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;
        if (!runner.SessionInfo.IsValid) return;

        // [FIX] Đếm người chơi thực tế đang kết nối thay vì dùng SessionInfo.PlayerCount (dễ bị delay)
        int currentPlayers = 0;
        foreach (var p in runner.ActivePlayers) currentPlayers++;

        int roomMaxPlayers = runner.SessionInfo.MaxPlayers;

        Debug.Log($"[Lobby] Player Joined | Current = {currentPlayers} | Max = {roomMaxPlayers}");

        // Khóa phòng và ẩn phòng đi khi đã đủ người
        if (currentPlayers >= roomMaxPlayers)
        {
            runner.SessionInfo.IsOpen = false;
            runner.SessionInfo.IsVisible = false;
            Debug.Log($"[Lobby] ROOM FULL - LOCKED | Room ID = {runner.SessionInfo.Name}");
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // [FIX] Chỉ cần có người thoát ra (mà trận đấu chưa bắt đầu), Host sẽ luôn mở khóa lại phòng
        if (runner.IsServer && runner.SessionInfo.IsValid)
        {
            runner.SessionInfo.IsOpen = true;
            runner.SessionInfo.IsVisible = true;
            Debug.Log($"[Lobby] Người chơi {player} đã thoát. Mở khóa lại phòng (IsOpen = true).");
        }
    }

    public async void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log("[Lobby] Host đã thoát. Đang tiến hành Host Migration...");

        NetworkRunner oldRunner = runner;
        if (currentRunner == oldRunner)
        {
            currentRunner = null;
            networkSceneManager = null;
        }

        await oldRunner.Shutdown();

        currentRunner = Instantiate(runnerPrefab);
        currentRunner.AddCallbacks(this);
        networkSceneManager = currentRunner.gameObject.AddComponent<NetworkSceneManagerDefault>();

        var startGameArgs = new StartGameArgs()
        {
            HostMigrationToken = hostMigrationToken,
            SceneManager = networkSceneManager
        };

        Debug.Log("[Lobby] Đang khởi động lại Server với quyền Host mới...");
        var result = await currentRunner.StartGame(startGameArgs);

        if (result.Ok)
        {
            Debug.Log("[Lobby] Chuyển quyền Host THÀNH CÔNG!");
            if (currentRunner.IsServer)
            {
                // [FIX] Mở khóa phòng lại vì phòng có thể đã bị khóa từ trước khi Host cũ out
                if (currentRunner.SessionInfo.IsValid)
                {
                    currentRunner.SessionInfo.IsOpen = true;
                    currentRunner.SessionInfo.IsVisible = true;
                    Debug.Log("[Lobby] Host mới đã mở lại phòng cho người khác tham gia.");
                }

                startBtn_1vs1.gameObject.SetActive(maxPlayers == 2);
                startBtn_2vs2.gameObject.SetActive(maxPlayers == 4);

                if (roomDataPrefab != null)
                {
                    currentRunner.Spawn(roomDataPrefab);
                }
            }
        }
        else
        {
            Debug.LogWarning("[Lobby] Chuyển Host thất bại. Đưa người chơi về sảnh.");
            QuitRoomAndReturnToGameMode();
        }
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning("[Lobby] Mất kết nối từ Server: " + reason);
        // [QUAN TRỌNG] Phải để trống phần này! 
        // Không gọi QuitRoomAndReturnToGameMode() ở đây. Nếu Host thoát, Fusion sẽ xử lý ở OnHostMigration.
        // Nếu lỡ gọi Quit ở đây, Client sẽ tự thoát game trước khi kịp làm Host.
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[Lobby] OnShutdown called. Reason: {shutdownReason}");

        // Chỉ dọn dẹp UI và văng ra sảnh nếu Runner bị sập chính là phiên đang chơi hiện tại
        // (Điều kiện này bỏ qua Runner cũ đang trong quá trình chuyển giao Host Migration)
        if (runner == currentRunner)
        {
            runner.RemoveCallbacks(this);
            Destroy(runner.gameObject);
            currentRunner = null;
            networkSceneManager = null;

            // Xóa UI phòng cũ nếu có lỗi sập thật sự
            if ((panelRoom1vs1 != null && panelRoom1vs1.activeSelf) ||
                (panelRoom2vs2 != null && panelRoom2vs2.activeSelf))
            {
                if (panelRoom1vs1 != null) panelRoom1vs1.SetActive(false);
                if (panelRoom2vs2 != null) panelRoom2vs2.SetActive(false);
                if (startBtn_1vs1 != null) startBtn_1vs1.gameObject.SetActive(false);
                if (startBtn_2vs2 != null) startBtn_2vs2.gameObject.SetActive(false);

                if (panelGameMode != null && !panelGameMode.activeSelf)
                {
                    OpenGameModePanel();
                }
            }
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }


    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
#pragma warning disable CS0618
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
#pragma warning restore CS0618
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ReadOnlySpan<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}