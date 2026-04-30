using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UnstablePlatform : MonoBehaviour
{
    public float triggerTime = 5f, warningDuration = 2f, detectionRadius = 2f;
    public float bounceForce = 12f, tiltForce = 8f, sinkDepth = 0.5f, vibrationForce = 15f, explosionForce = 20f;

    private Dictionary<int, float> timers = new Dictionary<int, float>();
    private Renderer rend;
    private Color origColor;
    private Vector3 origPos;
    private Quaternion origRot;
    private bool reacting = false, warning = false;

    // ФИX: кэшируем ссылку на GameManager, чтобы брать массив игроков оттуда.
    // FindObjectsOfType<PlayerController>() каждый кадр — дорогая операция O(n) по всей сцене.
    private GameManager gameManager;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        origPos = transform.position;
        origRot = transform.rotation;
        rend = GetComponent<Renderer>();
        if (rend == null) rend = GetComponentInChildren<Renderer>();
        if (rend != null && rend.material != null) origColor = rend.material.color;
        var r = GetComponent<Renderer>();
        if (r == null) r = GetComponentInChildren<Renderer>();
        if (r != null)
        {
            float m = Mathf.Max(r.bounds.size.x, r.bounds.size.y, r.bounds.size.z);
            detectionRadius = Mathf.Max(detectionRadius, m * 0.6f);
        }
    }

    void Update()
    {
        if (reacting) return;

        // Берём массив игроков из GameManager — без дорогого поиска каждый кадр
        PlayerController[] all = (gameManager != null)
            ? gameManager.players
            : FindObjectsOfType<PlayerController>();

        bool anyW = false;
        var rem = new List<int>();

        foreach (var p in all)
        {
            if (p == null || p.IsEliminated() || !p.gameObject.activeSelf) continue;
            int id = p.playerNumber;
            float dist = Vector3.Distance(transform.position, p.transform.position);
            bool on = dist < detectionRadius && p.transform.position.y > transform.position.y - 0.5f;

            if (on)
            {
                if (!timers.ContainsKey(id)) timers[id] = Time.time;
                float t = Time.time - timers[id];
                if (t >= triggerTime - warningDuration && t < triggerTime) anyW = true;
                if (t >= triggerTime) { Trigger(p.gameObject); rem.Add(id); }
            }
            else
            {
                if (timers.ContainsKey(id)) rem.Add(id);
            }
        }

        foreach (var id in rem) timers.Remove(id);

        if (anyW && !warning)
        {
            warning = true;
            if (rend != null && rend.material != null) rend.material.color = Color.red;
        }
        else if (!anyW && warning)
        {
            warning = false;
            ResetVisual();
        }

        if (warning)
        {
            float s = 0.05f;
            transform.position = origPos + new Vector3(Random.Range(-s, s), Random.Range(-s, s), Random.Range(-s, s));
        }
    }

    void Trigger(GameObject p)
    {
        switch (Random.Range(0, 5))
        {
            case 0: StartCoroutine(BounceDelayed(p)); break;
            case 1: StartCoroutine(Tilt(p)); break;
            case 2: StartCoroutine(Sink()); break;
            case 3: StartCoroutine(Vib(p)); break;
            case 4: StartCoroutine(ExplodeDelayed(p)); break;
        }
    }

    // ФИX: Bounce и Explode теперь используют корутину с reacting=true.
    // Раньше они были мгновенными и reacting не выставляли, из-за чего платформа
    // могла запускать следующую реакцию уже в следующем кадре (цепные выбросы).
    IEnumerator BounceDelayed(GameObject p)
    {
        reacting = true;
        var rb = p.GetComponent<Rigidbody>();
        if (rb != null) rb.AddForce(Vector3.up * bounceForce, ForceMode.Impulse);
        ResetVisual();
        yield return new WaitForSeconds(0.5f);
        reacting = false;
    }

    IEnumerator Tilt(GameObject p)
    {
        reacting = true;
        float rx = Random.Range(-30f, 30f), rz = Random.Range(-30f, 30f);
        var tr = origRot * Quaternion.Euler(rx, 0, rz);
        float t = 0;
        while (t < 0.5f) { t += Time.deltaTime; transform.rotation = Quaternion.Slerp(origRot, tr, t / 0.5f); yield return null; }
        var rb = p.GetComponent<Rigidbody>();
        if (rb != null) rb.AddForce(new Vector3(rx, 0, rz).normalized * tiltForce + Vector3.up * 3f, ForceMode.Impulse);
        yield return new WaitForSeconds(0.5f);
        t = 0;
        while (t < 0.5f) { t += Time.deltaTime; transform.rotation = Quaternion.Slerp(tr, origRot, t / 0.5f); yield return null; }
        transform.rotation = origRot;
        ResetVisual();
        reacting = false;
    }

    IEnumerator Sink()
    {
        reacting = true;
        var sp = origPos - Vector3.up * sinkDepth;
        float t = 0;
        while (t < 0.2f) { t += Time.deltaTime; transform.position = Vector3.Lerp(origPos, sp, t / 0.2f); yield return null; }
        yield return new WaitForSeconds(0.3f);
        t = 0;
        while (t < 0.3f) { t += Time.deltaTime; transform.position = Vector3.Lerp(sp, origPos, t / 0.3f); yield return null; }
        transform.position = origPos;
        ResetVisual();
        reacting = false;
    }

    IEnumerator Vib(GameObject p)
    {
        reacting = true;
        float t = 0;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            float s = 0.2f;
            transform.position = origPos + new Vector3(Random.Range(-s, s), 0, Random.Range(-s, s));
            yield return null;
        }
        transform.position = origPos;
        var rb = p.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce(new Vector3(Random.Range(-1f, 1f), 0.5f, Random.Range(-1f, 1f)).normalized * vibrationForce, ForceMode.Impulse);
        ResetVisual();
        reacting = false;
    }

    IEnumerator ExplodeDelayed(GameObject p)
    {
        reacting = true;
        var rb = p.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce(new Vector3(Random.Range(-0.5f, 0.5f), 1, Random.Range(-0.5f, 0.5f)).normalized * explosionForce, ForceMode.Impulse);
        ResetVisual();
        yield return new WaitForSeconds(0.5f);
        reacting = false;
    }

    void ResetVisual()
    {
        transform.position = origPos;
        if (rend != null && rend.material != null) rend.material.color = origColor;
    }

    public void ResetTimers()
    {
        timers.Clear();
        reacting = false;
        warning = false;
        StopAllCoroutines();
        transform.position = origPos;
        transform.rotation = origRot;
        if (rend != null && rend.material != null) rend.material.color = origColor;
    }
}