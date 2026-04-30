using UnityEngine;

public class InteractiveDoor : MonoBehaviour
{
    public float openAngle = 90f, openSpeed = 180f, autoCloseDelay = 3f, interactionRadius = 3f;
    public bool showHint = true;

    private bool isOpen = false, isMoving = false; private float currentAngle = 0f, targetAngle = 0f, closeTimer = 0f;
    private Quaternion closedRot; private GameObject hintCanvas; private UnityEngine.UI.Text hintText;
    private KeyCode[] keys = { KeyCode.F, KeyCode.RightShift, KeyCode.H, KeyCode.Keypad1 };

    void Start() { closedRot = transform.rotation; if (showHint) CreateHint(); }

    void Update()
    {
        var np = FindNearest(); bool near = np != null;
        if (hintCanvas != null) { hintCanvas.SetActive(near && !isOpen); if (near && !isOpen && hintText != null) { string[] kn = { "F", "RShift", "H", "Num1" }; hintText.text = "ֽאזלט [" + kn[np.playerNumber - 1] + "]"; } }
        if (near && np.playerNumber >= 1 && np.playerNumber <= 4 && Input.GetKeyDown(keys[np.playerNumber - 1])) { if (isOpen) Close(); else Open(np); }
        if (isMoving) { currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, openSpeed * Time.deltaTime); transform.rotation = closedRot * Quaternion.Euler(0, currentAngle, 0); if (Mathf.Abs(currentAngle - targetAngle) < 0.1f) { currentAngle = targetAngle; isMoving = false; } }
        if (isOpen && autoCloseDelay > 0) { closeTimer += Time.deltaTime; if (closeTimer >= autoCloseDelay) Close(); }
    }

    void Open(PlayerController p) { isOpen = true; isMoving = true; closeTimer = 0; float dot = Vector3.Dot(transform.forward, p.transform.position - transform.position); targetAngle = dot > 0 ? -openAngle : openAngle; }
    void Close() { isOpen = false; isMoving = true; targetAngle = 0; closeTimer = 0; }

    PlayerController FindNearest()
    {
        PlayerController n = null; float nd = interactionRadius;
        foreach (var p in FindObjectsOfType<PlayerController>()) { if (p.IsEliminated()) continue; float d = Vector3.Distance(transform.position, p.transform.position); if (d < nd) { nd = d; n = p; } }
        return n;
    }

    void CreateHint()
    {
        hintCanvas = new GameObject("DoorHint"); hintCanvas.transform.SetParent(transform, false); hintCanvas.transform.localPosition = Vector3.up * 2.5f;
        var c = hintCanvas.AddComponent<Canvas>(); c.renderMode = RenderMode.WorldSpace; c.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 50); c.transform.localScale = Vector3.one * 0.01f;
        hintCanvas.AddComponent<BillboardHint>();
        var to = new GameObject("T"); to.transform.SetParent(hintCanvas.transform, false);
        var tr = to.AddComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = tr.offsetMax = Vector2.zero;
        hintText = to.AddComponent<UnityEngine.UI.Text>(); hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); hintText.fontSize = 28; hintText.alignment = TextAnchor.MiddleCenter; hintText.color = Color.yellow; hintText.fontStyle = FontStyle.Bold; hintText.horizontalOverflow = HorizontalWrapMode.Overflow;
        to.AddComponent<UnityEngine.UI.Outline>().effectColor = Color.black;
        hintCanvas.SetActive(false);
    }
}

public class BillboardHint : MonoBehaviour
{
    void LateUpdate() { if (Camera.main != null) transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up); }
}