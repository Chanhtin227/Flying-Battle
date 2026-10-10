using UnityEngine;

/// <summary>
/// Luong vang luu tren THIET BI hien tai qua PlayerPrefs.
/// Khong phai he thong tai khoan online / bao mat chong gian lan.
/// </summary>
public static class GoldWallet
{
    private const string GoldKey = "FlyingBattle_TotalGold_v1";

    public static int GetGold()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(GoldKey, 0));
    }

    public static void AddGold(int amount)
    {
        if (amount <= 0) return;

        // Tranh tran so int khi cong so luong rat lon.
        long sum = (long)GetGold() + amount;
        PlayerPrefs.SetInt(GoldKey, (int)(sum > int.MaxValue ? int.MaxValue : sum));
        PlayerPrefs.Save();
    }

    public static bool SpendGold(int amount)
    {
        if (amount < 0 || GetGold() < amount) return false;
        PlayerPrefs.SetInt(GoldKey, GetGold() - amount);
        PlayerPrefs.Save();
        return true;
    }
}
