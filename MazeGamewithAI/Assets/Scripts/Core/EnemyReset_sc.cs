using UnityEngine;

public class EnemyReset_sc : MonoBehaviour {
    
    private Vector3 startPosition;
    private Rigidbody2D rb;

    void Awake() {
        // Oyun başladığı (veya düşman oluşturulduğu) anki konumu kaydet
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
    }

    // Bu fonksiyon çağrılınca düşman ilk yerine ışınlanır
    public void ResetToStart() {
        // 1. Pozisyonu eski haline getir
        transform.position = startPosition;

        // 2. Eğer fiziksel bir hızı varsa sıfırla (Kaymayı önler)
        if (rb != null) {
            rb.velocity = Vector2.zero;
        }
    }
}