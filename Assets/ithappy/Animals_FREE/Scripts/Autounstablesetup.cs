using UnityEngine;
using System.Collections.Generic;

public class AutoUnstableSetup : MonoBehaviour
{
    public float triggerTime = 5f, updateInterval = 3f;
    public string[] excludeNames = { "Player", "Tiger", "Dog", "Kitty", "Cat", "Pinguin", "Penguin", "FallZone", "GameManager", "UIManager", "Canvas", "Camera", "Light", "Plane", "STENA", "Cube", "MainMenu", "PowerUp" };
    private float timer = 0f; private HashSet<GameObject> done = new HashSet<GameObject>();

    void Start() { Setup(); }
    void Update() { timer += Time.deltaTime; if (timer >= updateInterval) { timer = 0; Setup(); } }

    void Setup()
    {
        int added = 0;
        foreach (var c in FindObjectsOfType<Collider>())
        {
            var o = c.gameObject; if (done.Contains(o) || Excl(o) || o.GetComponent<UnstablePlatform>() != null || c.isTrigger) continue;
            o.AddComponent<UnstablePlatform>().triggerTime = triggerTime; added++; done.Add(o);
        }
        if (added > 0) Debug.Log("[AutoUnstableSetup] Добавлено: " + added);
    }

    bool Excl(GameObject o) { var cur = o.transform; while (cur != null) { foreach (var e in excludeNames) if (!string.IsNullOrEmpty(e) && cur.name.Contains(e)) return true; cur = cur.parent; } return false; }
}