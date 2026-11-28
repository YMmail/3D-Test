using System.Collections;
using UnityEngine;

// ============================================================
// PlayerRespawn — มาริสาตาย → จอดำ → เกิดใหม่ที่ checkpoint ล่าสุด
// ใส่ที่ตัว Marisa (อยู่ใน prefab)
// ใครจะฆ่ามาริสา เรียก PlayerRespawn.Kill() ได้เลย (ปลา, ตกน้ำ, ตกหลุม)
// 💡 หน้าตายอยู่ใน DeathScreen / เปลี่ยนข้อความหรือใส่รูปได้ที่ช่องด้านล่าง
// ============================================================
public class PlayerRespawn : MonoBehaviour
{
    // --- 1. SETTINGS (ปรับได้) ---
    [Tooltip("จอค่อยๆ ดำกี่วินาที")] public float fadeOutTime = 0.5f;
    [Tooltip("ค้างจอดำกี่วินาที")] public float blackHoldTime = 0.6f;
    [Tooltip("จอค่อยๆ สว่างกลับกี่วินาที")] public float fadeInTime = 0.5f;
    [Tooltip("ข้อความหน้าตาย")] public string deathTitle = "คุณตายแล้ว";
    [Tooltip("💡 รูปพื้นหลังหน้าตาย 1920x1080 (เว้นว่าง = จอดำ)")] public Sprite deathBackground;

    // --- 2. สถานะ ---
    public static PlayerRespawn Instance { get; private set; }
    public bool IsDead { get; private set; }
    Vector3 checkpointPos;
    Quaternion checkpointFacing = Quaternion.identity;
    DeathScreen screen;
    public DeathScreen Screen => screen;

    PlayerController25D controller;
    Rigidbody body;

    // --- 3. เริ่มต้น: จุดเกิดแรก = ตำแหน่งตอนเริ่มเกม ---
    void Awake()
    {
        Instance = this;
        controller = GetComponent<PlayerController25D>();
        body = GetComponent<Rigidbody>();
        checkpointPos = transform.position;
        if (controller != null && controller.model != null) checkpointFacing = controller.model.rotation;
    }

    // --- 4. ตั้ง checkpoint (Checkpoint.cs เรียก) ---
    public void SetCheckpoint(Vector3 pos, Quaternion facing)
    {
        checkpointPos = pos;
        checkpointFacing = facing;
    }

    // --- 5. ฆ่ามาริสา (ขึ้นหน้าตาย รอกด "ลองใหม่") ---
    public static void Kill()
    {
        if (Instance != null && !Instance.IsDead) Instance.StartCoroutine(Instance.DieRoutine(true));
    }

    // กลับ checkpoint เฉยๆ ไม่ขึ้นหน้าตาย (เมนูหยุดเกมเรียก)
    public static void ReturnToCheckpoint()
    {
        if (Instance != null && !Instance.IsDead) Instance.StartCoroutine(Instance.DieRoutine(false));
    }

    // true = กลับ checkpoint เฉยๆ ไม่ได้ตายจริง (ระบบเสียงใช้ดู)
    public bool IsSilentReturn { get; private set; }

    IEnumerator DieRoutine(bool showScreen)
    {
        IsDead = true;
        IsSilentReturn = !showScreen;
        if (controller != null) controller.canMove = false;
        if (screen == null) screen = DeathScreen.Create(deathTitle, deathBackground);

        // 5.1 จอค่อยๆ ดำ
        for (float t = 0; t < fadeOutTime; t += Time.deltaTime) { screen.SetFade(Mathf.Max(0.01f, t / fadeOutTime)); yield return null; }
        screen.SetFade(1f);

        // 5.2 ย้ายกลับ checkpoint + รีเซ็ตของในฉาก
        if (body != null) { body.linearVelocity = Vector3.zero; body.position = checkpointPos; }
        transform.position = checkpointPos;
        if (controller != null && controller.model != null) controller.model.rotation = checkpointFacing;
        Physics.SyncTransforms();
        var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow25D>() : null;
        if (cam != null) cam.SnapToTarget();
        FishAI.ResetAll();
        Pushable.ResetAll();

        yield return new WaitForSeconds(blackHoldTime);

        // 5.3 หน้าตาย: รอกดลองใหม่
        if (showScreen)
        {
            screen.ShowMenu();
            while (!screen.Chosen) yield return null;
            screen.HideMenu();
            FishAI.ResetAll();                      // ปลาว่ายไปไหนระหว่างรอ ก็กลับที่เดิม
            yield return new WaitForSeconds(0.25f); // กันปุ่ม Space ที่กดเลือก กลายเป็นกระโดด
        }

        // 5.4 จอค่อยๆ สว่าง แล้วเล่นต่อได้
        if (controller != null) controller.canMove = true;
        IsDead = false;
        for (float t = 0; t < fadeInTime; t += Time.deltaTime) { if (IsDead) yield break; screen.SetFade(Mathf.Max(0.01f, 1f - t / fadeInTime)); yield return null; }
        if (!IsDead) screen.SetFade(0f);
    }
}
