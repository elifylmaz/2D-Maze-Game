using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A* algoritması için grid yönetimi
/// Labirenti grid'e dönüştürür ve pathfinding yapar
/// </summary>
public class GridManager_sc : MonoBehaviour {
    [Header("Grid Ayarları")]
    public LayerMask obstacleMask;              // "Obstacle" layer
    public Vector2 gridWorldSize = new Vector2(20f, 11f); // Harita boyutu
    public float nodeRadius = 0.5f;

    Node_sc[,] grid;
    float nodeDiameter;
    int gridSizeX, gridSizeY;

    // MazeGenerator labirenti oluşturduktan sonra çağırır
    public void CreateGrid() {
        nodeDiameter = nodeRadius * 2f;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        
        grid = new Node_sc[gridSizeX, gridSizeY];
        
        // Haritanın sol alt köşesi
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2f - Vector3.up * gridWorldSize.y / 2f;

        for (int x = 0; x < gridSizeX; x++) {
            for (int y = 0; y < gridSizeY; y++) {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.up * (y * nodeDiameter + nodeRadius);
                
                // Duvara çarpıyor mu kontrol et
                bool walkable = !Physics2D.OverlapCircle(worldPoint, nodeRadius - 0.1f, obstacleMask);
                grid[x, y] = new Node_sc(walkable, worldPoint, x, y);
            }
        }
    }

    // Dünya pozisyonundan grid node'unu bul
    public Node_sc NodeFromWorldPoint(Vector3 worldPosition) {
        float percentX = (worldPosition.x + gridWorldSize.x / 2f) / gridWorldSize.x;
        float percentY = (worldPosition.y + gridWorldSize.y / 2f) / gridWorldSize.y;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        return grid[x, y];
    }

    // A* Pathfinding
    public List<Node_sc> GetPath(Vector3 startPos, Vector3 targetPos) {
        Node_sc startNode = NodeFromWorldPoint(startPos);
        Node_sc targetNode = NodeFromWorldPoint(targetPos);

        List<Node_sc> openSet = new List<Node_sc>();
        HashSet<Node_sc> closedSet = new HashSet<Node_sc>();
        openSet.Add(startNode);

        while (openSet.Count > 0) {
            // En düşük F cost'lu node'u seç
            Node_sc currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++) {
                if (openSet[i].fCost < currentNode.fCost || 
                   (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)) {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            // Hedefe ulaştık
            if (currentNode == targetNode) {
                return RetracePath(startNode, targetNode);
            }

            // Komşuları kontrol et
            foreach (Node_sc neighbor in GetNeighbors(currentNode)) {
                if (!neighbor.isWalkable || closedSet.Contains(neighbor)) continue;

                int newCost = currentNode.gCost + GetDistance(currentNode, neighbor);
                if (newCost < neighbor.gCost || !openSet.Contains(neighbor)) {
                    neighbor.gCost = newCost;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor)) {
                        openSet.Add(neighbor);
                    }
                }
            }
        }
        return null; // Yol bulunamadı
    }

    // Yolu geri izle
    List<Node_sc> RetracePath(Node_sc startNode, Node_sc endNode) {
        List<Node_sc> path = new List<Node_sc>();
        Node_sc currentNode = endNode;

        while (currentNode != startNode) {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse();
        return path;
    }

    // Komşu node'ları bul (sadece 4 yön: yukarı, aşağı, sol, sağ)
    List<Node_sc> GetNeighbors(Node_sc node) {
        List<Node_sc> neighbors = new List<Node_sc>();
        
        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                if (x == 0 && y == 0) continue; // Kendisi
                if (Mathf.Abs(x) == 1 && Mathf.Abs(y) == 1) continue; // Çapraz hareket yok

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY) {
                    neighbors.Add(grid[checkX, checkY]);
                }
            }
        }
        return neighbors;
    }

    // İki node arası mesafe
    int GetDistance(Node_sc a, Node_sc b) {
    int dstX = Mathf.Abs(a.gridX - b.gridX);
    int dstY = Mathf.Abs(a.gridY - b.gridY);
    
    // Çapraz gidemediğimiz için maliyet basitçe X + Y farkıdır.
    // 10 ile çarpıyoruz ki tam sayı olsun.
    return 10 * (dstX + dstY);
}
    
    // Gizmo ile grid'i görselleştir
    void OnDrawGizmos() {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, gridWorldSize.y, 1));
        
        if (grid != null) {
            foreach (Node_sc n in grid) {
                Gizmos.color = n.isWalkable ? new Color(1, 1, 1, 0.1f) : new Color(1, 0, 0, 0.4f);
                Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - 0.1f));
            }
        }
    }
}