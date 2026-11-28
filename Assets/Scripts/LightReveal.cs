using UnityEngine;

// ============================================================
// LightReveal — ของลับที่มองเห็นเฉพาะตอนส่องไฟฉายใส่
// ส่องค้างไว้ครบเวลา = "เจอแล้ว" → ปลดล็อกทางเดินวน (LoopCorridor)
// ใส่ที่สัญลักษณ์บนผนัง (ไม่ต้องมี Collider ไม่งั้นบังแสงตัวเอง)
// ============================================================
public class LightReveal : MonoBehaviour
{
    // --- 1. SETTINGS (ปรับได้) ---
    [Tooltip("ส่องค้างกี่วินาทีถึงนับว่าเจอ")] public float holdTime = 1.0f;
    [Tooltip("💡 เจอแล้วให้เห็นตลอดไหม (false = เห็นเฉพาะตอนส่อง)")] public bool stayVisibleAfterReveal = true;
    [Tooltip("🚨 ทางเดินวนที่จะปลดล็อกตอนเจอ")] public LoopCorridor unlockLoop;
    [Tooltip("Renderer ที่จะซ่อน/โชว์ (เว้นว่าง = ทุกอันในตัวนี้)")] public Renderer[] renderers;

    // --- 2. สถานะ ---
    public bool Revealed { get; private set; }
    float litTime;

    // --- 3. เริ่มต้น: ซ่อนไว้ก่อน ---
    void Awake()
    {
        if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<Renderer>(true);
        SetVisible(false);
    }

    // --- 4. ทุกเฟรม: เช็คว่าโดนไฟฉายไหม ---
    void Update()
    {
        bool lit = Flashlight.Instance != null && Flashlight.Instance.IsLighting(transform.position);
        litTime = lit ? litTime + Time.deltaTime : 0f;

        if (!Revealed && litTime >= holdTime)
        {
            Revealed = true;
            if (unlockLoop != null) unlockLoop.Unlock();
        }
        SetVisible(lit || (Revealed && stayVisibleAfterReveal));
    }

    void SetVisible(bool v)
    {
        foreach (var r in renderers) if (r != null) r.enabled = v;
    }
}
