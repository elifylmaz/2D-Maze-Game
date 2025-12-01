using UnityEngine;
using System.Collections.Generic;

public class AgentAStar_sc : MonoBehaviour {
    [Header("A* Ayarları")]
    public float replanInterval = 2.0f;
    public float waypointReachDistance = 0.15f; 

    private List<Node_sc> currentPath; // Gizmo için buna dışarıdan erişmemiz lazım
    private int pathIndex = 0;
    private float lastReplanTime = 0;
    private bool isFollowingPath = false;
    private GridManager_sc gridManager;

    public void Setup(GridManager_sc _grid) {
        gridManager = _grid;
    }

    public void ForceReplan() {
        lastReplanTime = 0; 
    }

    // Gizmos çizimi için Path'i Brain'e gönderen fonksiyon
    public List<Node_sc> GetCurrentPath() {
        return currentPath;
    }

    public bool IsActive() { return isFollowingPath; }

    public Vector2 GetPathDirection(Transform exitTransform) {
        if (gridManager == null || exitTransform == null) return Vector2.zero;

        if (Time.time - lastReplanTime > replanInterval || currentPath == null || pathIndex >= currentPath.Count) {
            currentPath = gridManager.GetPath(transform.position, exitTransform.position);
            pathIndex = 0;
            lastReplanTime = Time.time;
            isFollowingPath = (currentPath != null && currentPath.Count > 0);
        }

        if (!isFollowingPath || currentPath == null || pathIndex >= currentPath.Count) return Vector2.zero;

        Vector2 targetWaypoint = currentPath[pathIndex].worldPosition;
        float dist = Vector2.Distance(transform.position, targetWaypoint);

        if (dist < waypointReachDistance) {
            pathIndex++;
            if (pathIndex >= currentPath.Count) {
                isFollowingPath = false;
                return Vector2.zero;
            }
            targetWaypoint = currentPath[pathIndex].worldPosition;
        }

        return (targetWaypoint - (Vector2)transform.position).normalized;
    }
}