using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

public class LobbyController : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Fusion Network Runner")]
    public NetworkRunner runnerPrefab;
    private NetworkRunner currentRunner;

    [Header("UI Canvas Elements")]
    public Button gameModeBtn;
    
    [Header("Panel Game Mode")]
    public GameObject panelGameMode;
    public Button btn1vs1;
    public Button btn2vs2;
    public TMP_InputField inputID;

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

    private int maxPlayers = 2;

    void Start()
    {
        ResetUI();

        // Mở Panel Game Mode
        gameModeBtn.onClick.AddListener(() => {
            panelGameMode.SetActive(true);
            gameModeBtn.gameObject.SetActive(false); 
        });

        // Tạo phòng (Host)
        btn1vs1.onClick.AddListener(() => CreateRoom(2, panelRoom1vs1, roomID_1vs1));
        btn2vs2.onClick.AddListener(() => CreateRoom(4, panelRoom2vs2, roomID_2vs2));

        // VÀO PHÒNG BẰNG PHÍM ENTER
        inputID.onSubmit.AddListener((text) => JoinRoom());

        // Bắt đầu game
        startBtn_1vs1.onClick.AddListener(StartMatch);
        startBtn_2vs2.onClick.AddListener(StartMatch);
    }

    void ResetUI()
    {
        gameModeBtn.gameObject.SetActive(true);
        panelGameMode.SetActive(false);
        panelRoom1vs1.SetActive(false);
        panelRoom2vs2.SetActive(false);
    }

    void CreateRoom(int players, GameObject roomPanelToOpen, TextMeshProUGUI roomIDText)
    {
        maxPlayers = players;
        string randomID = UnityEngine.Random.Range(1000, 9999).ToString();
        
        panelGameMode.SetActive(false);
        roomPanelToOpen.SetActive(true);
        roomIDText.text = "ID: " + randomID;

        StartSession(GameMode.Host, randomID);
    }

    void JoinRoom()
    {
        string roomID = inputID.text;
        if (string.IsNullOrEmpty(roomID))
        {
            Debug.LogWarning("Chưa nhập ID phòng!");
            return;
        }

        panelGameMode.SetActive(false);
        
        // Mặc định vào phòng với tư cách Client
        StartSession(GameMode.Client, roomID);
    }

    async void StartSession(GameMode mode, string sessionName)
    {
        if (currentRunner == null)
        {
            currentRunner = Instantiate(runnerPrefab);
            currentRunner.AddCallbacks(this);
        }

        currentRunner.ProvideInput = true;

        var startGameArgs = new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionName,
            PlayerCount = maxPlayers,
            SceneManager = currentRunner.gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        var result = await currentRunner.StartGame(startGameArgs);

        if (result.Ok)
        {
            Debug.Log("Kết nối thành công!");
            
            startBtn_1vs1.gameObject.SetActive(currentRunner.IsServer);
            startBtn_2vs2.gameObject.SetActive(currentRunner.IsServer);
        }
        else
        {
            Debug.LogError("Lỗi kết nối: " + result.ShutdownReason);
            ResetUI(); 
        }
    }

    void StartMatch()
    {
        if (currentRunner != null && currentRunner.IsServer)
        {
            // SỬA LỖI CS1061: Dùng LoadScene với SceneRef thay vì SetActiveScene
            currentRunner.LoadScene(SceneRef.FromIndex(1)); 
        }
    }

    // ==========================================
    // CÁC HÀM BẮT BUỘC CỦA INetworkRunnerCallbacks
    // ==========================================
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    
    // TẮT CẢNH BÁO CS0618
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