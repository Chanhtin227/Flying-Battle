
using UnityEngine;
using System;

public static class GoldWallet
{
    private const string GoldKey =
        "FlyingBattle_TotalGold_v1";

    public static event Action OnGoldChanged;

    // =====================================================
    // GET GOLD
    // =====================================================

    public static int GetGold()
    {
        return Mathf.Max(
            0,
            PlayerPrefs.GetInt(GoldKey, 0)
        );
    }

    // =====================================================
    // SET GOLD - NHAP SO VANG CHINH XAC
    // =====================================================

    public static void SetGold(int amount)
    {
        int newGold = Mathf.Max(0, amount);

        if (GetGold() == newGold)
            return;

        PlayerPrefs.SetInt(GoldKey, newGold);
        PlayerPrefs.Save();

        OnGoldChanged?.Invoke();
    }

    // =====================================================
    // ADD GOLD
    // =====================================================

    public static void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        long total = (long)GetGold() + amount;

        int newGold = (int)Math.Min(
            total,
            int.MaxValue
        );

        SetGold(newGold);
    }

    // =====================================================
    // SPEND GOLD - MUA VAT PHAM
    // =====================================================

    public static bool SpendGold(int amount)
    {
        if (amount < 0)
            return false;

        int currentGold = GetGold();

        if (currentGold < amount)
        {
            Debug.LogWarning(
                "[GoldWallet] Not enough Gold! " +
                "Current: " + currentGold +
                " | Price: " + amount
            );

            return false;
        }

        if (amount == 0)
            return true;

        SetGold(currentGold - amount);

        Debug.Log(
            "[GoldWallet] Spent: " + amount +
            " | Remaining: " + GetGold()
        );

        return true;
    }

    // =====================================================
    // CHECK GOLD
    // =====================================================

    public static bool HasEnoughGold(int amount)
    {
        return amount >= 0 && GetGold() >= amount;
    }
}
