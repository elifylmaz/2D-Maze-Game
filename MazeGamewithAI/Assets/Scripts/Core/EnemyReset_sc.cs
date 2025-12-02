using UnityEngine;

public class EnemyReset_sc : MonoBehaviour {
    
    private Vector3 startPosition;
    private Rigidbody2D rb;

    void Awake() {
        // Oyun başladığı anki konumu kaydet
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
    }

    // Bu fonksiyon çağrılınca düşman ilk yerine ışınlanır
    public void ResetToStart() {
        transform.position = startPosition;
        if (rb != null) {
            rb.velocity = Vector2.zero;
        }
    }
}