using UnityEngine;

/// <summary>
/// Keeps a chest marker at the chest's X/Z coordinates, but draws it
/// close to the top-down map camera so world geometry cannot occlude it.
/// Attach to MinimapMarker (a child of the network-spawned chest).
/// </summary>
public class ChestMinimapMarker : MonoBehaviour
{
    [Header("Chest Reference")]
    public Transform chestRoot;

    [Header("Minimap Camera")]
    [Tooltip("MapCamera (the one with MapCameraFollow). Leave empty to auto-find it.")]
    public Camera mapCamera;

    [Tooltip("Distance in front of the map camera, in meters. Must be greater than Near Clip Plane.")]
    [Min(0.5f)] public float distanceBelowCamera = 10f;

    [Header("Icon Settings")]
    [Tooltip("Fallback height above chest when the map camera is not found.")]
    public float heightOffset = 3f;

    [Tooltip("Keep the World Space Canvas facing the top-down map camera.")]
    public bool keepTopDownRotation = true;

    private void Awake()
    {
        if (chestRoot == null && transform.parent != null)
            chestRoot = transform.parent;
    }

    private void LateUpdate()
    {
        if (chestRoot == null)
            return;

        if (mapCamera == null)
        {
            MapCameraFollow follow = FindFirstObjectByType<MapCameraFollow>();
            if (follow != null)
                mapCamera = follow.GetComponent<Camera>();
        }

        float iconHeight = chestRoot.position.y + heightOffset;

        if (mapCamera != null)
        {
            // The orthographic map camera looks straight down (positive X rotation).
            // Place the marker between the camera and all world geometry.
            float safeDistance = Mathf.Max(
                mapCamera.nearClipPlane + 0.5f,
                distanceBelowCamera
            );
            iconHeight = mapCamera.transform.position.y - safeDistance;
        }

        transform.position = new Vector3(
            chestRoot.position.x,
            iconHeight,
            chestRoot.position.z
        );

        if (keepTopDownRotation)
        {
            // World Space UI Canvas is on the XY plane; -90 X makes its
            // front face point up toward the map camera.
            transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        }
    }
}
