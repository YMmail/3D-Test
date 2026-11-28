using UnityEngine;

// ============================================================
// CameraFollow25D — กล้องตามมาริสาแบบ Little Nightmares
// มุมเดียวทั้งเกม: มองจากด้านข้าง เฉียงลง 15° ผู้เล่นหมุนกล้องเองไม่ได้
// กล้องเลื่อนตามนุ่มๆ และไม่หมุนตามทิศที่ตัวละครหัน
// ใส่สคริปต์นี้ที่ Main Camera แล้วลากตัว Marisa ใส่ช่อง Target
// ============================================================
public class CameraFollow25D : MonoBehaviour
{
    // --- 1. SETTINGS (ปรับได้) ---
    [Header("เป้าหมาย")]
    [Tooltip("🚨 ลากตัว Marisa มาใส่")] public Transform target;
    [Tooltip("มองที่ความสูงเท่าไหร่จากเท้า (เมตร) ประมาณอกตัวละคร")] public float lookHeight = 1.1f;

    [Header("มุมกล้อง")]
    [Tooltip("มุมก้มลง (องศา) ตามที่ตกลงไว้ = 15")] public float pitch = 15f;
    [Tooltip("หมุนซ้าย-ขวา (องศา) 0 = มองตรงเข้าท่อ")] public float yaw = 0f;
    [Tooltip("ระยะห่างจากตัวละคร (เมตร)")] public float distance = 9f;

    [Header("ความนุ่ม")]
    [Tooltip("เวลาไล่ตาม (วินาที) ยิ่งมากยิ่งหน่วง")] public float smoothTime = 0.25f;
    [Tooltip("💡 มองล่วงหน้าไปทางที่ตัวละครเดิน (เมตร)")] public float lookAhead = 1.2f;
    [Tooltip("💡 กล้องตามความลึกตอนกด W/S แค่ไหน (0 = ไม่ตามเลย, 1 = ตามเต็มที่) เดินลึกเข้าไปกล้องจะซูมตามนิดหน่อย")] [Range(0f, 1f)] public float depthFollow = 0.4f;
    [Tooltip("ความลึกกลางท่อ (Z) ที่กล้องยึดเป็นหลัก")] public float fixedDepthZ = 2f;
    [Tooltip("💡 false = กล้องไม่เด้งขึ้น-ลงตอนกระโดด (ขยับตามความสูงตอนยืนบนพื้น/ลังเท่านั้น)")] public bool followJumps = false;

    [Header("ขอบเขตกล้อง (ไม่ให้เห็นนอกฉาก)")]
    public bool useBounds = false;
    public float minX = -100f, maxX = 100f;
    public float minY = -100f, maxY = 100f;

    // --- 2. ตัวแปรภายใน ---
    Vector3 velocity;
    Vector3 aheadOffset;
    Vector3 aheadVelocity;
    Rigidbody targetBody;
    PlayerController25D targetPlayer;
    float trackedY; // ความสูงที่กล้องใช้ (พื้นที่ยืนล่าสุด)

    // จำตำแหน่งกล้องข้ามฉาก (static = ไม่หายตอนเปลี่ยนฉาก) 💡 เปลี่ยนฉากแล้วกล้องเริ่มที่เดิม แล้วค่อยๆ เลื่อนไปตำแหน่งใหม่ ไม่กระตุก
    static bool hasCarried;
    static string carriedScene;
    static Vector3 carriedPosition, carriedAhead;

    // กด Play ใหม่ = ล้างของรอบก่อน (โปรเจกต์ปิด Domain Reload ค่า static เลยไม่หายเอง)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay() { hasCarried = false; carriedScene = null; }

    // --- 3. เริ่มต้น: วางกล้องที่ตำแหน่งทันที ไม่ให้เลื่อนมาจากที่ไกล ---
    void Start()
    {
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        if (target == null) return;
        targetBody = target.GetComponent<Rigidbody>();
        targetPlayer = target.GetComponent<PlayerController25D>();
        trackedY = target.position.y;
        string here = gameObject.scene.name;
        if (hasCarried && carriedScene != here) { transform.position = carriedPosition; aheadOffset = carriedAhead; }
        else transform.position = DesiredPosition();
        hasCarried = false;
    }

    // --- 4. ตามทุกเฟรม (หลังตัวละครขยับเสร็จ) ---
    void LateUpdate()
    {
        if (target == null) return;

        // 4.1 มองล่วงหน้าตามทิศที่เดิน (เฉพาะซ้าย-ขวา)
        float vx = targetBody != null ? targetBody.linearVelocity.x : 0f;
        Vector3 wantAhead = new Vector3(Mathf.Clamp(vx, -1f, 1f) * lookAhead, 0f, 0f);
        aheadOffset = Vector3.SmoothDamp(aheadOffset, wantAhead, ref aheadVelocity, 0.6f);

        // 4.2 ความสูง: ตามเฉพาะตอนยืนบนพื้น หรือตอนตกต่ำกว่าเดิม (กระโดดแล้วกล้องไม่เด้งตาม)
        bool grounded = targetPlayer == null || targetPlayer.IsGrounded;
        if (followJumps || grounded || target.position.y < trackedY) trackedY = target.position.y;

        // 4.3 เลื่อนกล้องนุ่มๆ (มุมไม่เปลี่ยน)
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = Vector3.SmoothDamp(transform.position, DesiredPosition(), ref velocity, smoothTime);

        // 4.4 จำไว้เผื่อเปลี่ยนฉาก
        hasCarried = true; carriedScene = gameObject.scene.name; carriedPosition = transform.position; carriedAhead = aheadOffset;
    }

    // --- 5. คำนวณตำแหน่งที่กล้องควรอยู่ ---
    Vector3 DesiredPosition()
    {
        Vector3 focus = new Vector3(target.position.x, trackedY, target.position.z) + Vector3.up * lookHeight + aheadOffset;
        focus.z = Mathf.Lerp(fixedDepthZ, focus.z, depthFollow); // ตามความลึกแค่บางส่วน
        if (useBounds)
        {
            focus.x = Mathf.Clamp(focus.x, minX, maxX);
            focus.y = Mathf.Clamp(focus.y, minY, maxY);
        }
        return focus - Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward * distance;
    }

    // --- 6. ย้ายกล้องทันที (ทางเดินวนเรียกใช้ ย้ายพร้อมตัวละครระยะเท่ากัน ภาพจะไม่กระตุก) ---
    // --- 7. วางกล้องที่ตัวละครทันที (ตอนเกิดใหม่หลังตาย) ---
    public void SnapToTarget()
    {
        if (target == null) return;
        trackedY = target.position.y;
        aheadOffset = Vector3.zero; aheadVelocity = Vector3.zero; velocity = Vector3.zero;
        transform.position = DesiredPosition();
    }

    public void ShiftBy(Vector3 offset)
    {
        transform.position += offset;
    }
}
