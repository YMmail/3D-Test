using UnityEngine;

// ============================================================
// PlayerController25D — ควบคุมมาริสา (เดิน/วิ่ง/กระโดด)
// เดินได้ทุกทิศแบบ Little Nightmares: A/D = ซ้าย-ขวาของจอ, W/S = ลึกเข้าจอ/ออกมาหาจอ
// ใส่สคริปต์นี้ที่ตัว "Marisa" (ตัวแม่) ที่มี Rigidbody + Capsule Collider
// 🚨 ตัวแม่ต้องชื่อ "Marisa" เพราะสคริปต์ของอาจารย์ (HitAnd...) เช็คจากชื่อ
// 💡 โมเดลให้อยู่ในลูกชื่อ "Model" สคริปต์จะหมุนลูกนี้ให้หันไปทางที่เดิน
// ============================================================
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController25D : MonoBehaviour
{
    // --- 1. SETTINGS (ปรับได้) ---
    [Header("ความเร็ว")]
    [Tooltip("ความเร็วเดิน (เมตร/วินาที)")] public float walkSpeed = 2.5f;
    [Tooltip("ความเร็ววิ่งตอนกด Shift (เมตร/วินาที)")] public float runSpeed = 5f;
    [Tooltip("เร่ง/ชะลอเร็วแค่ไหน ยิ่งมากยิ่งหยุดทันที")] public float acceleration = 20f;

    [Header("กระโดด")]
    [Tooltip("💡 กระโดดสูงสุดกี่เมตร (1.2 = ขึ้นลัง 1 ม. ได้ แต่ขึ้นขั้น 2 ม. ตรงๆ ไม่ได้)")] public float jumpHeight = 1.2f;
    [Tooltip("ตกจากขอบแล้วยังกดกระโดดได้อีกกี่วินาที (coyote time)")] public float coyoteTime = 0.12f;
    [Tooltip("กด Space ก่อนถึงพื้นนิดนึง ก็ยังนับว่ากระโดด (วินาที)")] public float jumpBuffer = 0.12f;
    [Tooltip("แรงโน้มถ่วงเพิ่มตอนตก ให้ตกเร็วไม่ลอยๆ")] public float fallGravityMultiplier = 1.8f;

    [Header("หันตัว")]
    [Tooltip("ลูกที่เป็นโมเดล (ถ้าเว้นว่างจะหาลูกชื่อ Model เอง)")] public Transform model;
    [Tooltip("หันตัวเร็วแค่ไหน (องศา/วินาที)")] public float turnSpeed = 720f;

    [Header("เช็คพื้น")]
    [Tooltip("🚨 เลือก Layer ที่นับเป็นพื้น (ค่าเริ่มต้น = ทุกอย่าง)")] public LayerMask groundMask = ~0;
    [Tooltip("ระยะยิงเช็คพื้นใต้เท้า")] public float groundCheckDistance = 0.15f;

    [Header("ให้สคริปต์อื่นสั่ง")]
    [Tooltip("false = ห้ามขยับ (เช่น ตอนฉากเปิดเกม / ตาย)")] public bool canMove = true;
    [Tooltip("คูณความเร็ว (เช่น ลากลังให้ช้าลง = 0.5)")] public float speedMultiplier = 1f;
    [Tooltip("true = ไม่หันตัว (เช่น ตอนลากลังต้องหันหน้าเข้าลังตลอด)")] public bool lockFacing = false;
    [Tooltip("false = กระโดดไม่ได้ (เช่น ตอนกำลังลากลัง)")] public bool canJump = true;

    // --- 2. สถานะ (อ่านได้จากสคริปต์อื่น) ---
    public bool IsGrounded { get; private set; }
    public Vector3 MoveDirection { get; private set; }
    public Vector3 FacingDirection => model != null ? model.forward : transform.forward;

    // --- 3. ตัวแปรภายใน ---
    Rigidbody body;
    CapsuleCollider capsule;
    Animator animator;
    Transform cam;
    Vector3 inputDir;
    bool running;
    float lastGroundedTime = -10f;
    float lastJumpPressedTime = -10f;
    bool hasSpeed, hasGrounded, hasJump;

    // --- 4. เริ่มต้น ---
    void Awake()
    {
        body = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        body.freezeRotation = true;                                   // ไม่ให้ล้มกลิ้ง
        body.interpolation = RigidbodyInterpolation.Interpolate;      // ภาพลื่น ไม่กระตุก
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // 💡 ใส่ผิวลื่นให้ตัวละคร จะได้ไม่เกาะติดผนังตอนกระโดดชน
        if (capsule.sharedMaterial == null)
        {
            var slippery = new PhysicsMaterial("Player_NoFriction");
            slippery.dynamicFriction = 0f; slippery.staticFriction = 0f;
            slippery.frictionCombine = PhysicsMaterialCombine.Minimum;
            capsule.sharedMaterial = slippery;
        }

        if (model == null) model = transform.Find("Model");
        animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            foreach (var p in animator.parameters)
            {
                if (p.name == "Speed") hasSpeed = true;
                if (p.name == "IsGrounded") hasGrounded = true;
                if (p.name == "Jump") hasJump = true;
            }
        }
        if (Camera.main != null) cam = Camera.main.transform;
    }

    // --- 5. รับปุ่ม (ทุกเฟรม) ---
    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal"); // A/D
        float v = Input.GetAxisRaw("Vertical");   // W/S
        running = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // ทิศเดินอิงจากกล้อง (ตัดแกนขึ้น-ลงทิ้ง)
        Vector3 right = Vector3.right, forward = Vector3.forward;
        if (cam != null)
        {
            right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        }
        inputDir = Vector3.ClampMagnitude(right * h + forward * v, 1f);
        if (!canMove) inputDir = Vector3.zero;

        if (Input.GetButtonDown("Jump")) lastJumpPressedTime = Time.time;

        // หันโมเดลไปทางที่เดิน
        if (model != null && !lockFacing && inputDir.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(inputDir, Vector3.up);
            model.rotation = Quaternion.RotateTowards(model.rotation, target, turnSpeed * Time.deltaTime);
        }

        UpdateAnimator();
    }

    // --- 6. ฟิสิกส์ (เดิน/กระโดด) ---
    void FixedUpdate()
    {
        CheckGround();

        // 6.1 เดิน/วิ่ง: ค่อยๆ ปรับความเร็วแนวนอนเข้าหาเป้า
        float speed = (running ? runSpeed : walkSpeed) * speedMultiplier;
        Vector3 targetVel = inputDir * speed;
        Vector3 vel = body.linearVelocity;
        Vector3 flat = new Vector3(vel.x, 0f, vel.z);
        flat = Vector3.MoveTowards(flat, targetVel, acceleration * Time.fixedDeltaTime);
        vel.x = flat.x; vel.z = flat.z;
        MoveDirection = flat;

        // 6.2 กระโดด (มี coyote time + jump buffer)
        bool canJumpNow = canMove && canJump && Time.time - lastGroundedTime <= coyoteTime;
        bool wantsJump = Time.time - lastJumpPressedTime <= jumpBuffer;
        if (canJumpNow && wantsJump)
        {
            vel.y = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * jumpHeight);
            lastJumpPressedTime = -10f;
            lastGroundedTime = -10f;
            if (animator != null && hasJump) animator.SetTrigger("Jump");
        }

        // 6.3 ตกให้เร็วขึ้น
        if (vel.y < 0f)
            vel += Physics.gravity * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime;

        body.linearVelocity = vel;
    }

    // --- 7. เช็คว่ายืนบนพื้นไหม ---
    void CheckGround()
    {
        float radius = capsule.radius * 0.9f;
        Vector3 origin = transform.position + Vector3.up * (radius + 0.05f);
        IsGrounded = Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit,
                                        0.05f + groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore)
                     && body.linearVelocity.y <= 0.1f;
        if (IsGrounded) lastGroundedTime = Time.time;
    }

    // --- 8. ส่งค่าไป Animator (ถ้ามี) ---
    void UpdateAnimator()
    {
        if (animator == null) return;
        if (hasSpeed) animator.SetFloat("Speed", MoveDirection.magnitude);
        if (hasGrounded) animator.SetBool("IsGrounded", IsGrounded);
    }
}
