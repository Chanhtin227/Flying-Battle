
using UnityEngine;

public class GoldUIVisibility : MonoBehaviour
{
    [Header("Gold UI")]
    public GameObject goldUI;

    [Header("Other UI Panels")]
    public GameObject panelGameMode;
    public GameObject panelRoom1vs1;
    public GameObject panelRoom2vs2;
    public GameObject panelSetting;

    [Header("Other Panels (Optional)")]
    public GameObject[] otherPanels;

    private void LateUpdate()
    {
        if (goldUI == null)
            return;

        bool isOtherUIOpen = false;

        if (IsPanelOpen(panelGameMode) ||
            IsPanelOpen(panelRoom1vs1) ||
            IsPanelOpen(panelRoom2vs2) ||
            IsPanelOpen(panelSetting))
        {
            isOtherUIOpen = true;
        }

        if (otherPanels != null)
        {
            foreach (GameObject panel in otherPanels)
            {
                if (IsPanelOpen(panel))
                {
                    isOtherUIOpen = true;
                    break;
                }
            }
        }

        bool shouldShowGold = !isOtherUIOpen;

        if (goldUI.activeSelf != shouldShowGold)
        {
            goldUI.SetActive(shouldShowGold);
        }
    }

    private bool IsPanelOpen(GameObject panel)
    {
        return panel != null && panel.activeInHierarchy;
    }
}
