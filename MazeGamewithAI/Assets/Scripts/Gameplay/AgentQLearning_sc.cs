using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class AgentQLearning_sc : MonoBehaviour {
    private Dictionary<string, float[]> qTable = new Dictionary<string, float[]>();
    private AgentPerceptron_sc perceptron; 

    public void Setup(AgentPerceptron_sc _perceptron) {
        perceptron = _perceptron;
        LoadBestQTable();
    }

    public Vector2 GetTacticDirection(Transform exitTransform) {
        if (qTable.Count == 0) return Vector2.zero;

        string state = GetState(exitTransform);
        if (!qTable.ContainsKey(state)) return Vector2.zero;

        float[] values = qTable[state];
        int bestAction = 0;
        for (int i = 1; i < 4; i++) if (values[i] > values[bestAction]) bestAction = i;

        Vector2 action = GetActionVector(bestAction);
        if (perceptron.IsBlocked(action)) return Vector2.zero;
        return action;
    }

    string GetState(Transform exit) {
        string s = "";
        if (exit != null) {
            Vector2 d = (exit.position - transform.position).normalized;
            s += (Mathf.Abs(d.x) > Mathf.Abs(d.y)) ? ((d.x>0)?"R_":"L_") : ((d.y>0)?"U_":"D_");
        } else s += "NoExit_";
        s += (perceptron.IsBlocked(Vector2.up)?"U":"_") + (perceptron.IsBlocked(Vector2.down)?"D":"_") + 
             (perceptron.IsBlocked(Vector2.left)?"L":"_") + (perceptron.IsBlocked(Vector2.right)?"R":"_");
        return s;
    }

    Vector2 GetActionVector(int a) { return a == 0 ? Vector2.up : (a == 1 ? Vector2.down : (a == 2 ? Vector2.left : Vector2.right)); }

    void LoadBestQTable() { 
        string path = Application.dataPath + "/Scripts/qtable_best.json"; 
        if (!File.Exists(path)) path = Application.dataPath + "/Scripts/qtable_save.json";

        if (File.Exists(path)) {
            QTableData data = JsonUtility.FromJson<QTableData>(File.ReadAllText(path));
            qTable.Clear();
            foreach (var e in data.entries) qTable[e.state] = e.values;
        }
    }
    
    [System.Serializable] public class QTableData { public List<QTableEntry> entries = new List<QTableEntry>(); }
    [System.Serializable] public class QTableEntry { public string state; public float[] values; }
}