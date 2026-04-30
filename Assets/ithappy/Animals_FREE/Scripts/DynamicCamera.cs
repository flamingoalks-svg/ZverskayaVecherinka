using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DynamicCamera : MonoBehaviour
{
    [Header("Игроки")]
    public PlayerController[] players;

    [Header("Общая камера")]
    public float minHeight = 4f;
    public float maxHeight = 14f;
    public float minDistance = 3f;
    public float maxDistance = 10f;

    [Header("Мышь")]
    public float mouseSensitivity = 3f;
    public float zoomSpeed = 3f;

    [Header("Split-screen")]
    public float splitDistance = 18f;
    public float splitHeight = 3f;
    public float splitFollowDist = 5f;

    [Header("Плавность")]
    public float positionSmooth = 4f;
    public float rotationSmooth = 3f;

    private Camera mainCam;
    private Camera[] splitCameras;
    private bool isSplitMode = false;
    private Vector3[] lastFwd;
    private float mouseYaw = 0f, mousePitch = 40f, zoomOffset = 0f;

    void Start()
    {
        mainCam = GetComponent<Camera>(); mainCam.fieldOfView = 55f;
        splitCameras = new Camera[4]; splitCameras[0] = mainCam;
        lastFwd = new Vector3[4]; for (int i = 0; i < 4; i++) lastFwd[i] = Vector3.forward;
        for (int i = 1; i < 4; i++)
        {
            var o = new GameObject("SplitCam_" + (i + 1)); o.transform.SetParent(transform.parent);
            var c = o.AddComponent<Camera>(); c.fieldOfView = 60f; c.nearClipPlane = mainCam.nearClipPlane;
            c.farClipPlane = mainCam.farClipPlane; c.backgroundColor = mainCam.backgroundColor;
            c.clearFlags = mainCam.clearFlags; c.enabled = false; splitCameras[i] = c;
        }
        SetSingle();
    }

    void LateUpdate()
    {
        if (players == null || players.Length == 0) return;
        var alive = players.Where(p => p != null && !p.IsEliminated() && p.gameObject.activeSelf).ToList();
        if (alive.Count == 0) return;

        for (int i = 0; i < alive.Count && i < lastFwd.Length; i++)
        { var f = alive[i].transform.forward; f.y = 0; if (f.magnitude > 0.1f) lastFwd[i] = Vector3.Lerp(lastFwd[i], f.normalized, rotationSmooth * Time.deltaTime); }

        if (!isSplitMode) HandleMouse();
        float md = MaxDist(alive);

        if (alive.Count >= 2 && md > splitDistance) { if (!isSplitMode) SetSplit(alive); UpdateSplitCams(alive); }
        else { if (isSplitMode) SetSingle(); FollowAll(alive); }
    }

    void HandleMouse()
    {
        float s = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(s) > 0.01f) { zoomOffset -= s * zoomSpeed; zoomOffset = Mathf.Clamp(zoomOffset, -8f, 10f); }
        if (Input.GetMouseButton(1)) { mouseYaw += Input.GetAxis("Mouse X") * mouseSensitivity; mousePitch -= Input.GetAxis("Mouse Y") * mouseSensitivity; mousePitch = Mathf.Clamp(mousePitch, 10f, 80f); }
        else { mouseYaw = Mathf.Lerp(mouseYaw, 0f, 2f * Time.deltaTime); mousePitch = Mathf.Lerp(mousePitch, 40f, 2f * Time.deltaTime); }
    }

    void FollowAll(List<PlayerController> alive)
    {
        Vector3 center = Vector3.zero; foreach (var p in alive) center += p.transform.position; center /= alive.Count;
        float sp = MaxDist(alive); float t = Mathf.Clamp01(sp / splitDistance);
        float h = Mathf.Lerp(minHeight, maxHeight, t) + zoomOffset * 0.5f;
        float d = Mathf.Lerp(minDistance, maxDistance, t) + zoomOffset;
        h = Mathf.Max(h, 2f); d = Mathf.Max(d, 2f);
        var rot = Quaternion.Euler(mousePitch, mouseYaw, 0);
        var off = rot * (Vector3.back * d) + Vector3.up * h;
        mainCam.transform.position = Vector3.Lerp(mainCam.transform.position, center + off, positionSmooth * Time.deltaTime);
        var lt = center + Vector3.up * 0.5f;
        mainCam.transform.rotation = Quaternion.Slerp(mainCam.transform.rotation, Quaternion.LookRotation(lt - mainCam.transform.position), rotationSmooth * Time.deltaTime);
        mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, Mathf.Lerp(50f, 70f, t), 2f * Time.deltaTime);
    }

    void FollowBehind(Camera cam, PlayerController p, int i)
    {
        var fwd = lastFwd[i]; if (fwd.magnitude < 0.1f) fwd = Vector3.forward;
        var yawRot = Quaternion.Euler(0, p.cameraYawOffset, 0);
        var back = yawRot * (-fwd);
        var desired = p.transform.position + back * splitFollowDist + Vector3.up * splitHeight;
        cam.transform.position = Vector3.Lerp(cam.transform.position, desired, positionSmooth * Time.deltaTime);
        var lt = p.transform.position + Vector3.up * 0.8f;
        cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(lt - cam.transform.position), rotationSmooth * Time.deltaTime);
    }

    void UpdateSplitCams(List<PlayerController> alive)
    {
        for (int i = 0; i < alive.Count && i < splitCameras.Length; i++)
        {
            if (splitCameras[i] != null)
            {
                FollowBehind(splitCameras[i], alive[i], i);
                // Назначаем каждому игроку его личную камеру для camera-relative движения
                alive[i].playerCamera = splitCameras[i];
            }
        }
    }

    void SetSingle()
    {
        isSplitMode = false; mainCam.rect = new Rect(0, 0, 1, 1); mainCam.enabled = true;
        for (int i = 1; i < splitCameras.Length; i++) if (splitCameras[i] != null) splitCameras[i].enabled = false;
        // В режиме общей камеры все игроки используют основную камеру
        if (players != null) foreach (var p in players) if (p != null) p.playerCamera = mainCam;
    }

    void SetSplit(List<PlayerController> alive)
    {
        isSplitMode = true; int c = Mathf.Min(alive.Count, 4);
        for (int i = 0; i < splitCameras.Length; i++) if (splitCameras[i] != null) splitCameras[i].enabled = false;
        Rect[] vp; switch (c) { case 2: vp = new[] { new Rect(0, 0, 0.5f, 1), new Rect(0.5f, 0, 0.5f, 1) }; break; case 3: vp = new[] { new Rect(0, 0.5f, 0.5f, 0.5f), new Rect(0.5f, 0.5f, 0.5f, 0.5f), new Rect(0.25f, 0, 0.5f, 0.5f) }; break; default: vp = new[] { new Rect(0, 0.5f, 0.5f, 0.5f), new Rect(0.5f, 0.5f, 0.5f, 0.5f), new Rect(0, 0, 0.5f, 0.5f), new Rect(0.5f, 0, 0.5f, 0.5f) }; break; }
        for (int i = 0; i < c; i++) { if (splitCameras[i] != null) { splitCameras[i].enabled = true; splitCameras[i].rect = vp[i]; splitCameras[i].fieldOfView = 60f; } }
    }

    float MaxDist(List<PlayerController> a) { float m = 0; for (int i = 0; i < a.Count; i++) for (int j = i + 1; j < a.Count; j++) { float d = Vector3.Distance(a[i].transform.position, a[j].transform.position); if (d > m) m = d; } return m; }
}