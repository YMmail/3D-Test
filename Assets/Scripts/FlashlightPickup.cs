using UnityEngine;

// ============================================================
// FlashlightPickup — ไฟฉายที่วางอยู่บนพื้น เดินเข้าใกล้แล้วกด E เพื่อเก็บ
// ใส่ที่ object ไฟฉายบนพื้น ต้องมี Collider ที่ติ๊ก Is Trigger (เป็นวงให้เดินเข้า)
// 🚨 ตัวผู้เล่นต้องมี Tag = "Player" และมีสคริปต์ Flashlight
// ============================================================
public class FlashlightPickup : MonoBehaviour
{
    // --- 1. SETTINGS (ปรับได้) ---
    [Tooltip("ปุ่มเก็บ")] public KeyCode pickupKey = KeyCode.E;
    [Tooltip("ข้อความที่ขึ้นตอนยืนใกล้")] public string promptText = "กด E เพื่อเก็บไฟฉาย";
    [Tooltip("💡 ของที่จะซ่อนตอนเก็บแล้ว (เว้นว่าง = ซ่อนตัวนี้ทั้งก้อน)")] public GameObject hideOnPickup;

    // --- 2. ตัวแปรภายใน ---
    Flashlight playerFlashlight; // ไม่ใช่ null = ผู้เล่นยืนอยู่ในวง

    // --- 3. เดินเข้า/ออกวง ---
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerFlashlight = other.GetComponentInParent<Flashlight>();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) { playerFlashlight = null; InteractPrompt.Hide(this); }
    }

    // --- 4. กด E ---
    void Update()
    {
        if (playerFlashlight == null) return;
        InteractPrompt.Show(this, promptText);
        if (Input.GetKeyDown(pickupKey))
        {
            playerFlashlight.GiveFlashlight();
            playerFlashlight = null;
            InteractPrompt.Hide(this);
            (hideOnPickup != null ? hideOnPickup : gameObject).SetActive(false);
        }
    }

    // --- 5. โดนปิด (หยุดเกม / เก็บแล้ว) = ซ่อนข้อความ ---
    void OnDisable() { InteractPrompt.Hide(this); }
}
