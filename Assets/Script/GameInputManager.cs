using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

/// <summary>
/// Keeps Fusion's input callback alive while changing from the lobby to the match scene.
/// The lobby is destroyed by LoadScene, so it must not be the only input provider.
/// </summary>
public class GameInputManager : MonoBehaviour, INetworkRunnerCallbacks
{
    private static GameInputManager instance;
    private NetworkRunner runner;

    public static void EnsureForRunner(NetworkRunner networkRunner)
    {
        if (networkRunner == null)
            return;

        if (instance == null)
        {
            var inputObject = new GameObject(nameof(GameInputManager));
            instance = inputObject.AddComponent<GameInputManager>();
        }

        instance.Bind(networkRunner);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Bind(NetworkRunner networkRunner)
    {
        if (runner == networkRunner)
            return;

        if (runner != null)
            runner.RemoveCallbacks(this);

        runner = networkRunner;
        runner.ProvideInput = true;
        runner.AddCallbacks(this);
    }

    private void OnDestroy()
    {
        if (runner != null)
            runner.RemoveCallbacks(this);

        if (instance == this)
            instance = null;
    }

    public void OnInput(NetworkRunner networkRunner, NetworkInput input)
    {
        if (PlayerMovement.LocalPlayer != null)
            input.Set(PlayerMovement.LocalPlayer.GetLocalInput());
    }

    public void OnShutdown(NetworkRunner networkRunner, ShutdownReason shutdownReason)
    {
        if (runner != networkRunner)
            return;

        runner.RemoveCallbacks(this);
        runner = null;
    }

    public void OnObjectExitAOI(NetworkRunner networkRunner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner networkRunner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner networkRunner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner networkRunner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner networkRunner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner networkRunner) { }
    public void OnDisconnectedFromServer(NetworkRunner networkRunner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner networkRunner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner networkRunner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
#pragma warning disable CS0618
    public void OnUserSimulationMessage(NetworkRunner networkRunner, SimulationMessagePtr message) { }
#pragma warning restore CS0618
    public void OnSessionListUpdated(NetworkRunner networkRunner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner networkRunner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner networkRunner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner networkRunner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner networkRunner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner networkRunner) { }
    public void OnSceneLoadStart(NetworkRunner networkRunner) { }
}
