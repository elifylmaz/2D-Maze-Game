using UnityEngine;

public class Node_sc {
    public bool isWalkable;      // Geçilebilir mi?
    public Vector3 worldPosition; // Dünya pozisyonu
    public int gridX;            // Grid X koordinatı
    public int gridY;            // Grid Y koordinatı

    public int gCost;            // Başlangıçtan bu düğüme maliyet
    public int hCost;            // Bu düğümden hedefe tahmini maliyet
    public Node_sc parent;       // Yolu geri izlemek için

    public Node_sc(bool _walkable, Vector3 _worldPos, int _gridX, int _gridY) {
        isWalkable = _walkable;
        worldPosition = _worldPos;
        gridX = _gridX;
        gridY = _gridY;
    }

    // F maliyet: G + H
    public int fCost {
        get { return gCost + hCost; }
    }
}