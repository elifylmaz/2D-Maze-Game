using UnityEngine;

/// <summary>
/// GÜNCELLENMİŞ ENEMY HAREKETİ
/// Özellikler: Duvarlara çarpmadan önce döner (Raycast), sınırlara sadıktır, sıkışırsa kurtulur.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class RandomMover_sc : MonoBehaviour {
    [Header("Hareket Ayarları")]
    public float speed = 2.0f; // Biraz hızlandırdık
    public float changeDirectionTime = 2.0f; // Kaç saniyede bir rastgele dönsün
    public float obstacleCheckDist = 0.6f; // Duvarı ne kadar uzaktan hissetsin

    private Rigidbody2D rb;
    private Vector2 movementDirection;
    private float timer;
    
    // Sıkışma kontrolü için
    private Vector2 lastPos;
    private float stuckCheckTimer;

    // Hareket sınırları
    private float xMin = -7.0f, xMax = 7.0f; 
    private float yMin = -3.0f, yMax = 3.0f;

    void Start() {
        rb = GetComponent<Rigidbody2D>();
        lastPos = transform.position;
        PickRandomDirection();
    }

    // MazeGenerator tarafından çağrılır
    public void SetBoundaries(float minX, float maxX, float minY, float maxY) {
        xMin = minX; xMax = maxX; 
        yMin = minY; yMax = maxY;
    }

    void Update() {
        timer += Time.deltaTime;
        stuckCheckTimer += Time.deltaTime;

        // 1. ZAMANLAYICI: Belirli aralıklarla yön değiştir (Kaotiklik için)
        if (timer > changeDirectionTime) { 
            PickRandomDirection(); 
            timer = 0; 
        }

        // 2. RAYCAST: Önünde duvar var mı?
        // "Obstacle" layer'ını algıla
        RaycastHit2D hit = Physics2D.Raycast(transform.position, movementDirection, obstacleCheckDist, LayerMask.GetMask("Obstacle"));
        if (hit.collider != null) {
            // Duvar gördü, hemen dön!
            PickRandomDirection();
        }

        // 3. SINIR KONTROLÜ: Sınırdan çıkıyorsa geri dön
        CheckBoundsSoft();
        
        // 4. SIKIŞMA KONTROLÜ: Hareket etmiyor muyuz?
        if (stuckCheckTimer > 0.5f) {
            if (Vector2.Distance(transform.position, lastPos) < 0.1f) {
                // Sıkışmışız, yön değiştir
                PickRandomDirection();
            }
            lastPos = transform.position;
            stuckCheckTimer = 0;
        }
    }

    void FixedUpdate() { 
        rb.velocity = movementDirection * speed; 
    }

    // Sınırlara yaklaştıysa yönü tersine çevir (Teleport etmek yerine)
    void CheckBoundsSoft() {
        bool outOfBounds = false;
        Vector2 pos = transform.position;

        if (pos.x < xMin || pos.x > xMax) {
            movementDirection.x = -movementDirection.x; // X yönünü ters çevir
            outOfBounds = true;
        }
        if (pos.y < yMin || pos.y > yMax) {
            movementDirection.y = -movementDirection.y; // Y yönünü ters çevir
            outOfBounds = true;
        }

        // Eğer çok dışarı çıktıysa (güvenlik) içeri it
        if (outOfBounds) {
            float clampedX = Mathf.Clamp(pos.x, xMin + 0.1f, xMax - 0.1f);
            float clampedY = Mathf.Clamp(pos.y, yMin + 0.1f, yMax - 0.1f);
            transform.position = new Vector3(clampedX, clampedY, 0);
        }
    }

    void PickRandomDirection() {
        // Tamamen rastgele bir yön (Normalize edilmiş)
        movementDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        
        // Eğer duruyorsa (0,0) tekrar seç
        if (movementDirection == Vector2.zero) {
            movementDirection = new Vector2(1, 0); 
        }
    }

    // Fiziksel çarpışma olursa (Raycast kaçırırsa yedek plan)
    void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject.CompareTag("Wall") || collision.gameObject.CompareTag("Enemy")) {
            PickRandomDirection();
        }
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((xMin + xMax) / 2, (yMin + yMax) / 2, 0);
        Vector3 size = new Vector3(xMax - xMin, yMax - yMin, 1);
        Gizmos.DrawWireCube(center, size);
        
        // Raycast'i göster (Debug için)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)movementDirection * obstacleCheckDist);
    }
}