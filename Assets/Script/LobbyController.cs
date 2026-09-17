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
    public TMP_InputField settingNameInput; // Kéo object InputName vào đây

    private int maxPlayers = 2;
    private Coroutine errorNotificationCoroutine;

    private void Start()
    {
        ResetUI();

        // Tải lại tên đã lưu trước đó lên ô InputField trong cài đặt (nếu có)
        if (settingNameInput != null)
        {
            settingNameInput.text = PlayerPrefs.GetString("MyPlayerName", "");
        }

        gameModeBtn.onClick.AddListener(OpenGameModePanel);

        settingBtn.onClick.AddListener(() => 
        {
            panelSetting.SetActive(true);
        });
        
        closeSettingBtn.onClick.AddListener(() => 
        {
            SavePlayerSettingName();
            panelSetting.SetActive(false);
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

        startBtn_1vs1.onClick.AddListener(StartMatch);
        startBtn_2vs2.onClick.AddListener(StartMatch);
    }

    private void Update()
    {
        // Liên tục cập nhật UI tên người chơi trong phòng
        UpdateRoomUI();
    }

    private void UpdateRoomUI()
    {
        // Kiểm tra xem dữ liệu mạng RoomData đã được spawn ra chưa
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
        panelGameMode.SetActive(true);
        gameModeBtn.gameObject.SetActive(false);
    }

    private void ResetUI()
    {
        gameModeBtn.gameObject.SetActive(true);
        panelGameMode.SetActive(false);
        panelRoom1vs1.SetActive(false);
        panelRoom2vs2.SetActive(false);
        startBtn_1vs1.gameObject.SetActive(false);
        startBtn_2vs2.gameObject.SetActive(false);
        panelSetting.SetActive(false);
    }

    private void CreateRoom(int players, GameObject roomPanelToOpen, TextMeshProUGUI roomIDText)
    {
        maxPlayers = players;
        string randomID = UnityEngine.Random.Range(1000, 10000).ToString();
        
        panelGameMode.SetActive(false);
        roomPanelToOpen.SetActive(true);
        roomIDText.text = "ID: " + randomID;

        Debug.Log($"[Lobby] Create Room\nRoom ID: {randomID}\nMax Players: {maxPlayers}");
        StartSession(GameMode.Host, randomID);
    }

    private void JoinRoom()
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

        Debug.Log($"[Fusion] StartGame\nMode: {mode}\nSession: {sessionName}\nPlayerCount: {(mode == GameMode.Host ? maxPlayers : -1)}");

        var result = await currentRunner.StartGame(startGameArgs);

        if (result.Ok)
        {
            Debug.Log($"[Fusion] StartGame SUCCESS\nSession: {sessionName}\nMode: {mode}");

            if (currentRunner.IsServer)
            {
                // HOST: Tự động Spawn Prefab quản lý dữ liệu tên người chơi
                if (roomDataPrefab != null)
                {
                    currentRunner.Spawn(roomDataPrefab);
                }
                else
                {
                    Debug.LogError("[Lobby] BẠN CHƯA KÉO ROOM DATA PREFAB VÀO LOBBY CONTROLLER!");
                }

                startBtn_1vs1.gameObject.SetActive(maxPlayers == 2);
                startBtn_2vs2.gameObject.SetActive(maxPlayers == 4);
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
        }
        else
        {
            Debug.LogError($"[Fusion] StartGame FAILED\nSession: {sessionName}\nMode: {mode}\nReason: {result.ShutdownReason}");
            if (mode == GameMode.Client)
            {
                panelGameMode.SetActive(true);
                ShowErrorNotification();
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

        if (currentPlayers < maxPlayers)
        {
            Debug.LogWarning($"[Lobby] Chưa đủ người chơi!\nCurrent: {currentPlayers}\nRequired: {maxPlayers}");
            return;
        }

        Debug.Log("[Lobby] Đủ người chơi. Starting Match...");
        currentRunner.LoadScene(SceneRef.FromIndex(1));
    }

    private void ShowErrorNotification()
    {
        HideErrorNotification();
        errorNotificationCoroutine = StartCoroutine(ShowErrorNotificationCoroutine());
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

    private System.Collections.IEnumerator ShowErrorNotificationCoroutine()
    {
        if (errorText != null)
        {
            errorText.text = "Room ID not correct! Again please";
            errorText.gameObject.SetActive(true);
            yield return new WaitForSeconds(1.5f);
            errorText.gameObject.SetActive(false);
        }
        errorNotificationCoroutine = null;
    }

    // ==========================================
    // CÁC CALLBACK CỦA FUSION NETWORK RUNNER
    // ==========================================

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[Fusion] Player Joined: {player} | Total Players: {runner.SessionInfo.PlayerCount}/{runner.SessionInfo.MaxPlayers}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[Fusion] Player Left: {player}\nTotal Players: {runner.SessionInfo.PlayerCount}/{runner.SessionInfo.MaxPlayers}");
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (PlayerMovement.LocalPlayer != null)
        {
            input.Set(PlayerMovement.LocalPlayer.GetLocalInput());
        }
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.LogError($"[Fusion] Runner Shutdown: {shutdownReason}");

        if (runner == currentRunner)
        {
            runner.RemoveCallbacks(this);
            Destroy(runner.gameObject);
            currentRunner = null;
            networkSceneManager = null;
        }
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log($"[Fusion] Connected To Server\nSession: {runner.SessionInfo.Name}");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogError($"[Fusion] Disconnected From Server\nReason: {reason}");
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"[Fusion] Connect Failed\nAddress: {remoteAddress}\nReason: {reason}");
    }

#pragma warning disable CS0618
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
#pragma warning restore CS0618

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.Log("========== SESSION LIST ==========");
        foreach (var session in sessionList)
        {
            Debug.Log($"Room: {session.Name} | Players: {session.PlayerCount}/{session.MaxPlayers} | IsOpen: {session.IsOpen} | IsVisible: {session.IsVisible}");
        }
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ReadOnlySpan<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { Debug.Log("[Fusion] Scene Load Done"); }
    public void OnSceneLoadStart(NetworkRunner runner) { Debug.Log("[Fusion] Scene Load Start"); }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}