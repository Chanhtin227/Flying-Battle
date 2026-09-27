using UnityEngine;

public class MapUIController : MonoBehaviour
{
    // =========================================================
    // UI
    // =========================================================

    [Header("Map UI")]

    public GameObject mapUI;


    // =========================================================
    // STATE
    // =========================================================

    private bool isMapOpen = false;


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // =====================================================
        // CHECK MAP UI
        // =====================================================

        if (mapUI == null)
        {
            return;
        }


        // =====================================================
        // TOGGLE M
        // =====================================================

        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMap();
        }
    }


    // =========================================================
    // TOGGLE MAP
    // =========================================================

    private void ToggleMap()
    {
        isMapOpen =
            !isMapOpen;


        mapUI.SetActive(
            isMapOpen
        );


        // =====================================================
        // OPEN MAP
        // =====================================================

        if (isMapOpen)
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible =
                true;
        }


        // =====================================================
        // CLOSE MAP
        // =====================================================

        else
        {
            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible =
                false;
        }
    }


    // =========================================================
    // CLOSE MAP
    // =========================================================

    public void CloseMap()
    {
        isMapOpen = false;


        if (mapUI != null)
        {
            mapUI.SetActive(
                false
            );
        }


        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible =
            false;
    }


    // =========================================================
    // IS OPEN
    // =========================================================

    public bool IsMapOpen()
    {
        return isMapOpen;
    }
}