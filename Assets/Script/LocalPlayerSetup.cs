using UnityEngine;
using Fusion;

public class LocalPlayerSetup : NetworkBehaviour
{
    [Header("Camera")]
    public GameObject cameraRoot;

    public override void Spawned()
    {
        if (cameraRoot == null)
        {
            Debug.LogError(
                "[LocalPlayerSetup] CAMERA ROOT CHƯA ĐƯỢC GÁN!"
            );

            return;
        }

        if (Object.HasInputAuthority)
        {
            // Player của máy hiện tại
            cameraRoot.SetActive(true);

            Debug.Log(
                "[LocalPlayerSetup] LOCAL PLAYER -> BẬT CAMERA"
            );
        }
        else
        {
            // Player của máy khác
            cameraRoot.SetActive(false);

            Debug.Log(
                "[LocalPlayerSetup] REMOTE PLAYER -> TẮT CAMERA"
            );
        }
    }
}