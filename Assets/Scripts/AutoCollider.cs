using UnityEngine;
using System.Collections.Generic;

public class AutoCollider : MonoBehaviour
{
    public float updateInterval = 2f; public bool useMeshCollider = true;
    public string[] excludeNames = { "Player", "Tiger", "Dog", "Kitty", "Cat", "Pinguin", "Penguin", "FallZone", "GameManager", "UIManager", "Canvas", "Camera", "Light" };
    public string[] excludeTags = { "Player", "FallZone" };
    private float timer = 0f; private HashSet<GameObject> done = new HashSet<GameObject>();

    void Start() { Run(); }
    void Update() { timer += Time.deltaTime; if (timer >= updateInterval) { timer = 0; Run(); } }

    void Run()
    {
        int added = 0;
        foreach (var r in FindObjectsOfType<MeshRenderer>())
        {
            var o = r.gameObject; if (done.Contains(o)) { continue; }
            if (Excl(o)) { done.Add(o); continue; }
            if (o.GetComponent<Collider>() != null) { done.Add(o); continue; }
            var mf = o.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) { done.Add(o); continue; }
            if (useMeshCollider) { var mc = o.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; mc.convex = false; }
            else o.AddComponent<BoxCollider>();
            added++; done.Add(o);
        }
        if (added > 0) Debug.Log("[AutoCollider] Добавлено: " + added);
    }

    bool Excl(GameObject o)
    {
        foreach (var tag in excludeTags) { if (!string.IsNullOrEmpty(tag)) { try { if (o.CompareTag(tag)) return true; } catch { } } }
        var cur = o.transform; while (cur != null) { foreach (var e in excludeNames) if (!string.IsNullOrEmpty(e) && cur.name.Contains(e)) return true; cur = cur.parent; }
        return false;
    }
}