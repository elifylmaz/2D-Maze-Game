using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

[RequireComponent(typeof(Rigidbody2D))]
public class QLearningTrainer_sc : MonoBehaviour {
    
    [Header("★ EĞİTİM AYARLARI ★")]
    public string experimentName = "QLearning"; 
    public int maxEpisodes = 200;          
    public int evaluateInterval = 20;       
    public int evaluateDuration = 2;        

    [Header("Q-Learning Parametreleri")]
    public float alpha = 0.2f;              
    public float gamma = 0.95f;             
    public float epsilon = 1.0f;          
    public float minEpsilon = 0.01f;        
    public float epsilonDecay = 0.995f;   

    [Header("Ortam Ayarları")]
    public float maxEpisodeDuration = 40f;  
    public float moveSpeed = 10f;            
    public float exitDetectRange = 50f;     
    public float rayDistance = 1.5f;        

    [Header("Ödül Değerleri")]
    public float rewardExit = 2000f;         
    public float rewardCoinNear = 120f;  
    public float rewardCoinFar = 80f;       
    public float penaltyEnemy = -200f;      
    public float penaltyWall = -2f;         
    public float penaltyStep = -0.02f;       
    public float penaltyTimeout = -300f;  
    public float penaltyStuck = -5f; 

    // İç değişkenler
    private Dictionary<string, float[]> qTable = new Dictionary<string, float[]>();
    private Rigidbody2D rb;
    private Transform exitTransform;
    private Vector3 startPosition;
    private string currentState;
    private int lastAction = -1;
    
    private int episodeCount = 1;
    private float totalScore = 0;
    private float episodeStartTime;
    private float bestScore = float.MinValue;
    private int successCount = 0;
    private int consecutiveSuccess = 0;
    
    private bool isEvaluating = false;
    private int evalCounter = 0;
    
    private string csvFilePath;
    private string saveFilePath;
    private string bestSaveFilePath;
    
    private Coroutine decisionCoroutine;
    private int stuckCounter = 0;
    private Vector3 lastCheckPosition;
    private float distanceToExit;

    void Start() {
        Debug.Log("<color=yellow>🔍 START ÇAĞRILDI</color>");
        
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) {
            Debug.LogError("❌ Rigidbody2D BULUNAMADI!");
            return;
        }
        Debug.Log($"<color=green>✅ Rigidbody2D bulundu: Type={rb.bodyType}</color>");
        
        startPosition = transform.position;
        lastCheckPosition = transform.position;
        
        GameObject exitObj = GameObject.FindGameObjectWithTag("Finish");
        if (exitObj != null) {
            exitTransform = exitObj.transform;
            Debug.Log($"<color=green>✅ Exit bulundu: {exitTransform.position}</color>");
        } else {
            Debug.LogError("❌ EXIT BULUNAMADI! Tag 'Finish' kontrol et!");
        }
        
        UpdateDistanceToExit();
        
        SetupFiles();
        
        currentState = GetState();
        Debug.Log($"<color=cyan>İlk State: {currentState}</color>");
        
        episodeStartTime = Time.time;
        
        decisionCoroutine = StartCoroutine(DecisionLoop());
        if (decisionCoroutine == null) {
            Debug.LogError("❌ COROUTINE BAŞLATILMADI!");
        } else {
            Debug.Log("<color=green>✅ DecisionLoop başlatıldı</color>");
        }
        
