using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Настройки игрока")]
    public int playerNumber = 1;

    [Header("Движение")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;

    [Header("Прыжок")]
    public float jumpForce = 7f;
    public float doubleJumpForce = 6f;
    public int maxJumps = 2;

    [Header("Толчок")]
    public float pushForce = 15f;
    public float pushCooldown = 1f;
    public float pushRadius = 2f;

    [Header("Комбо-рывок")]
    public float airDashForce = 12f;
    public float superDashForce = 18f;
    public float airDashLift = 3f;

    [Header("Вихрь")]
    public float spinDuration = 1.5f;
    public float spinRotationSpeed = 720f;
    public float spinPushForce = 20f;
    public float spinRadius = 3f;
    public float spinCooldown = 5f;

    [Header("Камера")]
    public float cameraRotateSpeed = 120f;

    [HideInInspector] public float cameraYawOffset = 0f;
    [HideInInspector] public bool isFlying = false;

    private Rigidbody rb;
    private Animator animator;
    private float lastPushTime = -10f;
    private float lastSpinTime = -10f;
    private bool isEliminated = false;
    private Vector3 moveDirection;
    private bool isRunning = false;
    private bool isGrounded = true;
    private bool isSpinning = false;
    private float spinEndTime;
    private int jumpsRemaining;
    private int jumpsUsedThisAir = 0;
    private float targetYRotation = 0f;
    private bool hasMovedEver = false;
    private GameManager gameManager;

    // ФИX: сохраняем оригинальный масштаб при старте.
    // PowerUp.Giant меняет localScale, и без этого после респавна
    // игрок остаётся гигантским навсегда.
    private Vector3 originalScale;

    private KeyCode keyUp, keyDown, keyLeft, keyRight;
    private KeyCode keyPush, keyJump, keyRun, keySpin;
    private KeyCode keyCamLeft, keyCamRight;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        originalScale = transform.localScale;

        if (rb != null)
        {
            // ФИX: constraints устанавливаются один раз в Start, а не каждый кадр.
            // Переписывание физических свойств каждый Update мешает физическому движку
            // и создаёт лишнюю нагрузку.
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.angularDamping = 10f;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        targetYRotation = transform.eulerAngles.y;
        jumpsRemaining = maxJumps;
        gameManager = FindObjectOfType<GameManager>();
        SetupKeys();
    }

    void SetupKeys()
    {
        switch (playerNumber)
        {
            case 1: // Тигр
                keyUp = KeyCode.W; keyDown = KeyCode.S; keyLeft = KeyCode.A; keyRight = KeyCode.D;
                keyPush = KeyCode.F; keyJump = KeyCode.R; keyRun = KeyCode.T; keySpin = KeyCode.V;
                keyCamLeft = KeyCode.Z; keyCamRight = KeyCode.X;
                break;
            case 2: // Собака
                keyUp = KeyCode.UpArrow; keyDown = KeyCode.DownArrow; keyLeft = KeyCode.LeftArrow; keyRight = KeyCode.RightArrow;
                keyPush = KeyCode.RightShift; keyJump = KeyCode.RightControl; keyRun = KeyCode.RightAlt; keySpin = KeyCode.Return;
                keyCamLeft = KeyCode.Comma; keyCamRight = KeyCode.Period;
                break;
            case 3: // Кот
                keyUp = KeyCode.I; keyDown = KeyCode.K; keyLeft = KeyCode.J; keyRight = KeyCode.L;
                keyPush = KeyCode.H; keyJump = KeyCode.Y; keyRun = KeyCode.G; keySpin = KeyCode.B;
                keyCamLeft = KeyCode.N; keyCamRight = KeyCode.M;
                break;
            case 4: // Пингвин
                keyUp = KeyCode.Keypad8; keyDown = KeyCode.Keypad5; keyLeft = KeyCode.Keypad4; keyRight = KeyCode.Keypad6;
                keyPush = KeyCode.Keypad1; keyJump = KeyCode.Keypad3; keyRun = KeyCode.Keypad7; keySpin = KeyCode.Keypad9;
                keyCamLeft = KeyCode.KeypadDivide; keyCamRight = KeyCode.KeypadMultiply;
                break;
        }
    }

    void Update()
    {
        if (isEliminated) return;
        if (gameManager != null && !gameManager.IsRoundActive()) return;

        if (Input.GetKey(keyCamLeft)) cameraYawOffset -= cameraRotateSpeed * Time.deltaTime;
        if (Input.GetKey(keyCamRight)) cameraYawOffset += cameraRotateSpeed * Time.deltaTime;

        // Вращение при вихре — только логика поворота и таймер, без физики
        if (isSpinning)
        {
            targetYRotation += spinRotationSpeed * Time.deltaTime;
            if (Time.time >= spinEndTime) isSpinning = false;
            UpdateAnimations();
            return;
        }

        float h = Input.GetKey(keyRight) ? 1f : Input.GetKey(keyLeft) ? -1f : 0f;
        float v = Input.GetKey(keyUp) ? 1f : Input.GetKey(keyDown) ? -1f : 0f;
        bool pushP = Input.GetKeyDown(keyPush);
        bool jumpP = Input.GetKeyDown(keyJump);
        bool runH = Input.GetKey(keyRun);
        bool spinP = Input.GetKeyDown(keySpin);

        moveDirection = new Vector3(h, 0, v).normalized;
        isRunning = runH && moveDirection.magnitude > 0.1f;

        if (moveDirection.magnitude > 0.1f)
        {
            targetYRotation = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            hasMovedEver = true;
        }

        CheckGrounded();

        if (isFlying)
        {
            if (jumpP && rb != null) { rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z); rb.AddForce(Vector3.up * 3f, ForceMode.Impulse); }
            if (runH && rb != null) rb.AddForce(Vector3.down * 8f * Time.deltaTime, ForceMode.VelocityChange);
            if (pushP) { isFlying = false; if (rb != null) rb.useGravity = true; }
            UpdateAnimations();
            return;
        }

        if (jumpP && jumpsRemaining > 0 && rb != null)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            float f = (jumpsRemaining == maxJumps) ? jumpForce : doubleJumpForce;
            rb.AddForce(Vector3.up * f, ForceMode.Impulse);
            if (moveDirection.magnitude > 0.1f)
                rb.AddForce(moveDirection * ((jumpsRemaining == maxJumps) ? 2f : 3f), ForceMode.Impulse);
            jumpsRemaining--;
            jumpsUsedThisAir++;
        }

        if (pushP)
        {
            if (!isGrounded && jumpsUsedThisAir > 0) AirDash();
            else if (Time.time - lastPushTime >= pushCooldown) { Push(); lastPushTime = Time.time; }
        }

        if (spinP && Time.time - lastSpinTime >= spinCooldown)
        {
            isSpinning = true;
            spinEndTime = Time.time + spinDuration;
            lastSpinTime = Time.time;
        }

        UpdateAnimations();
    }

    void LateUpdate()
    {
        if (isEliminated) return;
        if (hasMovedEver || isSpinning)
        {
            float cur = transform.eulerAngles.y;
            float smooth = Mathf.LerpAngle(cur, targetYRotation, 15f * Time.deltaTime);
            transform.eulerAngles = new Vector3(0, smooth, 0);
        }
        else
        {
            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
        }
    }

    void FixedUpdate()
    {
        if (isEliminated || rb == null) return;
        if (gameManager != null && !gameManager.IsRoundActive()) return;

        // ФИX: rb.angularVelocity сбрасывается только в FixedUpdate.
        // Физические свойства Rigidbody нельзя менять в Update —
        // это нарушает детерминизм физического движка.
        rb.angularVelocity = Vector3.zero;

        if (isSpinning)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            // ФИX: SpinPushNearby перенесён в FixedUpdate.
            // ForceMode.Force применяет силу за физический шаг (fixedDeltaTime),
            // вызов из Update давал зависящий от фреймрейта результат.
            SpinPushNearby();
            return;
        }

        float spd = isRunning ? runSpeed : walkSpeed;
        rb.linearVelocity = new Vector3(moveDirection.x * spd, rb.linearVelocity.y, moveDirection.z * spd);
    }

    void UpdateAnimations()
    {
        if (animator == null) return;
        float speed = moveDirection.magnitude;
        float animSpeed = speed > 0.1f ? (isRunning ? 2f : 1f) : 0f;
        TrySetFloat("Vert", animSpeed);
        TrySetFloat("State", animSpeed);
        TrySetFloat("Speed", animSpeed);
        TrySetBool("isWalking", speed > 0.1f && !isRunning);
        TrySetBool("isRunning", isRunning);
    }

    void AirDash()
    {
        Vector3 dir = moveDirection.magnitude > 0.1f ? moveDirection : transform.forward;
        float pwr = jumpsUsedThisAir >= 2 ? superDashForce : airDashForce;
        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y * 0.3f, 0);
        rb.AddForce(dir * pwr + Vector3.up * airDashLift, ForceMode.Impulse);
        foreach (Collider c in Physics.OverlapSphere(transform.position + dir * 2f, pushRadius))
        {
            if (c.gameObject == gameObject) continue;
            var op = c.GetComponent<PlayerController>();
            if (op != null && !op.isEliminated)
            {
                var orb = c.GetComponent<Rigidbody>();
                if (orb != null)
                    orb.AddForce(((c.transform.position - transform.position).normalized + Vector3.up * 0.3f) * pwr * 0.8f, ForceMode.Impulse);
            }
        }
        lastPushTime = Time.time;
        jumpsRemaining = 0;
    }

    void CheckGrounded()
    {
        bool was = isGrounded;
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 0.3f))
        {
            if (hit.collider.gameObject != gameObject)
            {
                isGrounded = true;
                if (!was) { jumpsRemaining = maxJumps; jumpsUsedThisAir = 0; }
            }
            else isGrounded = false;
        }
        else isGrounded = false;
    }

    void Push()
    {
        Vector3 dir = moveDirection.magnitude > 0.1f ? moveDirection : transform.forward;
        foreach (Collider c in Physics.OverlapSphere(transform.position, pushRadius))
        {
            if (c.gameObject == gameObject) continue;
            var op = c.GetComponent<PlayerController>();
            if (op != null && !op.isEliminated)
            {
                var orb = c.GetComponent<Rigidbody>();
                if (orb != null)
                    orb.AddForce(((c.transform.position - transform.position).normalized + Vector3.up * 0.3f) * pushForce, ForceMode.Impulse);
            }
        }
        if (rb != null)
            rb.AddForce(dir * pushForce * 0.5f, ForceMode.Impulse);
    }

    void SpinPushNearby()
    {
        foreach (Collider c in Physics.OverlapSphere(transform.position, spinRadius))
        {
            if (c.gameObject == gameObject) continue;
            var op = c.GetComponent<PlayerController>();
            if (op != null && !op.isEliminated)
            {
                var d = (c.transform.position - transform.position).normalized;
                d.y = 0;
                var orb = c.GetComponent<Rigidbody>();
                if (orb != null)
                    orb.AddForce((d + Vector3.up * 0.2f) * spinPushForce, ForceMode.Force);
            }
        }
    }

    public void Eliminate()
    {
        isEliminated = true;
        isFlying = false;
        if (rb != null) { rb.linearVelocity = Vector3.zero; rb.useGravity = true; }
        if (gameManager != null) gameManager.PlayerEliminated(playerNumber);
        gameObject.SetActive(false);
    }

    public bool IsEliminated() => isEliminated;
    public float GetSpinCooldownRemaining() => Mathf.Max(0, spinCooldown - (Time.time - lastSpinTime));
    public int GetJumpsRemaining() => jumpsRemaining;
    public bool IsInAir() => !isGrounded;

    public void ResetPlayer(Vector3 pos)
    {
        isEliminated = false;
        isSpinning = false;
        isFlying = false;
        hasMovedEver = false;
        cameraYawOffset = 0f;
        jumpsRemaining = maxJumps;
        jumpsUsedThisAir = 0;

        // ФИX: сбрасываем масштаб на оригинальный — Giant мог оставить игрока увеличенным
        transform.localScale = originalScale;

        // ФИX: позиция ставится ДО SetActive, чтобы физдвижок не "просыпался"
        // на старой позиции (на полу) и не генерировал ложный OnCollisionEnter
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        transform.position = pos;
        gameObject.SetActive(true);
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    void TrySetFloat(string p, float v)
    {
        try { foreach (var x in animator.parameters) if (x.name == p && x.type == AnimatorControllerParameterType.Float) { animator.SetFloat(p, v); return; } } catch { }
    }
    void TrySetBool(string p, bool v)
    {
        try { foreach (var x in animator.parameters) if (x.name == p && x.type == AnimatorControllerParameterType.Bool) { animator.SetBool(p, v); return; } } catch { }
    }
    void TrySetTrigger(string p)
    {
        try { foreach (var x in animator.parameters) if (x.name == p && x.type == AnimatorControllerParameterType.Trigger) { animator.SetTrigger(p); return; } } catch { }
    }
}