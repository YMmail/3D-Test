using UnityEngine;

// ============================================================
// LightingZone — เปลี่ยนความมืด/สว่างของฉากตามโซน (ค่อยๆ เปลี่ยน ไม่วูบ)
// ใส่ที่ GameObject ที่มี Box Collider (ติ๊ก Is Trigger) คลุมพื้นที่ของโซน
// 🚨 เช็คจาก "จุดกลางตัวมาริสา" อยู่ในกล่องไหน → กล่องแต่ละโซนห้ามทับกัน (เดินกลับไปโซนเดิมก็กลับมาสว่าง)
// 💡 โซนไหนอยากมืด ตั้ง Sun Intensity กับ Ambient ต่ำๆ แล้วใส่ไฟสลัวเป็นจุดๆ แทน
// ============================================================
[RequireComponent(typeof(Collider))]
public class LightingZone : MonoBehaviour
{
    // --- 1. SETTINGS (ปรับได้) ---
    [Tooltip("ความสว่างของแสงหลัก (Directional Light) ในโซนนี้")] public float sunIntensity = 1f;
    [Tooltip("ความสว่างของแสงรอบๆ (Ambient) ในโซนนี้")] public float ambientIntensity = 1f;
    [Tooltip("ความสว่างของแสงสะท้อน")] public float reflectionIntensity = 1f;
    [Tooltip("เปลี่ยนเสร็จภายในกี่วินาที")] public float blendTime = 1.5f;

    // --- 2. โซนที่ใช้อยู่ตอนนี้ ---
    static LightingZone active;
    static Light sun;
    static Transform player;
    Collider box;

    void Awake() { box = GetComponent<Collider>(); }

    // --- 3. ทุกเฟรม: จุดกลางตัวมาริสาอยู่ในโซนนี้ = ใช้ค่าของโซนนี้ ---
    void Update()
    {
        if (player == null)
        {
            var pc = FindFirstObjectByType<PlayerController25D>();
            if (pc == null) return;
            player = pc.transform;
        }
        if (box.bounds.Contains(player.position + Vector3.up * 0.6f)) active = this;
        if (active != this) return;

        // --- 4. ค่อยๆ ปรับไปหาค่าของโซนนี้ ---
        if (sun == null)
        {
            sun = RenderSettings.sun;
            if (sun == null)
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (l.type == LightType.Directional) { sun = l; break; }
        }
        float k = Time.deltaTime / Mathf.Max(0.01f, blendTime) * 3f;
        if (sun != null) sun.intensity = Mathf.Lerp(sun.intensity, sunIntensity, k);
        RenderSettings.ambientIntensity = Mathf.Lerp(RenderSettings.ambientIntensity, ambientIntensity, k);
        RenderSettings.reflectionIntensity = Mathf.Lerp(RenderSettings.reflectionIntensity, reflectionIntensity, k);
    }
}
