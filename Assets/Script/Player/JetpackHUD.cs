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
        float maxRecharge = Mathf.Max(0.01f, player.jetpackRechargeTime);
        float fuel = Mathf.Clamp(player.JetpackFuelRemaining, 0f, maxFuel);
        float cooldown = Mathf.Max(0f, player.JetpackCooldownRemaining);

        if (cooldown > 0.01f)
        {
            SetLabels("RECHARGING", cooldown.ToString("0.0") + "s");
            SetFill(1f - Mathf.Clamp01(cooldown / maxRecharge), rechargingColor);
        }
        else
        {
            bool flying = player.IsJetpackBoosting;
            SetLabels(flying ? "FLYING" : "READY", fuel.ToString("0.0") + "s");
            SetFill(fuel / maxFuel, readyAndFlyingColor);
        }
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
