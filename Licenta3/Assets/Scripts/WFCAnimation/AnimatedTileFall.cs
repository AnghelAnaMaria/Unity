using UnityEngine;
using System;

public class AnimatedTileFall : MonoBehaviour
{
    public Vector3 targetPosition;
    public float speed = 5f;
    public Action onArrived;

    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogError("NU ai SpriteRenderer pe animatedTilePrefab!");
            Destroy(gameObject);
            return;
        }
        sr.color = new Color(1, 1, 1, 0);//sprite invizibil la inceput (alb complet (1,1,1 pentru RGB), cu alpha = 0)
    }

    void Update()
    {
        if (sr == null) return;

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        // Fade in
        if (sr.color.a < 1f)
        {
            Color c = sr.color;
            c.a += Time.deltaTime * 2f;
            sr.color = c;
        }

        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            try
            {
                onArrived?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError("Eroare la onArrived: " + ex);
            }
            Destroy(gameObject);
        }
    }
}

