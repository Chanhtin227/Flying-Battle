using UnityEngine;

public class ItemRotate : MonoBehaviour
{
    [Header("Rotate")]
    public float rotateSpeed = 100f;

    private bool canRotate = true;

    private void Update()
    {
        if (!canRotate)
            return;

        transform.Rotate(
            Vector3.up,
            rotateSpeed * Time.deltaTime,
            Space.World
        );
    }

    public void StopRotate()
    {
        canRotate = false;
    }
}