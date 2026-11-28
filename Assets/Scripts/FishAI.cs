using System.Collections.Generic;
using UnityEngine;

// ============================================================
// FishAI — ปลาหมอคางดำกลายพันธุ์
// ว่ายวน → เห็นมาริสาแล้วไล่ → หยุดใต้น้ำแป๊บนึง (เตือน) → พุ่งขึ้นมางับ (โดน = ตาย)
// 🔦 โดนไฟฉายส่อง = ตกใจ ว่ายหนี งับไม่ได้ช่วงนึง
// ปลาอยู่ในน้ำเท่านั้น (ขอบเขต X/Z ตั้งได้) ไม่ต้องมี Rigidbody
// 💡 ตัวโมเดลให้หันหัวไปทาง +Z ของตัวปลา
// ============================================================
public class FishAI : MonoBehaviour
{
    // --- 1. SETTINGS (ปรับได้) ---
    [Header("ขอบเขตน้ำ (ปลาออกนอกนี้ไม่ได้)")]
    public float minX = 27.3f, maxX = 58.7f;
    public float minZ = 0.4f, maxZ = 2.2f;
    [Tooltip("ระดับความลึกตอนว่าย (Y)")] public float swimY = -0.6f;

    [Header("ว่ายวน")]
    [Tooltip("ว่ายวนห่างจากบ้านได้ข้างละกี่เมตร")] public float patrolRange = 5f;
    public float patrolSpeed = 1.5f;

    [Header("ไล่")]
    [Tooltip("มาริสาเข้าใกล้กว่านี้ (เมตร) ปลาจะเริ่มไล่")] public float detectRange = 7f;
    [Tooltip("💡 ความเร็วไล่ (มาริสาเดิน 2.5 / วิ่ง 5 → วิ่งหนีทัน)")] public float chaseSpeed = 3.5f;
    [Tooltip("มาริสาอยู่สูงกว่าน้ำเกินนี้ ปลาไม่สนใจ (เช่น ยืนบนขั้นสูง)")] public float ignoreAboveY = 1.2f;

    [Header("งับ")]
    [Tooltip("ห่างแค่นี้ (แนวซ้าย-ขวา) จะเตรียมงับ")] public float attackRange = 1.2f;
    [Tooltip("🚨 หยุดเตือนก่อนงับกี่วินาที (ผู้เล่นมีเวลาหนี/ส่องไฟ)")] public float warnTime = 0.6f;
    [Tooltip("พุ่งขึ้นมางับใช้เวลากี่วินาที")] public float lungeTime = 0.35f;
    [Tooltip("ปากปลาใกล้ตัวมาริสาแค่นี้ = โดนงับ")] public float biteRadius = 0.7f;
    [Tooltip("งับพลาดแล้วพักกี่วินาที")] public float attackCooldown = 1.5f;

    [Header("กลัวไฟฉาย")]
    [Tooltip("โดนไฟส่องแล้วหนีนานกี่วินาที")] public float fleeTime = 2f;
    public float fleeSpeed = 4f;

    // --- 2. สถานะ ---
    public enum State { Patrol, Chase, Warn, Lunge, Cooldown, Flee }
    public State CurrentState { get; private set; }
    static readonly List<FishAI> all = new List<FishAI>();

    Vector3 home;          // จุดบ้าน (ตอนเริ่มเกม)
    Vector3 startPos;      // ไว้รีเซ็ตตอนผู้เล่นตาย
    float patrolDir = 1f;
    float timer;
    Vector3 lungeFrom, lungeTo;
    Transform player;
    Rigidbody playerBody;
    [Tooltip("💡 เล็งล่วงหน้าตามที่มาริสาเดิน (0 = งับตำแหน่งเดิม หลบง่าย / 1 = เดาทางแม่น)")] [Range(0f, 1f)] public float leadAim = 0.6f;

