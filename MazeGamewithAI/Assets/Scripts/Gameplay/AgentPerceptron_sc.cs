using UnityEngine;
using System.Collections.Generic;

public class AgentPerceptron_sc : MonoBehaviour {
    [Header("Refleks Ayarları")]
    public float enemyDetectRange = 1.2f;
    public float safetyMargin = 0.7f;
    public float rayDistance = 0.7f;

    private List<Transform> allEnemies = new List<Transform>();
    private Rigidbody2D rb;

    public void Setup(Rigidbody2D _rb) {
        rb = _rb;
        FindEnemies();
    }

    public void FindEnemies() {
        allEnemies.Clear();
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject e in enemies) allEnemies.Add(e.transform);
    }

    public Vector2 GetEscapeVector(bool isFleeing) {
        Vector2 totalFleeVector = Vector2.zero;
        int threatCount = 0;
        float threshold = isFleeing ? (enemyDetectRange + safetyMargin) : enemyDetectRange;

        foreach (Transform enemy in allEnemies) {
            if (enemy == null) continue;
            float dist = Vector2.Distance(transform.position, enemy.position);
            
            if (dist < threshold) {
                threatCount++;
                Vector2 runDir = (transform.position - enemy.position).normalized;
                totalFleeVector += runDir;
            }
        }

        if (threatCount > 0) {
            if (totalFleeVector == Vector2.zero) 
                totalFleeVector = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));

            Vector2 bestDir = totalFleeVector.normalized;
            if (!IsBlocked(bestDir)) return bestDir;

            Vector2 p1 = new Vector2(-bestDir.y, bestDir.x);
            Vector2 p2 = new Vector2(bestDir.y, -bestDir.x);
            if (!IsBlocked(p1)) return p1;
            if (!IsBlocked(p2)) return p2;

            return FindOpenDirection(bestDir);
        }

        // Düşman yoksa ama duvarla burun burunaysak (Duvar Refleksi)
        Vector2 currentDir = rb.velocity.normalized;
        if (currentDir != Vector2.zero && IsBlocked(currentDir)) return FindOpenDirection(currentDir);

        return Vector2.zero;
    }

    public bool IsBlocked(Vector2 dir) {
        return Physics2D.Raycast(transform.position, dir, rayDistance, LayerMask.GetMask("Obstacle"));
    }

    Vector2 FindOpenDirection(Vector2 blockedDir) {
        Vector2[] dirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        foreach (var d in dirs) {
            if (d == blockedDir) continue;
            if (!IsBlocked(d)) return d;
        }
        return Vector2.zero;
    }
}