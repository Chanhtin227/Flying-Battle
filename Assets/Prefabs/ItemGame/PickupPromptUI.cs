using UnityEngine;
using TMPro;

public class PickupPromptUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject promptPanel;
    public TMP_Text promptText;

    private void Awake()
    {
        Hide();
    }

    private void LateUpdate()
    {
        var player = PlayerMovement.LocalPlayer;

        if (player == null || !player.isActiveAndEnabled)
        {
            Hide();
            return;
        }

        PickupPromptTarget nearest = null;
        float nearestDistance = float.PositiveInfinity;

        foreach (var target in PickupPromptTarget.Targets)
        {
            if (target == null || !target.CanShow())
                continue;

            float distance = target.GetPlayerDistance(
                player.transform.position
            );

            if (distance > target.GetDistance())
                continue;

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = target;
            }
        }

        if (nearest == null)
        {
            Hide();
            return;
        }

        if (promptText != null)
            promptText.text = nearest.GetPrompt();

        if (promptPanel != null)
            promptPanel.SetActive(true);
    }

    private void OnDisable()
    {
        Hide();
    }

    private void Hide()
    {
        if (promptPanel != null)
            promptPanel.SetActive(false);
    }
}