using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HierarchicalBrain_sc : MonoBehaviour {
    
    [Header("★ KONTROL PANELİ ★")]
    public bool usePerceptron = true;
    public bool useQLearning = true;
    public bool useAStar = true;
    public float moveSpeed = 5f;

    [Header("Mıknatıs Modu")]
    public float finalApproachDistance = 0.8f;
    public float exitReachDistance = 0.5f;

    [Header("Sıkışma Ayarları")]
    public float stuckThreshold = 0.8f;
    public float layerLockoutTime = 2.0f;
    
    private AgentPerceptron_sc perceptron;
    private AgentAStar_sc astar;
    private AgentQLearning_sc qlearning;
    
    // Değişkenler
    private Rigidbody2D rb;
    private Transform exitTransform;
    private Vector2 lastPositionCheck;
    private float stuckTimer = 0f;
    private float qLearningLockTimer = 0f;
    private float conflictTimer = 0f;
    private bool isFleeing = false;
    private bool isGameOver = false;
    private string gameOverMessage = "";
    private Color gameOverColor = Color.white;

    // İstatistikler (GUI İçin)
    private string currentDecisionLayer = "Başlatılıyor...";
    private int perceptronCount = 0;
    private int qlearningCount = 0;
    private int astarCount = 0;
    private int finalApproachCount = 0;
    private int coinsCollected = 0;
    private float survivalTime = 0;
    private float startTime;
    private int spawnIndex = 0;

    // Harita Sınırları (Spawn için)
    public Vector2 mapMin = new Vector2(-8, -4); 
    public Vector2 mapMax = new Vector2(8, 4);

    void Start() {
        
        rb = GetComponent<Rigidbody2D>();
        perceptron = GetComponent<AgentPerceptron_sc>();
        astar = GetComponent<AgentAStar_sc>();
        qlearning = GetComponent<AgentQLearning_sc>();

        // Güvenlik Kontrolü
        if (rb == null || perceptron == null || astar == null || qlearning == null) {
            Debug.LogError("❌ EKSİK BİLEŞEN: Lütfen AgentPerceptron, AgentAStar ve AgentQLearning scriptlerini ajana eklediğinden emin ol!");
            return; 
        }
        
        // Modülleri Başlat
        perceptron.Setup(rb);
        astar.Setup(FindObjectOfType<GridManager_sc>());
        qlearning.Setup(perceptron);
        
        GameObject exitObj = GameObject.FindGameObjectWithTag("Finish");
        if (exitObj != null) exitTransform = exitObj.transform;

        ResetGameVariables(false);
    }

    void Update() {
        if (isGameOver) { if(rb) rb.velocity = Vector2.zero; return; }
        survivalTime = Time.time - startTime;

        CheckIfStuck();
        
        if (exitTransform != null && Vector2.Distance(transform.position, exitTransform.position) <= exitReachDistance) {
            EndGame(true);
            return;
        }

        Vector2 action = DecideHierarchicalAction();
        
        // HAREKET UYGULAMA
        if (action != Vector2.zero && rb != null) rb.velocity = action * moveSpeed;
        else if (rb != null) rb.velocity = Vector2.zero;
    }

    Vector2 DecideHierarchicalAction() {
        // 0. MIKNATIS MODU
        if (exitTransform != null) {
            float dist = Vector2.Distance(transform.position, exitTransform.position);
            if (dist < finalApproachDistance && !isFleeing) {
                currentDecisionLayer = ">>> MIKNATIS <<<";
                finalApproachCount++;
                conflictTimer = 0;
                return (exitTransform.position - transform.position).normalized;
            }
        }

        // 1. KATMAN: PERCEPTRON
        if (usePerceptron) {
            Vector2 reflex = perceptron.GetEscapeVector(isFleeing);
            if (reflex != Vector2.zero) {
                isFleeing = true;
                currentDecisionLayer = "PERCEPTRON (Refleks)";
                perceptronCount++;
                
                // Çatışma Kontrolü
                if (useAStar && astar.IsActive()) {
                    conflictTimer += Time.deltaTime;
                    if (conflictTimer > 1.0f) {
                        Debug.Log("⚠️ Çatışma! A* Yenileniyor...");
                        astar.ForceReplan();
                        conflictTimer = 0;
                    }
                }
                return reflex;
            }
        }
        isFleeing = false;
        conflictTimer = 0;

        // 2. KATMAN: Q-LEARNING
        if (useQLearning && qLearningLockTimer <= 0) {
            Vector2 qAction = qlearning.GetTacticDirection(exitTransform);
            if (qAction != Vector2.zero) {
                currentDecisionLayer = "Q-LEARNING (Taktik)";
                qlearningCount++;
                return qAction;
            }
        }
        if(qLearningLockTimer > 0) currentDecisionLayer = "⚠️ Q-L KİLİTLİ -> A*";

        // 3. KATMAN: A*
        if (useAStar) {
            Vector2 aAction = astar.GetPathDirection(exitTransform);
            if (aAction != Vector2.zero) {
                if(qLearningLockTimer <= 0) currentDecisionLayer = "A* (Strateji)";
                astarCount++;
                return aAction;
            }
        }

        return Vector2.zero;
    }

    void CheckIfStuck() {
        if (qLearningLockTimer > 0) qLearningLockTimer -= Time.deltaTime;
        
        if (Vector2.Distance(transform.position, lastPositionCheck) < 0.1f) {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > stuckThreshold) {
                if (!currentDecisionLayer.Contains("A*") && !currentDecisionLayer.Contains("MIKNATIS")) {
                    qLearningLockTimer = layerLockoutTime;
                    stuckTimer = 0;
                }
            }
        } else {
            stuckTimer = 0;
            lastPositionCheck = transform.position;
        }
    }

    void EndGame(bool isWin) {
        if (isGameOver) return;
        isGameOver = true;
        
        if (isWin) {
            gameOverMessage = "★ KAZANDIN! ★";
            gameOverColor = Color.green;
        } else {
            gameOverMessage = "💀 YAKALANDIN! 💀";
            gameOverColor = Color.red;
        }
        
        StartCoroutine(RestartRoutine());
    }

    IEnumerator RestartRoutine() {
        float countdown = 3f;
        while (countdown > 0) { countdown -= Time.deltaTime; yield return null; }
        ResetGameVariables(true);
    }
    
    void OnCollisionEnter2D(Collision2D col) { if(!isGameOver && col.gameObject.CompareTag("Enemy")) EndGame(false); }
    void OnTriggerEnter2D(Collider2D col) { if(!isGameOver && col.gameObject.CompareTag("Reward")) { coinsCollected++; col.gameObject.SetActive(false); } }

    void ResetGameVariables(bool move) {
        isGameOver = false;
        survivalTime = 0;
        startTime = Time.time;
        if(perceptron) perceptron.FindEnemies(); 
        
        // 1. Perceptron'a düşmanları tekrar buldur 
        if(perceptron) perceptron.FindEnemies(); 
        
        // 2. Ödülleri tekrar aktif et
        GameObject[] rewards = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in rewards) if (go.CompareTag("Reward")) go.SetActive(true);

        // DÜŞMANLARI BAŞA SAR 
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject e in enemies) {
            
            EnemyReset_sc resetScript = e.GetComponent<EnemyReset_sc>();
            
            // Eğer script varsa çalıştır
            if (resetScript != null) {
                resetScript.ResetToStart();
            }
        }
        
        if (move) {
             // Köşelerin Listesi:
             List<Vector2> corners = new List<Vector2> { 
                 mapMax,                          
                 new Vector2(mapMax.x, mapMin.y), 
                 new Vector2(mapMin.x, mapMax.y), 
                 mapMin                         
             };
             
             transform.position = corners[spawnIndex];
            
             spawnIndex = (spawnIndex + 1) % 4;
        }
    }

    void OnGUI() {
        GUIStyle s = new GUIStyle { fontSize = 20, fontStyle = FontStyle.Bold };
        int x = 15, y = 15, g = 26; s.normal.textColor = Color.cyan;
        GUI.Label(new Rect(x, y, 600, 30), "★ HİYERARŞİK AGENT v6 (MODÜLER) ★", s);
        y += g; s.fontSize = 18; s.normal.textColor = Color.yellow;
        GUI.Label(new Rect(x, y, 600, 30), $"Karar: {currentDecisionLayer}", s);
        y += g; s.normal.textColor = Color.white;
        GUI.Label(new Rect(x, y, 400, 30), $"⏱ {survivalTime:F1}s | Coin: {coinsCollected}", s);
        y += g + 10; s.fontSize = 16; s.normal.textColor = Color.cyan;
        GUI.Label(new Rect(x, y, 400, 25), $"Perceptron: {perceptronCount}", s);
        y += 22; GUI.Label(new Rect(x, y, 400, 25), $"Q-Learning: {qlearningCount}", s);
        y += 22; GUI.Label(new Rect(x, y, 400, 25), $"A* (Strateji): {astarCount}", s);
        
        y += 22; s.normal.textColor = Color.green;
        GUI.Label(new Rect(x, y, 400, 25), $">>> MIKNATIS: {finalApproachCount}", s);

        if (qLearningLockTimer > 0) {
            y += 28; s.normal.textColor = Color.red;
            GUI.Label(new Rect(x, y, 400, 25), $"⚠️ SIKIŞMA! A* Zorlanıyor...", s);
        }

        if (isGameOver) {
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.95f));
            
            float boxWidth = 600f;
            float boxHeight = 200f;
            float boxX = (Screen.width - boxWidth) / 2;
            float boxY = (Screen.height - boxHeight) / 2;
            
            GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), "", boxStyle);
            
            GUIStyle bigStyle = new GUIStyle { 
                fontSize = 60, fontStyle = FontStyle.Bold, 
                normal = { textColor = gameOverColor }, alignment = TextAnchor.MiddleCenter 
            };
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), gameOverMessage, bigStyle);
            
            GUIStyle infoStyle = new GUIStyle {
                fontSize = 24, fontStyle = FontStyle.Normal,
                normal = { textColor = Color.white }, alignment = TextAnchor.MiddleCenter
            };
            string infoText = $"Süre: {survivalTime:F1}s | Coin: {coinsCollected}";
            GUI.Label(new Rect(0, Screen.height / 2 + 40, Screen.width, 50), infoText, infoStyle);
       }
    }

    private Texture2D MakeTex(int width, int height, Color col) {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
    void OnDrawGizmos() {
        if (!Application.isPlaying) return;

        if (perceptron != null) {
            Gizmos.color = isFleeing ? new Color(1, 0, 0, 0.4f) : new Color(1, 1, 0, 0.2f);
            Gizmos.DrawWireSphere(transform.position, isFleeing ? (perceptron.enemyDetectRange + perceptron.safetyMargin) : perceptron.enemyDetectRange);
        }

        if (useAStar && astar != null) {
            List<Node_sc> path = astar.GetCurrentPath(); 
            if (path != null) {
                Gizmos.color = Color.green;
                for (int i = 0; i < path.Count - 1; i++) 
                    Gizmos.DrawLine(path[i].worldPosition, path[i + 1].worldPosition);
            }
        }
        
        if (exitTransform != null) {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(exitTransform.position, finalApproachDistance); 
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(exitTransform.position, exitReachDistance); 
        }
    }
}