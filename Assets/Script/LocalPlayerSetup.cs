using UnityEngine;
using Fusion;

public class LocalPlayerSetup : NetworkBehaviour
{
    [Header("Components to Disable for Clones")]
    public GameObject cameraRoot;

    public override void Spawned()
    {
        // Nếu đây là nhân vật của người khác (Clone)
        if (!HasInputAuthority)
        {
            // Tắt hoàn toàn CameraRoot (bao gồm Camera và AudioListener bên trong)
            if (cameraRoot != null)
            {
                cameraRoot.SetActive(false);
            }
        }
    }
}