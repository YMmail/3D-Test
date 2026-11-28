using UnityEngine;

// ============================================================
// Flashlight — ไฟฉายของมาริสา
// กด F เปิด/ปิด (ต้องเก็บไฟฉายก่อน) / ปลาเรียก IsLighting() เพื่อเช็คว่าโดนแสงไหม
// ใส่สคริปต์นี้ที่ตัว Marisa แล้วลาก Spot Light (ลูกของ Model) ใส่ช่อง spotLight
// ============================================================
public class Flashlight : MonoBehaviour
{
    // --- 1. SETTINGS (ปรับได้) ---
    [Header("ไฟ")]
    [Tooltip("🚨 Spot Light ที่เป็นไฟฉาย (ควรเป็นลูกของ Model จะได้หันตามตัว)")] public Light spotLight;
    [Tooltip("ปุ่มเปิด/ปิด")] public KeyCode toggleKey = KeyCode.F;
    [Tooltip("เริ่มเกมมามีไฟฉายเลยไหม (โซน 1 ต้องเก็บก่อน = false)")] public bool hasFlashlight = false;
    [Tooltip("เก็บแล้วเปิดไฟให้อัตโนมัติไหม")] public bool turnOnWhenPicked = true;
    [Tooltip("💡 โมเดลไฟฉายในมือ (โชว์เมื่อเก็บไฟฉายแล้ว)")] public GameObject heldModel;

    [Header("ให้ปลาเช็ค")]
    [Tooltip("💡 ปลาโดนแสงถ้าอยู่ในกรวยแสงที่แคบกว่าแสงจริงนิดนึง (0.8 = 80% ของมุม)")] public float hitConeScale = 0.8f;
    [Tooltip("Layer ที่บังแสงได้ (ผนัง, ลัง)")] public LayerMask blockMask = ~0;

    // --- 2. สถานะ ---
    public static Flashlight Instance { get; private set; } // ให้ปลาเรียกได้ง่ายๆ: Flashlight.Instance
    public bool IsOn => hasFlashlight && spotLight != null && spotLight.enabled;

    // --- 3. เริ่มต้น ---
    void Awake()
    {
        Instance = this;
        if (spotLight == null) spotLight = GetComponentInChildren<Light>(true);
        if (spotLight != null) spotLight.enabled = false;
    }

    // --- 4. กด F ---
    void Update()
    {
        if (heldModel != null && heldModel.activeSelf != hasFlashlight) heldModel.SetActive(hasFlashlight);
        if (!hasFlashlight || spotLight == null) return;
        if (Input.GetKeyDown(toggleKey)) spotLight.enabled = !spotLight.enabled;
    }

    // --- 5. เรียกตอนเก็บไฟฉาย (FlashlightPickup เรียกให้) ---
    public void GiveFlashlight()
    {
        hasFlashlight = true;
        if (spotLight != null && turnOnWhenPicked) spotLight.enabled = true;
    }

    // --- 6. เช็คว่าจุดนี้โดนไฟฉายส่องอยู่ไหม (ปลาใช้) ---
    public bool IsLighting(Vector3 point) { return IsLighting(point, hitConeScale); }

    // coneScale = ใช้กรวยแสงกี่ส่วน (1 = เต็มกรวยที่เห็นจริง — ของลับใช้ค่านี้)
    public bool IsLighting(Vector3 point, float coneScale)
    {
        if (!IsOn) return false;
        Transform t = spotLight.transform;
        Vector3 toPoint = point - t.position;
        float dist = toPoint.magnitude;
        if (dist > spotLight.range) return false;                                    // ไกลเกิน
        float angle = Vector3.Angle(t.forward, toPoint);
        if (angle > spotLight.spotAngle * 0.5f * coneScale) return false;         // นอกกรวยแสง
        // มีอะไรบังไหม (ไม่นับ Trigger)
        if (Physics.Raycast(t.position, toPoint.normalized, out RaycastHit hit, dist - 0.1f, blockMask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.transform.IsChildOf(transform)) return false;
        }
        return true;
    }
}