    // --- 3. เริ่มต้น ---
    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); }

    void Start()
    {
        startPos = transform.position;
        home = startPos;
        var pc = FindFirstObjectByType<PlayerController25D>();
        if (pc != null) { player = pc.transform; playerBody = pc.GetComponent<Rigidbody>(); }
        CurrentState = State.Patrol;
    }

    // --- 4. ทุกเฟรม ---
    void Update()
    {
        if (player == null) return;
        bool playerDead = PlayerRespawn.Instance != null && PlayerRespawn.Instance.IsDead;

        // 4.1 โดนไฟฉาย = หนี (ยกเว้นตอนกำลังพุ่งงับอยู่กลางอากาศ)
        if (CurrentState != State.Lunge && IsLit()) { CurrentState = State.Flee; timer = fleeTime; }

        Vector3 p = player.position;
        float dx = p.x - transform.position.x;
        bool playerReachable = !playerDead && p.y < ignoreAboveY && p.x > minX - 1f && p.x < maxX + 1f;

        switch (CurrentState)
        {
            case State.Patrol:
                // ว่ายไป-กลับรอบบ้าน
                float targetX = home.x + patrolDir * patrolRange;
                SwimTowards(new Vector3(targetX, swimY, Mathf.Lerp(minZ, maxZ, 0.5f)), patrolSpeed);
                if (Mathf.Abs(transform.position.x - targetX) < 0.3f) patrolDir *= -1f;
                if (playerReachable && Mathf.Abs(dx) < detectRange) CurrentState = State.Chase;
                break;

            case State.Chase:
                // ว่ายไปใต้มาริสา (ชิดขอบน้ำฝั่งทางเดิน)
                SwimTowards(new Vector3(p.x, swimY, maxZ), chaseSpeed);
                if (!playerReachable || Mathf.Abs(dx) > detectRange * 1.5f) { home = transform.position; CurrentState = State.Patrol; }
                else if (Mathf.Abs(dx) < attackRange) { CurrentState = State.Warn; timer = warnTime; }
                break;

            case State.Warn:
                // หยุดนิ่งใต้น้ำ สั่นนิดๆ เป็นสัญญาณเตือน
                transform.position += new Vector3(Mathf.Sin(Time.time * 60f) * 0.01f, 0f, 0f);
                timer -= Time.deltaTime;
                if (!playerReachable) { CurrentState = State.Patrol; break; }
                // 💡 ตอนหมดเวลาเตือน ถ้ามาริสาถอยหนีไปไกลแล้ว = ยกเลิกงับ ว่ายหนีไปพัก (หลบทัน = รอด)
                if (timer <= 0f && Mathf.Abs(dx) > attackRange * 1.8f) { CurrentState = State.Cooldown; timer = attackCooldown * 0.5f; break; }
                if (timer <= 0f)
                {
                    CurrentState = State.Lunge; timer = 0f;
                    lungeFrom = transform.position;
                    Vector3 lead = playerBody != null ? new Vector3(playerBody.linearVelocity.x, 0f, playerBody.linearVelocity.z) * lungeTime * leadAim : Vector3.zero;
                    lungeTo = p + lead + Vector3.up * 0.7f; // พุ่งไปที่อกมาริสา (เดาทางล่วงหน้านิดนึง)
                }
                break;

            case State.Lunge:
                // พุ่งโค้งขึ้นจากน้ำ
                timer += Time.deltaTime / lungeTime;
                float t = Mathf.Clamp01(timer);
                Vector3 pos = Vector3.Lerp(lungeFrom, lungeTo, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.6f;
                FaceTowards(pos - transform.position);
                transform.position = pos;
                if (!playerDead && Vector3.Distance(transform.position, player.position + Vector3.up * 0.7f) < biteRadius)
                    PlayerRespawn.Kill();
                if (t >= 1f) { CurrentState = State.Cooldown; timer = attackCooldown; }
                break;

            case State.Cooldown:
                // ตกกลับลงน้ำ แล้วพักแป๊บนึง
                SwimTowards(new Vector3(transform.position.x, swimY, Mathf.Lerp(minZ, maxZ, 0.5f)), patrolSpeed * 2f);
                timer -= Time.deltaTime;
                if (timer <= 0f) CurrentState = playerReachable ? State.Chase : State.Patrol;
                break;

            case State.Flee:
                // ว่ายหนีไปฝั่งตรงข้ามกับมาริสา
                float away = transform.position.x - p.x >= 0f ? 1f : -1f;
                SwimTowards(new Vector3(transform.position.x + away * 3f, swimY, minZ), fleeSpeed);
                timer -= Time.deltaTime;
                if (timer <= 0f) { home = transform.position; CurrentState = State.Patrol; }
                break;
        }
    }

    // --- 5. ว่ายไปหาจุด (อยู่ในน้ำเสมอ) ---
    void SwimTowards(Vector3 target, float speed)
    {
        target.x = Mathf.Clamp(target.x, minX, maxX);
        target.z = Mathf.Clamp(target.z, minZ, maxZ);
        Vector3 next = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        next.y = Mathf.MoveTowards(transform.position.y, swimY, 3f * Time.deltaTime); // กลับลงไปที่ระดับว่าย
        FaceTowards(next - transform.position);
        transform.position = next;
    }

    void FaceTowards(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.00001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up), 10f * Time.deltaTime);
    }

    // --- 6. โดนไฟฉายไหม (เช็คที่หัวปลา ยกขึ้นมาที่ผิวน้ำนิดนึง) ---
    bool IsLit()
    {
        if (Flashlight.Instance == null) return false;
        Vector3 head = transform.position + transform.forward * 0.6f;
        head.y = Mathf.Max(head.y, swimY + 0.3f);
        return Flashlight.Instance.IsLighting(head);
    }

    // --- 7. ทางเดินวนย้ายปลาไปด้วย (LoopCorridor เรียก) ---
    public void ShiftBy(Vector3 offset)
    {
        transform.position += offset;
        home += offset;
        lungeFrom += offset; lungeTo += offset;
    }

    public static IEnumerable<FishAI> All => all;

    // --- 8. ผู้เล่นตาย → ปลากลับที่เดิม ---
    public static void ResetAll()
    {
        foreach (var f in all)
        {
            f.transform.position = f.startPos;
            f.home = f.startPos;
            f.CurrentState = State.Patrol;
        }
    }
}