        Debug.Log($"<color=cyan>★★★ Q-LEARNING EĞİTİMİ BAŞLADI ★★★\nHedef: {maxEpisodes} episode</color>");
    }

    void SetupFiles() {
        csvFilePath = Application.dataPath + "/Scripts/qlearning_training.csv";
        saveFilePath = Application.dataPath + "/Scripts/qtable_save.json";
        bestSaveFilePath = Application.dataPath + "/Scripts/qtable_best.json";
        
        if (!File.Exists(csvFilePath)) {
            File.WriteAllText(csvFilePath, "Deney,Mod,Episode,Sure,Skor,StateSayisi,Sonuc,Epsilon,Streak\n");
        }
    }

    void Update() {
        float elapsed = Time.time - episodeStartTime;
        if (elapsed >= maxEpisodeDuration) HandleTimeout();
        
        // Takılma kontrolü
        if (Time.frameCount % 120 == 0) CheckIfStuck();
    }

    void CheckIfStuck() {
        if (Time.time - episodeStartTime < 2f) return;
        
        float distMoved = Vector3.Distance(transform.position, lastCheckPosition);
        if (distMoved < 0.5f) {
            stuckCounter++;
            if (stuckCounter >= 10) {
                Debug.Log("<color=yellow>⚠️ Agent takıldı</color>");
                totalScore += penaltyTimeout;
                EndEpisode("Takılma");
            }
        } else {
            stuckCounter = 0;
        }
        lastCheckPosition = transform.position;
    }

    IEnumerator DecisionLoop() {
        Debug.Log("<color=yellow>🔄 DecisionLoop BAŞLADI</color>");
        
        while (true) {
            currentState = GetState();
            int action = ChooseAction(currentState);
            Vector2 moveDir = GetActionVector(action);
            
            Debug.Log($"<color=cyan>State: {currentState} | Action: {action} | Dir: {moveDir}</color>");
            
            rb.velocity = moveDir * moveSpeed;
            Debug.Log($"<color=green>Velocity set: {rb.velocity}</color>");
            
            lastAction = action;
            
            if (moveDir != Vector2.zero) {
                string nextState = GetState();
                float stepReward = penaltyStep + CalculateProgressReward();
                
                if (!isEvaluating) {
                    UpdateQTable(currentState, lastAction, stepReward, nextState);
                }
                
                totalScore += stepReward;
            }
            
            yield return new WaitForSeconds(0.12f);
        }
    }

    // STATE: Exit yönü + Duvar tespiti
    string GetState() {
        string state = "";
        
        // Exit yönü (4 yön)
        if (exitTransform != null) {
            Vector2 dir = (exitTransform.position - transform.position).normalized;
            state += GetDirectionString(dir) + "_";
        } else {
            state += "NoExit_";
        }
        
        // Duvar tespiti (4 yön)
        bool wU = Physics2D.Raycast(transform.position, Vector2.up, rayDistance, LayerMask.GetMask("Obstacle"));
        bool wD = Physics2D.Raycast(transform.position, Vector2.down, rayDistance, LayerMask.GetMask("Obstacle"));
        bool wL = Physics2D.Raycast(transform.position, Vector2.left, rayDistance, LayerMask.GetMask("Obstacle"));
        bool wR = Physics2D.Raycast(transform.position, Vector2.right, rayDistance, LayerMask.GetMask("Obstacle"));
        
        state += (wU ? "U" : "_");
        state += (wD ? "D" : "_");
        state += (wL ? "L" : "_");
        state += (wR ? "R" : "_");
        
        return state;
    }

    string GetDirectionString(Vector2 dir) {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) {
            return (dir.x > 0) ? "R" : "L";
        } else {
            return (dir.y > 0) ? "U" : "D";
        }
    }

    int ChooseAction(string state) {
        // Optimistic Initialization
        if (!qTable.ContainsKey(state)) {
            qTable[state] = new float[] { 2.0f, 2.0f, 2.0f, 2.0f };
        }
        
        // Epsilon-Greedy (Rastgelelik)
        if (!isEvaluating && Random.value < epsilon) {
            return Random.Range(0, 4);
        }
        
        float[] values = qTable[state];
        
        // --- YENİ MANTIK: Eşitlik durumunda rastgele seç ---
        
        // 1. En yüksek puanı bul
        float maxVal = float.MinValue;
        for (int i = 0; i < 4; i++) {
            if (values[i] > maxVal) maxVal = values[i];
        }

        // 2. En yüksek puana sahip olan TÜM aksiyonları listele
        List<int> bestActions = new List<int>();
        for (int i = 0; i < 4; i++) {
            // Küçük float hatalarını yutmak için toleranslı karşılaştırma
            if (Mathf.Abs(values[i] - maxVal) < 0.001f) {
                bestActions.Add(i);
            }
        }

        // 3. Eşitler arasından rastgele birini seç
        return bestActions[Random.Range(0, bestActions.Count)];
    }

    void UpdateQTable(string s, int a, float r, string ns) {
        if (!qTable.ContainsKey(s)) qTable[s] = new float[4];
        if (!qTable.ContainsKey(ns)) qTable[ns] = new float[4];
        
        float maxQ = qTable[ns][0];
        for (int i = 1; i < 4; i++) {
            if (qTable[ns][i] > maxQ) maxQ = qTable[ns][i];
        }
        
        // Q-Learning formülü
        qTable[s][a] += alpha * (r + gamma * maxQ - qTable[s][a]);
    }

   
    float CalculateProgressReward() {
        if (exitTransform == null) return 0;
        float newDist = Vector2.Distance(transform.position, exitTransform.position);
        float progress = distanceToExit - newDist;
        UpdateDistanceToExit();
        return progress * 1.0f; 
    }

    void UpdateDistanceToExit() {
        if (exitTransform != null) {
            distanceToExit = Vector2.Distance(transform.position, exitTransform.position);
        }
    }

    Vector2 GetActionVector(int a) {
        return a == 0 ? Vector2.up : (a == 1 ? Vector2.down : (a == 2 ? Vector2.left : Vector2.right));
    }

    void OnCollisionEnter2D(Collision2D col) { HandleInteraction(col.gameObject); }
    void OnTriggerEnter2D(Collider2D col) { HandleInteraction(col.gameObject); }

    void HandleInteraction(GameObject obj) {
        float reward = 0;
        bool reset = false;
        string result = "Devam";
        
        if (obj.CompareTag("Reward")) {
    // Exit'e olan mesafeyi ölçüyor
           float dist = exitTransform != null ? Vector2.Distance(obj.transform.position, exitTransform.position) : 99f;
    
    // Eğer 4 birimden yakınsa "Near" ödülü, değilse "Far" ödülü veriyor.
          reward = (dist < 4f) ? rewardCoinNear : rewardCoinFar; 
    
            obj.SetActive(false);
       }
       else if (obj.CompareTag("Enemy")) {
            reward = penaltyEnemy;
            reset = true;
            result = "Olum";
            consecutiveSuccess = 0;
        }
        else if (obj.CompareTag("Finish")) {
            rb.velocity = Vector2.zero;
            reward = rewardExit;
            
            float time = Time.time - episodeStartTime;
            if (time < 20f) reward += 50f; 
            
            reset = true;
            result = "Basari";
            successCount++;
            consecutiveSuccess++;
            
        
            if (consecutiveSuccess >= 5 && !isEvaluating) {
                epsilon = Mathf.Max(minEpsilon, epsilon * 0.9f);
            }
        }
        else if (obj.CompareTag("Wall")) {
            if (!isEvaluating) reward = penaltyWall;
        }
        
        if (reward != 0) totalScore += reward;
        
        if (!isEvaluating && reward != 0) {
            UpdateQTable(currentState, lastAction, reward, GetState());
        }
        
        if (reset) {
            if (!isEvaluating && result == "Basari" && totalScore > bestScore) {
                bestScore = totalScore;
                SaveBestQTable();
            }
            EndEpisode(result);
        }
    }

    void HandleTimeout() {
        totalScore += penaltyTimeout;
        if (!isEvaluating) {
            UpdateQTable(currentState, lastAction, penaltyTimeout, GetState());
        }
        consecutiveSuccess = 0;
        EndEpisode("Timeout");
    }

    void EndEpisode(string result) {
        SaveToCSV(result);
        
        if (!isEvaluating) {
            if (epsilon > minEpsilon) epsilon *= epsilonDecay;
        }
        
        // Her 10 episode'da bir kaydet 
        if (episodeCount % 10 == 0) {
            SaveQTable();
            Debug.Log($"<color=cyan>💾 Q-Table kaydedildi (Episode {episodeCount}): {qTable.Count} state</color>");
        }
        
        if (episodeCount >= maxEpisodes) {
            Debug.Log($"<color=green>★★★ EĞİTİM TAMAMLANDI! ★★★\n" +
                     $"Toplam Başarı: {successCount}/{maxEpisodes}\n" +
                     $"En İyi Skor: {bestScore:F1}\n" +
                     $"Q-Table Boyutu: {qTable.Count} state</color>");
            
            SaveQTable();
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
            return;
        }
        
        ManageEvaluation();
        episodeCount++;
        ResetEpisode();
    }

    void ManageEvaluation() {
        if (isEvaluating) {
            evalCounter++;
            if (evalCounter >= evaluateDuration) {
                isEvaluating = false;
            }
        } else {
            if (episodeCount % evaluateInterval == 0) {
                isEvaluating = true;
                evalCounter = 0;
                Debug.Log("<color=green>★ EVALUATION BAŞLADI ★</color>");
            }
        }
    }

    void ResetEpisode() {
        if (decisionCoroutine != null) StopCoroutine(decisionCoroutine);
        
        rb.velocity = Vector2.zero;
        transform.position = startPosition;
        totalScore = 0;
        episodeStartTime = Time.time;
        stuckCounter = 0;
        lastCheckPosition = startPosition;
        UpdateDistanceToExit();
        
        // Rewardları yeniden aktif et
        GameObject[] coins = GameObject.FindGameObjectsWithTag("Reward");
        foreach (var coin in coins) coin.SetActive(true);
        
        decisionCoroutine = StartCoroutine(DecisionLoop());
    }

    void SaveToCSV(string result) {
        float duration = Time.time - episodeStartTime;
        string mode = isEvaluating ? "EVAL" : "TRAIN";
        string line = $"{experimentName},{mode},{episodeCount},{duration:F2},{totalScore:F2},{qTable.Count},{result},{epsilon:F3},{consecutiveSuccess}\n";
        File.AppendAllText(csvFilePath, line);
    }

    void SaveQTable() {
        QTableData data = new QTableData();
        foreach (var kvp in qTable) {
            data.entries.Add(new QTableEntry { state = kvp.Key, values = kvp.Value });
        }
        File.WriteAllText(saveFilePath, JsonUtility.ToJson(data, true));
    }

    void SaveBestQTable() {
        QTableData data = new QTableData();
        foreach (var kvp in qTable) {
            data.entries.Add(new QTableEntry { state = kvp.Key, values = kvp.Value });
        }
        File.WriteAllText(bestSaveFilePath, JsonUtility.ToJson(data, true));
        Debug.Log($"<color=green>★★★ YENİ REKOR: {bestScore:F1} ★★★</color>");
    }

    [System.Serializable]
    public class QTableData {
        public List<QTableEntry> entries = new List<QTableEntry>();
    }

    [System.Serializable]
    public class QTableEntry {
        public string state;
        public float[] values;
    }

   
    void OnGUI() {
        GUIStyle s = new GUIStyle { fontSize = 22, fontStyle = FontStyle.Bold };
        s.normal.textColor = Color.white;
        
        int x = 15, y = 15, g = 28;
        
        s.normal.textColor = isEvaluating ? Color.green : Color.yellow;
        GUI.Label(new Rect(x, y, 500, 35), isEvaluating ? "📊 EVALUATION" : "🎓 TRAINING", s);
        
        y += g; s.normal.textColor = Color.white;
        GUI.Label(new Rect(x, y, 500, 35), $"Episode: {episodeCount}/{maxEpisodes}", s);
        
        y += g;
        float timeLeft = maxEpisodeDuration - (Time.time - episodeStartTime);
        s.normal.textColor = timeLeft < 15 ? Color.red : Color.cyan;
        GUI.Label(new Rect(x, y, 500, 35), $"⏱ {timeLeft:F1}s", s);
        
        y += g; s.normal.textColor = totalScore > 0 ? Color.green : Color.red;
        GUI.Label(new Rect(x, y, 500, 35), $"Skor: {totalScore:F1}", s);
        
        y += g; s.normal.textColor = Color.yellow;
        GUI.Label(new Rect(x, y, 500, 35), $"★ Rekor: {(bestScore == float.MinValue ? "---" : bestScore.ToString("F1"))}", s);
        
        y += g; s.normal.textColor = Color.magenta;
        GUI.Label(new Rect(x, y, 500, 35), $"ε: {epsilon * 100:F1}%", s);
        
        y += g; s.normal.textColor = Color.green;
        GUI.Label(new Rect(x, y, 500, 35), $"✓ {successCount} | Streak: {consecutiveSuccess}", s);
        
        y += g; s.fontSize = 16; s.normal.textColor = Color.cyan;
        GUI.Label(new Rect(x, y, 800, 30), $"State: {currentState}", s);
        
        y += 22;
        GUI.Label(new Rect(x, y, 500, 30), $"Q-Table: {qTable.Count} states", s);
    }

    void OnApplicationQuit() { SaveQTable(); }
    void OnDisable() { SaveQTable(); }
}