using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator_sc : MonoBehaviour {
    [Header("Prefablar")]
    public GameObject wallPrefab; 
    public GameObject exitPrefab;
    public GameObject agentPrefab; 
    public GameObject rewardPrefab;
    public GameObject enemyPrefab;

    [Header("Ayarlar")]
    public float cellSize = 1f;

    [Header("Düşman Ayarları")]
    public int enemyCount = 3; 
    
    public float minX = -7f, maxX = 7f;
    public float minY = -3f, maxY = 3f;

    // Harita (20x11)
    // 0: Yol, 1: Duvar
    int[,] levelMap = {
        {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
        {1,0,0,0,0,0,1,0,0,0,0,0,0,0,1,0,0,0,0,1},
        {1,0,1,1,1,0,0,0,1,1,1,0,1,0,1,0,1,1,0,1},
        {1,1,1,0,0,0,0,0,1,0,0,0,1,0,0,0,0,1,0,1},
        {1,0,1,0,1,1,1,0,0,0,1,0,1,0,1,1,0,1,0,1},
        {1,0,0,0,0,0,1,0,1,1,1,0,0,0,0,1,0,0,0,1},
        {1,0,1,1,1,0,1,0,0,1,0,0,1,1,0,1,0,1,0,1},
        {1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,0,1},
        {1,1,1,0,1,1,0,1,0,1,1,1,0,1,1,1,0,1,1,1},
        {1,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,1},
        {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1}
    };

    private List<Vector3> emptySpotsForEnemies = new List<Vector3>();

    // Sabit Ödül Yolu
    private List<Vector2Int> fixedRewardPath = new List<Vector2Int>() {
        new Vector2Int(7, 2), new Vector2Int(6, 9), new Vector2Int(10, 9),
        new Vector2Int(1, 7), new Vector2Int(5, 7), new Vector2Int(10, 7), new Vector2Int(16, 7),
        new Vector2Int(2, 5), new Vector2Int(7, 4), new Vector2Int(11, 5), 
        new Vector2Int(14, 5), new Vector2Int(11, 2), new Vector2Int(18, 5),
        new Vector2Int(4, 3), new Vector2Int(9, 3), new Vector2Int(13, 3), new Vector2Int(16, 3),
        new Vector2Int(2, 1), new Vector2Int(17, 1), new Vector2Int(12, 1)
    };

    void Start() {
        GenerateMap();       
        SpawnFixedRewards(); 
        
        GridManager_sc gridMgr = FindObjectOfType<GridManager_sc>();
        if(gridMgr != null) gridMgr.CreateGrid();
    }

    void GenerateMap() {
        int rows = levelMap.GetLength(0); 
        int cols = levelMap.GetLength(1);
        float startX = -(cols * cellSize) / 2f + cellSize / 2f;
        float startY = (rows * cellSize) / 2f - cellSize / 2f;

        emptySpotsForEnemies.Clear(); 

        for (int y = 0; y < rows; y++) {
            for (int x = 0; x < cols; x++) {
                Vector3 pos = new Vector3(startX + x * cellSize, startY - y * cellSize, 0);
                
                // EXIT (12, 0)
                if (x == 12 && y == 0) { 
                    Instantiate(exitPrefab, pos, Quaternion.identity).tag = "Finish"; 
                    continue; 
                }
                
                // AGENT (1, 9)
                if (x == 1 && y == 9) {
                    GameObject existingAgent = GameObject.FindGameObjectWithTag("Player");
                    if (existingAgent == null && agentPrefab != null) {
                        GameObject ag = Instantiate(agentPrefab, pos, Quaternion.identity);
                        ag.tag = "Player";
                        
                        SetSortingOrder(ag, 20); 
                    } else if (existingAgent != null) {
                        existingAgent.transform.position = pos;
                    }
                    continue; 
                }

                if (levelMap[y, x] == 1) {
                    GameObject wall = Instantiate(wallPrefab, pos, Quaternion.identity, transform);
                    wall.tag = "Wall";
                    wall.layer = LayerMask.NameToLayer("Obstacle"); 
                } else {
                    if (pos.x >= minX && pos.x <= maxX && pos.y >= minY && pos.y <= maxY) {
                        emptySpotsForEnemies.Add(pos);
                    }
                }
            }
        }
        
        SpawnEnemies();
    }

    void SpawnEnemies() {
        if (enemyPrefab == null) return;
        int placedCount = 0;

        int safetyLoop = 0; 

        while (placedCount < enemyCount && safetyLoop < 100) {
            safetyLoop++;
            
            if (emptySpotsForEnemies.Count == 0) break;

            int randomIndex = Random.Range(0, emptySpotsForEnemies.Count);
            Vector3 spawnPos = emptySpotsForEnemies[randomIndex];
            
            emptySpotsForEnemies.RemoveAt(randomIndex); 

            if (Physics2D.OverlapCircle(spawnPos, 0.4f, LayerMask.GetMask("Obstacle")) != null) {
                continue; 
            }

            GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            newEnemy.tag = "Enemy";

            SetSortingOrder(newEnemy, 10);

            RandomMover_sc mover = newEnemy.GetComponent<RandomMover_sc>();
            if (mover != null) {
                mover.SetBoundaries(minX, maxX, minY, maxY);
            }
            
            if (newEnemy.GetComponent<EnemyReset_sc>() == null) {
                newEnemy.AddComponent<EnemyReset_sc>();
            }
            
            placedCount++;
        }
        Debug.Log($"<color=red>⚔️ {placedCount} düşman yerleştirildi.</color>");
    }

    void SpawnFixedRewards() {
        if (rewardPrefab == null) return;

        int rows = levelMap.GetLength(0); 
        int cols = levelMap.GetLength(1);
        float startX = -(cols * cellSize) / 2f + cellSize / 2f;
        float startY = (rows * cellSize) / 2f - cellSize / 2f;

        foreach (Vector2Int gridPos in fixedRewardPath) {
            if (gridPos.y < rows && gridPos.x < cols && levelMap[gridPos.y, gridPos.x] == 1) continue;

            Vector3 worldPos = new Vector3(startX + gridPos.x * cellSize, startY - gridPos.y * cellSize, 0);

            GameObject reward = Instantiate(rewardPrefab, worldPos, Quaternion.identity);
            reward.tag = "Reward";
            
            SetSortingOrder(reward, 5);
            
            RandomMover_sc mover = reward.GetComponent<RandomMover_sc>();
            if (mover != null) Destroy(mover);
        }
    }

    // Sprite Renderer Order Ayarlayıcı
    void SetSortingOrder(GameObject obj, int order) {
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null) {
            sr.sortingOrder = order;
        }
    }
}