using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class RoomData : NetworkBehaviour, IPlayerLeft
{
    public static RoomData Instance;

    [Networked]
    [Capacity(4)]
    public NetworkDictionary<int, NetworkString<_32>> SyncPlayerNames => default;

    // Lưu trữ tạm thời trên Host để biết PlayerRef nào đang giữ Slot (int) nào
    private Dictionary<PlayerRef, int> playerSlots = new Dictionary<PlayerRef, int>();

    public override void Spawned()
    {
        Instance = this;
        string myName = PlayerPrefs.GetString("MyPlayerName", "Player" + Random.Range(10, 99));

        if (HasStateAuthority)
        {
            AddName(Runner.LocalPlayer, myName);
        }
        else
        {
            Rpc_RegisterPlayerName(myName);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // Sử dụng RpcInfo để biết chính xác Client nào (PlayerRef) đang gửi yêu cầu
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_RegisterPlayerName(string playerName, RpcInfo info = default)
    {
        AddName(info.Source, playerName);
    }

    private void AddName(PlayerRef player, string playerName)
    {
        int maxSlot = Runner.SessionInfo.MaxPlayers;
        for (int i = 0; i < maxSlot; i++)
        {
            if (!SyncPlayerNames.ContainsKey(i))
            {
                SyncPlayerNames.Add(i, playerName);
                playerSlots[player] = i; // Ghi nhớ slot của người chơi này
                break;
            }
        }
    }

    // Tự động được Fusion gọi khi có người chơi thoát phòng
    public void PlayerLeft(PlayerRef player)
    {
        if (HasStateAuthority)
        {
            if (playerSlots.TryGetValue(player, out int slotId))
            {
                SyncPlayerNames.Remove(slotId); // Xóa tên khỏi UI
                playerSlots.Remove(player);     // Xóa khỏi từ điển tạm
            }
        }
    }
}