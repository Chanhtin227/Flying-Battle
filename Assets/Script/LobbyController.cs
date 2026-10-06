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

    [Header("Panel Room 2vs2")]
    public GameObject panelRoom2vs2;
    public TextMeshProUGUI roomID_2vs2;
    public TextMeshProUGUI namePlayer1_2v2;
    public TextMeshProUGUI namePlayer2_2v2;
    public TextMeshProUGUI namePlayer3_2v2;
    public TextMeshProUGUI namePlayer4_2v2;
    public Button startBtn_2vs2;

    [Header("Settings Panel")]
    public Button settingBtn;
    public Button closeSettingBtn;
    public GameObject panelSetting;
    public TMP_InputField settingNameInput; 

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
        // Tải lại tên đã lưu trước đó lên ô InputField trong cài đặt (nếu có)
        if (settingNameInput != null)
        {
            settingNameInput.text = PlayerPrefs.GetString("MyPlayerName", "");
        }

        gameModeBtn.onClick.AddListener(OpenGameModePanel);
        btnBack.onClick.AddListener(CloseGameModePanel);

        settingBtn.onClick.AddListener(() => 
        {
            panelSetting.SetActive(true);
            settingBtn.gameObject.SetActive(false);
            gameModeBtn.gameObject.SetActive(false);
        });
        
        closeSettingBtn.onClick.AddListener(() => 
        {
            SavePlayerSettingName();
            panelSetting.SetActive(false);
            settingBtn.gameObject.SetActive(true);
            gameModeBtn.gameObject.SetActive(true);
        });

        btn1vs1.onClick.AddListener(() =>
        {
            CreateRoom(2, panelRoom1vs1, roomID_1vs1);
        });

        btn2vs2.onClick.AddListener(() =>
        {
            CreateRoom(4, panelRoom2vs2, roomID_2vs2);
        });

        inputID.onSubmit.AddListener((text) =>
        {
            JoinRoom();
        });

        // Bất kể nút Start của 1vs1 hay 2vs2 đều gọi chung hàm StartMatch
        startBtn_1vs1.onClick.AddListener(StartMatch);
        startBtn_2vs2.onClick.AddListener(StartMatch);
    }

    private void Update()
    {
        UpdateRoomUI();
    }

    private void UpdateRoomUI()
    {
        if (RoomData.Instance == null) return;

        var syncNames = RoomData.Instance.SyncPlayerNames;

        if (panelRoom1vs1.activeSelf)
        {
            namePlayer1_1v1.text = syncNames.ContainsKey(0) ? syncNames.Get(0).ToString() : "Waiting...";
            namePlayer2_1v1.text = syncNames.ContainsKey(1) ? syncNames.Get(1).ToString() : "Waiting...";
        }
        else if (panelRoom2vs2.activeSelf)
        {
            namePlayer1_2v2.text = syncNames.ContainsKey(0) ? syncNames.Get(0).ToString() : "Waiting...";
            namePlayer2_2v2.text = syncNames.ContainsKey(1) ? syncNames.Get(1).ToString() : "Waiting...";
            namePlayer3_2v2.text = syncNames.ContainsKey(2) ? syncNames.Get(2).ToString() : "Waiting...";
            namePlayer4_2v2.text = syncNames.ContainsKey(3) ? syncNames.Get(3).ToString() : "Waiting...";
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
        if (isGameModeAnimating)
            return;

        panelGameMode.SetActive(true);

        gameModeBtn.gameObject.SetActive(false);
        settingBtn.gameObject.SetActive(false);

        // Đặt button ra ngoài 2 phía
        btn1vs1Rect.anchoredPosition =
            btn1vs1OriginalPos + Vector2.left * gameModeSlideDistance;

        btn2vs2Rect.anchoredPosition =
            btn2vs2OriginalPos + Vector2.right * gameModeSlideDistance;

        // Khi mở panel thì hiện đầy đủ
        btn1vs1CanvasGroup.alpha = 1f;
        btn2vs2CanvasGroup.alpha = 1f;

        gameModeAnimationCoroutine =
            StartCoroutine(AnimateGameModeButtonsOpen());
    }

    private System.Collections.IEnumerator AnimateGameModeButtonsOpen()
    {
        isGameModeAnimating = true;

        btn1vs1.interactable = false;
        btn2vs2.interactable = false;

        Vector2 startPos1 =
            btn1vs1OriginalPos + Vector2.left * gameModeSlideDistance;

        Vector2 startPos2 =
            btn2vs2OriginalPos + Vector2.right * gameModeSlideDistance;

        float time = 0f;

        while (time < gameModeSlideDuration)
        {
            time += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(time / gameModeSlideDuration);

            // Ease Out Cubic
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);

            btn1vs1Rect.anchoredPosition =
                Vector2.LerpUnclamped(
                    startPos1,
                    btn1vs1OriginalPos,
                    smoothT
                );

            btn2vs2Rect.anchoredPosition =
                Vector2.LerpUnclamped(
                    startPos2,
                    btn2vs2OriginalPos,
                    smoothT
                );

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
        if (isGameModeAnimating)
            return;

        gameModeAnimationCoroutine =
            StartCoroutine(AnimateGameModeButtonsClose());
    }

    private System.Collections.IEnumerator AnimateGameModeButtonsClose()
    {
        isGameModeAnimating = true;

        btn1vs1.interactable = false;
        btn2vs2.interactable = false;

        // Vị trí hiện tại
        Vector2 startPos1 = btn1vs1Rect.anchoredPosition;
        Vector2 startPos2 = btn2vs2Rect.anchoredPosition;

        // Vị trí sẽ bay ra ngoài
        Vector2 targetPos1 =
            btn1vs1OriginalPos + Vector2.left * gameModeSlideDistance;

        Vector2 targetPos2 =
            btn2vs2OriginalPos + Vector2.right * gameModeSlideDistance;

        float startAlpha1 = btn1vs1CanvasGroup.alpha;
        float startAlpha2 = btn2vs2CanvasGroup.alpha;

        float time = 0f;

        while (time < gameModeSlideDuration)
        {
            time += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(time / gameModeSlideDuration);

            // Ease In Cubic:
            // ban đầu đi chậm, sau đó nhanh dần ra ngoài
            float smoothT = t * t * t;

            // =============================
            // Di chuyển ra 2 cạnh
            // =============================
            btn1vs1Rect.anchoredPosition =
                Vector2.LerpUnclamped(
                    startPos1,
                    targetPos1,
                    smoothT
                );

            btn2vs2Rect.anchoredPosition =
                Vector2.LerpUnclamped(
                    startPos2,
                    targetPos2,
                    smoothT
                );

            // =============================
            // Mờ dần
            // =============================
            btn1vs1CanvasGroup.alpha =
                Mathf.Lerp(startAlpha1, 0f, t);

            btn2vs2CanvasGroup.alpha =
                Mathf.Lerp(startAlpha2, 0f, t);

            yield return null;
        }

        // Đảm bảo kết thúc chính xác
        btn1vs1Rect.anchoredPosition = targetPos1;
        btn2vs2Rect.anchoredPosition = targetPos2;

        btn1vs1CanvasGroup.alpha = 0f;
        btn2vs2CanvasGroup.alpha = 0f;

        // Sau khi animation xong mới đóng panel
        panelGameMode.SetActive(false);

        // =============================
        // RESET để lần mở sau hoạt động bình thường
        // =============================

        btn1vs1Rect.anchoredPosition = btn1vs1OriginalPos;
        btn2vs2Rect.anchoredPosition = btn2vs2OriginalPos;

        btn1vs1CanvasGroup.alpha = 1f;
        btn2vs2CanvasGroup.alpha = 1f;

        btn1vs1.interactable = true;
        btn2vs2.interactable = true;

        gameModeBtn.gameObject.SetActive(true);
        settingBtn.gameObject.SetActive(true);

        isGameModeAnimating = false;
        gameModeAnimationCoroutine = null;
    }

    private void ResetUI()
    {
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
        StartSession(GameMode.Host, randomID);
    }

    private async void JoinRoom()
    {
        string roomID = inputID.text.Trim();
        if (string.IsNullOrEmpty(roomID) || roomID.Length != 4)
        {
            Debug.LogWarning("[Lobby] ID phòng không hợp lệ!");
            ShowErrorNotification(); // Sẽ dùng câu thông báo mặc định
            return;
        }

        HideErrorNotification();
        Debug.Log($"[Lobby] Joining Room: {roomID}");
        await Fade(1f, 0.5f);
        panelGameMode.SetActive(false);
        StartSession(GameMode.Client, roomID);
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
            GameMode = mode,
            SessionName = sessionName,
            SceneManager = networkSceneManager
        };

        if (mode == GameMode.Host)
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
            if (mode == GameMode.Client)
            {
                panelGameMode.SetActive(true);
                await Fade(0f, 0.5f);
                ShowErrorNotification(); // Thông báo mặc định
            }
        }
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

        // KIỂM TRA SỐ LƯỢNG NGƯỜI CHƠI (Tự động áp dụng cho 1v1 hoặc 2v2 dựa trên maxPlayers)
        if (currentPlayers < maxPlayers)
        {
            Debug.LogWarning($"[Lobby] Chưa đủ người chơi!\nCurrent: {currentPlayers}\nRequired: {maxPlayers}");
            
            // Hiện thông báo sử dụng UI errorText hiện tại
            ShowErrorNotification("Not enough players, please wait...");
            return;
        }

        Debug.Log("[Lobby] Đủ người chơi. Starting Match...");
        currentRunner.LoadScene(SceneRef.FromIndex(1));
    }

    // Đã thêm tham số "message" với giá trị mặc định
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

        if (errorText != null)
            errorText.gameObject.SetActive(false);
    }

    private System.Collections.IEnumerator ShowErrorNotificationCoroutine(string message)
    {
        if (errorText != null)
        {
            errorText.text = message; // Đưa message mới vào UI
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

    // ==========================================
    // CÁC CALLBACK CỦA FUSION NETWORK RUNNER
    // ==========================================

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (runner == currentRunner)
        {
            runner.RemoveCallbacks(this);
            Destroy(runner.gameObject);
            currentRunner = null;
            networkSceneManager = null;
        }
    }

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
#pragma warning disable CS0618
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
#pragma warning restore CS0618
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ReadOnlySpan<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}