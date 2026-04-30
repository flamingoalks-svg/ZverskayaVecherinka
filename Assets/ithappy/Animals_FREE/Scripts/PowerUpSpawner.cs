using UnityEngine;
using System.Collections.Generic;

public class PowerUpSpawner : MonoBehaviour
{
    public int maxPowerUps = 5; public float spawnInterval = 8f, floatHeight = 2f;
    public string[] excludeNames = { "Plane", "STENA", "Cube", "Player", "Tiger", "Dog", "Kitty", "Cat", "Pinguin", "Penguin", "Camera", "Light", "GameManager", "UIManager", "AutoCollider", "AutoUnstable", "PowerUp", "Canvas", "door", "MainMenu" };
    private float timer = 0f; private List<Vector3> positions = new List<Vector3>();

    void Start() { FindPositions(); for (int i = 0; i < Mathf.Min(3, maxPowerUps); i++) Spawn(); }
    void Update() { timer += Time.deltaTime; if (timer >= spawnInterval) { timer = 0; if (FindObjectsOfType<PowerUp>().Length < maxPowerUps) Spawn(); } }

    void FindPositions()
    {
        positions.Clear();
        foreach (var c in FindObjectsOfType<Collider>()) { if (c.isTrigger || Excl(c.gameObject)) continue; positions.Add(c.bounds.center + Vector3.up * (c.bounds.extents.y + floatHeight)); }
        Debug.Log("[PowerUpSpawner] Точек спавна: " + positions.Count);
    }

    void Spawn()
    {
        if (positions.Count == 0) return;
        var pos = positions[Random.Range(0, positions.Count)];
        var type = (PowerUp.PowerUpType)Random.Range(0, 5);
        var o = new GameObject("PowerUp_" + type); o.transform.position = pos;
        o.AddComponent<PowerUp>().type = type;
    }

    bool Excl(GameObject o) { var cur = o.transform; while (cur != null) { foreach (var e in excludeNames) if (!string.IsNullOrEmpty(e) && cur.name.Contains(e)) return true; cur = cur.parent; } return false; }
}