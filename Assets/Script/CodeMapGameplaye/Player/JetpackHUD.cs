using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Local UI HUD, should live in a scene Canvas, not in the player prefab.</summary>
public class JetpackHUD : MonoBehaviour
{
    [Header("UI References")]
    public Image progressFill;
    public TMP_Text statusText;
    public TMP_Text timeText;

    [Header("UI Colors")]
    public Color readyAndFlyingColor = new Color(0f, 0.92f, 0.98f);
    public Color rechargingColor = new Color(1f, 0.72f, 0.12f);

    [Header("Fill Animation")]
    [Min(0f)] public float fillSmoothSpeed = 10f;

    private PlayerMovement player;

    public void Bind(PlayerMovement localPlayer)
    {
        if (localPlayer != null && localPlayer.HasInputAuthority)
            player = localPlayer;
    }

    private void Awake()
    {
        if (progressFill != null)
        {
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            progressFill.fillAmount = 1f;
        }
    }

    private void Update()
    {
        if (player == null || player.Object == null || !player.Object.IsValid)
            Bind(PlayerMovement.LocalPlayer);

        if (player == null || player.Object == null || !player.Object.IsValid)
        {
            SetLabels("READY", "7.0s");
            SetFill(1f, readyAndFlyingColor);
            return;
        }

        float maxFuel = Mathf.Max(0.1f, player.maxBoostTime);
        float fuel = Mathf.Clamp(player.JetpackFuelRemaining, 0f, maxFuel);
        
        bool flying = player.IsJetpackBoosting;
        bool isFull = fuel >= (maxFuel - 0.01f);

        // Hiển thị trạng thái tuỳ thuộc vào việc đang bay, đang đầy, hay đang nạp
        string status = flying ? "FLYING" : (isFull ? "READY" : "RECHARGING");
        
        // Chuyển màu vàng nếu đang hồi, màu xanh nếu đang bay hoặc đã đầy
        Color currentColor = (flying || isFull) ? readyAndFlyingColor : rechargingColor;

        SetLabels(status, fuel.ToString("0.0") + "s");
        SetFill(fuel / maxFuel, currentColor);
    }

    private void SetLabels(string status, string time)
    {
        if (statusText != null) statusText.text = status;
        if (timeText != null) timeText.text = time;
    }

    private void SetFill(float amount, Color color)
    {
        if (progressFill == null) return;
        progressFill.color = color;
        float value = Mathf.Clamp01(amount);
        progressFill.fillAmount = fillSmoothSpeed <= 0f
            ? value
            : Mathf.MoveTowards(progressFill.fillAmount, value,
                fillSmoothSpeed * Time.deltaTime);
    }
}