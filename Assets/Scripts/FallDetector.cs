using UnityEngine;

public class FallDetector : MonoBehaviour
{
    public float fallThreshold = -5f;
    public string[] floorNames = { "Plane", "Floor", "Пол", "Ground" };

    // ФИX: Иммунитет после спавна.
    // Без него: SetActive(true) будит Rigidbody на старой позиции (на полу),
    // OnCollisionEnter с "Plane" срабатывает сразу → игрок снова умирает до того
    // как transform.position переставит его на спавн-точку.
    public float spawnImmunityDuration = 1.5f;
    private float spawnTime = -999f;

    private PlayerController pc;

    void Awake()
    {
        pc = GetComponent<PlayerController>();
    }

    void OnEnable()
    {
        // Каждый раз когда объект активируется — запоминаем время
        spawnTime = Time.time;
    }

    bool IsImmune()
    {
        return Time.time - spawnTime < spawnImmunityDuration;
    }

    void Update()
    {
        if (IsImmune()) return;
        if (transform.position.y < fallThreshold && pc != null && !pc.IsEliminated())
            pc.Eliminate();
    }

    void OnCollisionEnter(Collision col)
    {
        // Иммунитет защищает от ложного срабатывания при спавне
        if (IsImmune()) return;
        if (pc == null || pc.IsEliminated()) return;

        string n = col.gameObject.name;
        foreach (string fn in floorNames)
            if (n.Contains(fn)) { pc.Eliminate(); return; }

        try { if (col.gameObject.CompareTag("FallZone")) pc.Eliminate(); } catch { }
    }
}