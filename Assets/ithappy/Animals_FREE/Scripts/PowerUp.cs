using UnityEngine;

public class PowerUp : MonoBehaviour
{
    public enum PowerUpType { Flight, Freeze, Invisibility, Teleport, Giant }
    public PowerUpType type;

    public float flightDuration = 6f, flightLiftForce = 3f;
    public float freezeDuration = 2.5f;
    public float invisDuration = 3f;
    public float giantDuration = 4f, giantScale = 2f;

    private float bobSpeed = 2f, bobHeight = 0.3f, spinSpeed = 90f;
    private Vector3 startPos;
    private bool collected = false;

    private static readonly Color[] colors =
    {
        new Color(0.3f, 0.8f, 1f),
        new Color(0.5f, 0.9f, 1f),
        new Color(0.7f, 0.7f, 0.9f),
        new Color(1f,  0.5f, 1f),
        new Color(1f,  0.8f, 0.2f)
    };

    void Start()
    {
        startPos = transform.position;
        var vis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        vis.transform.SetParent(transform, false);
        vis.transform.localScale = Vector3.one * 0.6f;
        var vc = vis.GetComponent<Collider>(); if (vc != null) Destroy(vc);
        var r = vis.GetComponent<Renderer>();
        if (r != null)
        {
            r.material.color = colors[(int)type];
            r.material.EnableKeyword("_EMISSION");
            r.material.SetColor("_EmissionColor", colors[(int)type] * 0.5f);
        }
        var col = gameObject.GetComponent<SphereCollider>();
        if (col == null) col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 1.2f;
    }

    void Update()
    {
        if (collected) return;
        transform.position = new Vector3(startPos.x, startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight, startPos.z);
        transform.Rotate(0, spinSpeed * Time.deltaTime, 0);
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        var p = other.GetComponent<PlayerController>();
        if (p == null || p.IsEliminated()) return;
        collected = true;
        Activate(p);
        Destroy(gameObject, 0.1f);
    }

    void Activate(PlayerController p)
    {
        // ФИX: корутины запускаются на PlayerController (p), а не на самом PowerUp.
        // PowerUp уничтожается через 0.1 секунды — если корутина на нём,
        // она останавливается через 0.1с, не завершив эффект (полёт 6с, заморозка 2.5с и т.д.)
        // Игрок остаётся в сломанном состоянии: isFlying=true вечно, kinematic вечно и т.п.
        // Запуск на PlayerController гарантирует, что корутина живёт столько, сколько нужно.
        switch (type)
        {
            case PowerUpType.Flight: p.StartCoroutine(Fly(p)); break;
            case PowerUpType.Freeze: Freeze(p); break;
            case PowerUpType.Invisibility: p.StartCoroutine(Invis(p)); break;
            case PowerUpType.Teleport: Teleport(p); break;
            case PowerUpType.Giant: p.StartCoroutine(Giant(p)); break;
        }
    }

    System.Collections.IEnumerator Fly(PlayerController p)
    {
        var rb = p.GetComponent<Rigidbody>();
        if (rb == null) yield break;

        p.isFlying = true;
        rb.useGravity = false;

        // ФИX: обнуляем ВСЮ скорость (включая Y), а не только X и Z.
        // Иначе при подборе в прыжке существующая вертикальная скорость
        // складывается с impulse и игрок улетает слишком высоко.
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(Vector3.up * flightLiftForce, ForceMode.Impulse);

        float targetY = p.transform.position.y + 2.5f;
        float elapsed = 0f;

        while (elapsed < flightDuration && p != null && p.isFlying)
        {
            elapsed += Time.deltaTime;
            if (rb != null && !p.IsEliminated())
            {
                // Плавный hover: стремимся к targetY, не допуская резких скачков
                float diff = targetY - p.transform.position.y;
                float targetVelY = Mathf.Clamp(diff * 4f, -4f, 4f);
                rb.linearVelocity = new Vector3(
                    rb.linearVelocity.x,
                    Mathf.Lerp(rb.linearVelocity.y, targetVelY, 8f * Time.deltaTime),
                    rb.linearVelocity.z
                );
            }
            yield return null;
        }

        if (p != null) { p.isFlying = false; if (rb != null) rb.useGravity = true; }
    }

    void Freeze(PlayerController player)
    {
        PlayerController nearest = null;
        float nd = float.MaxValue;
        foreach (var p in FindObjectsOfType<PlayerController>())
        {
            if (p == player || p.IsEliminated()) continue;
            float d = Vector3.Distance(player.transform.position, p.transform.position);
            if (d < nd) { nd = d; nearest = p; }
        }
        // Запускаем корутину на замораживаемом игроке
        if (nearest != null) nearest.StartCoroutine(FreezeEffect(nearest));
    }

    System.Collections.IEnumerator FreezeEffect(PlayerController t)
    {
        var rb = t.GetComponent<Rigidbody>();
        if (rb == null) yield break;
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        var r = t.GetComponentInChildren<Renderer>();
        Color orig = Color.white;
        if (r != null) { orig = r.material.color; r.material.color = new Color(0.5f, 0.8f, 1f); }
        yield return new WaitForSeconds(freezeDuration);
        if (rb != null) rb.isKinematic = false;
        if (r != null) r.material.color = orig;
    }

    System.Collections.IEnumerator Invis(PlayerController p)
    {
        var rs = p.GetComponentsInChildren<Renderer>();
        var oc = new Color[rs.Length];
        for (int i = 0; i < rs.Length; i++)
        {
            oc[i] = rs[i].material.color;
            var c = oc[i]; c.a = 0.15f; rs[i].material.color = c;
            rs[i].material.SetFloat("_Mode", 3);
            rs[i].material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            rs[i].material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            rs[i].material.SetInt("_ZWrite", 0);
            rs[i].material.EnableKeyword("_ALPHABLEND_ON");
            rs[i].material.renderQueue = 3000;
        }
        yield return new WaitForSeconds(invisDuration);
        for (int i = 0; i < rs.Length; i++)
        {
            if (rs[i] == null) continue;
            rs[i].material.color = oc[i];
            rs[i].material.SetFloat("_Mode", 0);
            rs[i].material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            rs[i].material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            rs[i].material.SetInt("_ZWrite", 1);
            rs[i].material.renderQueue = -1;
        }
    }

    void Teleport(PlayerController p)
    {
        var valid = new System.Collections.Generic.List<Vector3>();
        foreach (var c in FindObjectsOfType<Collider>())
        {
            if (c.isTrigger || c.GetComponent<PlayerController>() != null) continue;
            var n = c.gameObject.name;
            if (n.Contains("Plane") || n.Contains("STENA") || n.Contains("Cube")) continue;
            valid.Add(c.bounds.center + Vector3.up * (c.bounds.extents.y + 1.5f));
        }
        if (valid.Count > 0)
        {
            p.transform.position = valid[Random.Range(0, valid.Count)];
            var rb = p.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
        }
    }

    System.Collections.IEnumerator Giant(PlayerController p)
    {
        var os = p.transform.localScale;
        float op = p.pushForce, or2 = p.pushRadius;
        p.transform.localScale = os * giantScale;
        p.pushForce = op * 3f;
        p.pushRadius = or2 * 2f;
        yield return new WaitForSeconds(giantDuration);
        // Сбрасываем всегда, даже если игрок выбыл — ResetPlayer тоже сбросит,
        // но лучше не оставлять состояние висеть
        if (p != null)
        {
            p.transform.localScale = os;
            p.pushForce = op;
            p.pushRadius = or2;
        }
    }
}