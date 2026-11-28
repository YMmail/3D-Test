using UnityEngine;

// ============================================================
// LoopCorridor — ทางเดินวนไม่รู้จบแบบเนียนๆ
// หลักการ: สร้างท่อช่วงเดียวกันไว้ 2 ชุดต่อกัน (ชุด A กับ ชุด B หน้าตาเหมือนกันเป๊ะ)
// พอเดินมาถึงกล่องนี้ (กลางชุด B) → ย้ายมาริสา + กล้อง ถอยกลับไปตำแหน่งเดียวกันในชุด A
// ภาพบนจอเหมือนเดิมทุกอย่าง ผู้เล่นเลยไม่รู้ตัวว่าวนอยู่
// ใส่ที่ GameObject ที่มี Box Collider ติ๊ก Is Trigger
// 🚨 ระยะ loopLength ต้องเท่ากับระยะห่างระหว่างชุด A กับชุด B พอดี
// ============================================================
[RequireComponent(typeof(Collider))]
public class LoopCorridor : MonoBehaviour
{
    // --- 1. SETTINGS (ปรับได้) ---
    [Tooltip("🚨 ระยะที่ย้ายกลับ (เมตร) = ความยาวท่อ 1 ชุด")] public float loopLength = 20f;
    [Tooltip("true = ปลดล็อกแล้ว เดินผ่านได้ ไม่วนแล้ว")] public bool unlocked = false;
    [Tooltip("💡 ของที่จะโผล่ตอนปลดล็อก (เช่น ทางออกจริง)")] public GameObject[] showOnUnlock;
    [Tooltip("ของที่จะหายตอนปลดล็อก")] public GameObject[] hideOnUnlock;

    // --- 2. สถานะ ---
    public int LoopCount { get; private set; } // วนไปแล้วกี่รอบ (เผื่อใช้ทำเอฟเฟกต์ เช่น ยิ่งวนยิ่งมืด)

    // --- 3. เดินเข้ากล่อง = วนกลับ ---
    void OnTriggerEnter(Collider other)
    {
        if (unlocked) return;
        var player = other.GetComponentInParent<PlayerController25D>();
        if (player == null) return;

        Vector3 offset = new Vector3(-loopLength, 0f, 0f);

        // 3.1 ย้ายมาริสา (ทั้ง Rigidbody และ Transform จะได้ไม่กระตุก)
        var body = player.GetComponent<Rigidbody>();
        if (body != null) body.position += offset;
        player.transform.position += offset;

        // 3.2 ย้ายกล้องไปด้วยระยะเท่ากัน ภาพจะเหมือนเดิมเป๊ะ
        var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow25D>() : null;
        if (cam != null) cam.ShiftBy(offset);

        // 3.3 ย้ายปลาที่อยู่ในช่วงทางเดินวนไปด้วย ปลาจะได้ตามมาริสาต่อเนื่อง ไม่หายไป
        foreach (var fish in FishAI.All)
            if (fish.transform.position.x >= transform.position.x - loopLength * 2f && fish.transform.position.x <= transform.position.x + loopLength)
                fish.ShiftBy(offset);

        Physics.SyncTransforms();
        LoopCount++;
    }

    // --- 4. ปลดล็อก (LightReveal เรียกตอนส่องไฟฉายเจอสัญลักษณ์ลับ) ---
    public void Unlock()
    {
        if (unlocked) return;
        unlocked = true;
        foreach (var g in showOnUnlock) if (g != null) g.SetActive(true);
        foreach (var g in hideOnUnlock) if (g != null) g.SetActive(false);
    }
}
