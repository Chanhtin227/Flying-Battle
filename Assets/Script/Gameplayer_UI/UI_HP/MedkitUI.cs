using UnityEngine;
using TMPro;

public class MedkitUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text medkitCount;


    private PlayerHealth playerHealth;


    private void Update()
    {
        // =====================================================
        // TÌM LOCAL PLAYER
        // =====================================================

        if (PlayerMovement.LocalPlayer == null)
        {
            if (medkitCount != null)
                medkitCount.text = "0";

            return;
        }


        // =====================================================
        // LẤY PLAYER HEALTH
        // =====================================================

        if (playerHealth == null)
        {
            playerHealth =
                PlayerMovement.LocalPlayer.GetComponent<PlayerHealth>();
        }


        if (playerHealth == null)
            return;


        // =====================================================
        // TỔNG MEDKIT
        // =====================================================

        int total =
            playerHealth.GetTotalMedkitCount();


        // =====================================================
        // HIỂN THỊ
        // =====================================================

        if (medkitCount != null)
        {
            medkitCount.text =
                total.ToString();
        }
    }
}