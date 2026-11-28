using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================
// PlayerCarryOver — ให้มาริสาเปลี่ยนฉากแล้ว "ยืนต่อที่เดิม" ไม่วาร์ป
// ใส่ที่ตัว Marisa (อยู่ใน prefab แล้ว)
// หลักการ: จำตำแหน่ง/ทิศที่หัน/ความเร็วไว้ทุกเฟรม → พอโหลดฉากใหม่ ก็วางตัวที่เดิม
// 🚨 ฉากที่ต่อกันต้องใช้พิกัดเดียวกัน (เช่น ขั้นสูงปลายโซน 1 = ขั้นสูงต้นโซน 2 ที่ X 19.5–22)
// 💡 เริ่มเกมใหม่ / เปิดฉากตรงๆ ใน Editor = ใช้ตำแหน่งที่วางไว้ในฉากตามปกติ
// ============================================================
[DefaultExecutionOrder(-100)] // ทำงานก่อนสคริปต์อื่น กล้องจะได้เห็นตำแหน่งใหม่ตั้งแต่แรก
public class PlayerCarryOver : MonoBehaviour
{
    // --- 1. SETTINGS ---
    [Tooltip("ลูกที่เป็นโมเดล (เอาไว้จำทิศที่หัน)")] public Transform model;
    [Tooltip("จำความเร็วข้ามฉากด้วยไหม (เดิน/ตกต่อได้ลื่นๆ)")] public bool carryVelocity = true;

    // --- 2. ข้อมูลที่จำข้ามฉาก (static = ไม่หายตอนเปลี่ยนฉาก) ---
    static bool hasData;
    static string fromScene;
    static Vector3 savedPosition;
    static Quaternion savedModelRotation;
    static Vector3 savedVelocity;
    static bool savedHasFlashlight, savedLightOn; // จำว่าเก็บไฟฉายแล้วไหม / เปิดอยู่ไหม
    bool applyFlashlight;

    Rigidbody body;

    // --- 3. ฉากใหม่เริ่ม: ถ้ามาจากฉากอื่น วางตัวที่เดิม ---
    void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (model == null) model = transform.Find("Model");

        string here = SceneManager.GetActiveScene().name;
        if (hasData && fromScene != here)
        {
            transform.position = savedPosition;
            if (body != null)
            {
                body.position = savedPosition;
                body.linearVelocity = carryVelocity ? savedVelocity : Vector3.zero;
            }
            if (model != null) model.rotation = savedModelRotation;
            applyFlashlight = true;
        }
        hasData = false;
    }

    // --- 4. จำไว้ทุกเฟรม (เผื่อโดนโหลดฉากตอนไหนก็ได้) ---
    void LateUpdate()
    {
        hasData = true;
        fromScene = gameObject.scene.name;
        savedPosition = transform.position;
        savedModelRotation = model != null ? model.rotation : Quaternion.identity;
        savedVelocity = body != null ? body.linearVelocity : Vector3.zero;
        var fl = GetComponent<Flashlight>();
        if (fl != null) { savedHasFlashlight = fl.hasFlashlight; savedLightOn = fl.IsOn; }
    }

    // --- 4.1 ไฟฉาย: ต้องทำหลัง Flashlight.Awake (ที่ปิดไฟตอนเริ่ม) เลยทำใน Start ---
    void Start()
    {
        if (!applyFlashlight) return;
        var fl = GetComponent<Flashlight>();
        if (fl == null) return;
        fl.hasFlashlight = savedHasFlashlight;
        if (fl.spotLight != null) fl.spotLight.enabled = savedHasFlashlight && savedLightOn;
    }

    // --- 5. ถ้าตายแล้วโหลดฉากเดิมใหม่ ไม่ต้องจำ (เริ่มที่จุดเกิด) ---
    public static void Clear() { hasData = false; }

    // --- 6. กด Play ใหม่ = ล้างของรอบก่อน (โปรเจกต์ปิด Domain Reload ค่า static เลยไม่หายเอง) ---
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay() { hasData = false; fromScene = null; savedHasFlashlight = false; savedLightOn = false; }
}
