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