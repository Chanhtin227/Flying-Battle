using Fusion;
using UnityEngine;

public class RoomData : NetworkBehaviour
{
    public static RoomData Instance; // Để UI dễ dàng truy cập dữ liệu

    [Networked]
    [Capacity(4)]
    public NetworkDictionary<int, NetworkString<_32>> SyncPlayerNames => default;

    public override void Spawned()
    {
        Instance = this;

        // Tự động lấy tên đã lưu ở bảng Cài đặt
        string myName = PlayerPrefs.GetString("MyPlayerName", "Player" + Random.Range(10, 99));

        if (HasStateAuthority)
        {
            AddName(myName);
        }
        else
        {
            Rpc_RegisterPlayerName(myName);
        }
    }

    // [FIX] Dọn dẹp static reference khi object bị hủy (kết thúc trận, rời phòng,
    // Host disconnect...). Thiếu bước này khiến RoomData.Instance có thể vẫn trỏ
    // tới object đã bị hủy của PHIÊN TRƯỚC, gây lỗi khi LobbyController.UpdateRoomUI()
    // vô tình đọc dữ liệu cũ hoặc NullReferenceException ở trận chơi tiếp theo.
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_RegisterPlayerName(string playerName)
    {
        AddName(playerName);
    }

    private void AddName(string playerName)
    {
        int maxSlot = Runner.SessionInfo.MaxPlayers;
        for (int i = 0; i < maxSlot; i++)
        {
            if (!SyncPlayerNames.ContainsKey(i))
            {
                SyncPlayerNames.Add(i, playerName);
                break;
            }
        }
    }
}