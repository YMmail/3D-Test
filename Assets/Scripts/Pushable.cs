using UnityEngine;

// ============================================================
// Pushable — ของที่มาริสาเดินชนแล้วดันได้ (ไม่ต้องกดปุ่ม)
// 🚨 ดันได้เฉพาะของที่ใส่สคริปต์นี้เท่านั้น (ตอนนี้มีแค่ลังในโซน 1)
// ใส่ที่ลัง (ต้องมี Collider) สคริปต์จะใส่ Rigidbody ให้เอง
// - ดันได้อย่างเดียว ดึงไม่ได้
// - ดันตรงๆ ตามแกน ซ้าย-ขวา หรือ เข้า-ออก (ไม่ไถลเฉียง) จะได้วางชิดขั้นง่าย
// - ไม่ได้ดัน = ลังหยุดนิ่งทันที / ยืนบนลังไม่ทำให้ลังขยับ / ชนผนังก็หยุด
// ============================================================
[RequireComponent(typeof(Rigidbody))]
public class Pushable : MonoBehaviour
{
    // --- 1. SETTINGS (ปรับได้) ---
    [Tooltip("💡 ตอนดัน มาริสาเดินช้าลงเหลือกี่เท่า (0.5 = ครึ่งนึง ลังหนัก)")] [Range(0.1f, 1f)] public float pushSpeedMultiplier = 0.5f;
    [Tooltip("ต้องเดินเข้าหาลังตรงแค่ไหนถึงจะนับว่าดัน (0.5 = เฉียงได้ไม่เกิน 60°)")] [Range(0f, 1f)] public float pushAngleThreshold = 0.5f;
    [Tooltip("น้ำหนักลัง (กก.)")] public float mass = 20f;

    // --- 2. สถานะ ---
    public bool IsBeingPushed { get; private set; }

    // --- 3. ตัวแปรภายใน ---
    Rigidbody body;
    Collider col;
    PlayerController25D pusher;   // คนที่กำลังดันอยู่ (เฟรมนี้)
    PlayerController25D lastPusher;
    Vector3 pushVelocity;
    float originalSpeedMul = 1f;
    Vector3 startPos;   // ตำแหน่งเริ่มต้น (ผู้เล่นตาย = กลับที่เดิม)
    static readonly System.Collections.Generic.List<Pushable> all = new System.Collections.Generic.List<Pushable>();
    void OnEnable() { all.Add(this); }
    // 💡 ผู้เล่นตายแล้วกล่องกลับที่เดิม กันดันไปตกผิดที่แล้วเล่นต่อไม่ได้ (PlayerRespawn เรียก)
    public static void ResetAll()
    {
        foreach (var p in all)
        {
            if (p == null || p.body == null) continue;
            p.body.linearVelocity = Vector3.zero;
            p.body.position = p.startPos; p.transform.position = p.startPos;
        }
    }


    // --- 4. เริ่มต้น ---
    void Awake()
    {
        startPos = transform.position;
    }

    void Start()
    {
        body = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        body.mass = mass;
        body.constraints = RigidbodyConstraints.FreezeRotation; // ไม่กลิ้ง ไม่ล้ม
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // ผิวลื่น เลื่อนบนพื้นได้ไม่สะดุด
        if (col.sharedMaterial == null)
        {
            var slippery = new PhysicsMaterial("Pushable_NoFriction");
            slippery.dynamicFriction = 0f; slippery.staticFriction = 0f;
            slippery.frictionCombine = PhysicsMaterialCombine.Minimum;
            col.sharedMaterial = slippery;
        }
    }

    // --- 5. ฟิสิกส์: ใส่ความเร็วตามที่ถูกดัน / ไม่ถูกดัน = หยุด ---
    void FixedUpdate()
    {
        Vector3 v = body.linearVelocity;
        if (pusher != null) { v.x = pushVelocity.x; v.z = pushVelocity.z; }
        else { v.x = 0f; v.z = 0f; }
        body.linearVelocity = v;

        // ทำให้คนดันเดินช้าลงตอนดัน แล้วคืนค่าตอนเลิกดัน
        if (pusher != lastPusher)
        {
            if (lastPusher != null) lastPusher.speedMultiplier = originalSpeedMul;
            if (pusher != null) { originalSpeedMul = pusher.speedMultiplier; pusher.speedMultiplier = originalSpeedMul * pushSpeedMultiplier; }
            lastPusher = pusher;
        }
        IsBeingPushed = pusher != null;

        pusher = null; // เคลียร์ทุกรอบ เดี๋ยว OnCollisionStay ใส่ให้ใหม่ถ้ายังดันอยู่
    }

    // --- 6. เช็คว่ามาริสากำลังเดินดันอยู่ไหม ---
    void OnCollisionStay(Collision c)
    {
        var player = c.collider.GetComponentInParent<PlayerController25D>();
        if (player == null) return;

        // ยืนอยู่บนลัง = ไม่ใช่การดัน
        if (player.transform.position.y > col.bounds.max.y - 0.2f) return;

        // ทิศดัน = ทิศที่มาริสาเดิน เลือกแกนที่ใกล้ที่สุด (X หรือ Z) จะได้ดันตรงๆ
        // 💡 ใช้ทิศเดิน (ไม่ใช่ตำแหน่งลัง) กันลังติดผนังหลังแล้วดันไม่ไป
        Vector3 move = player.MoveDirection; move.y = 0f;
        if (move.sqrMagnitude < 0.01f) return;
        Vector3 axis = Mathf.Abs(move.x) >= Mathf.Abs(move.z)
            ? new Vector3(Mathf.Sign(move.x), 0f, 0f)
            : new Vector3(0f, 0f, Mathf.Sign(move.z));
        if (Vector3.Dot(move.normalized, axis) < pushAngleThreshold) return;

        // ลังต้องอยู่ข้างหน้าตามทิศที่เดิน และตัวต้องชนหน้าลัง (ไม่ใช่แค่เฉียดข้าง)
        Vector3 toCrate = transform.position - player.transform.position; toCrate.y = 0f;
        float ahead = Vector3.Dot(toCrate, axis);
        float side = (toCrate - axis * ahead).magnitude;
        if (ahead < 0.2f || side > col.bounds.extents.x + 0.3f) return;

        pusher = player;
        pushVelocity = axis * Vector3.Dot(move, axis);
    }

    // --- 7. ถ้าลังถูกปิด/ลบระหว่างดัน คืนความเร็วให้คนดัน ---
    void OnDisable()
    {
        all.Remove(this);
        if (lastPusher != null) lastPusher.speedMultiplier = originalSpeedMul;
        lastPusher = null;
    }
}
