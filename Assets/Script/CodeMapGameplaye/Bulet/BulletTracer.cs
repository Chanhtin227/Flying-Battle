using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class BulletTracer : MonoBehaviour
{
    public float speed = 250f;
    public float lifeTime = 0.05f;

    private LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
    }

    public void Fire(Vector3 start, Vector3 end)
    {
        StartCoroutine(Tracer(start, end));
    }

    IEnumerator Tracer(Vector3 start, Vector3 end)
    {
        lr.positionCount = 2;

        float distance = Vector3.Distance(start, end);

        // Tránh chia cho 0
        if (distance <= 0.01f)
        {
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            yield return new WaitForSeconds(lifeTime);
            Destroy(gameObject);
            yield break;
        }

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * speed / distance;

            lr.SetPosition(0, start);
            lr.SetPosition(1, Vector3.Lerp(start, end, t));

            yield return null;
        }

        lr.SetPosition(1, end);

        yield return new WaitForSeconds(lifeTime);

        Destroy(gameObject);
    }
}