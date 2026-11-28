using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================
// EndingTrigger — เดินเข้ามา = จบเกม → จอค่อยๆ ดำ → ข้อความจบ → เมนูหลัก
// ใส่ที่ GameObject ที่มี Box Collider ติ๊ก Is Trigger (หลังตะแกรง)
// ============================================================
[RequireComponent(typeof(Collider))]
public class EndingTrigger : MonoBehaviour
{
    // --- SETTINGS (ปรับได้) ---
    [Tooltip("ข้อความตอนจบ (ใช้ตอนไม่มีรูป)")] public string endText = "จบ";
    [Tooltip("💡 รูปฉากจบ เช่น THE TRUTH SURFACES (เว้นว่าง = ใช้ข้อความ)")] public Sprite endImage;
    [Tooltip("💡 รูปเครดิตทีม ขึ้นต่อจากรูปฉากจบ (เว้นว่าง = ข้าม)")] public Sprite creditsImage;
    public float fadeTime = 2f;
    [Tooltip("ค้างข้อความ/รูปฉากจบกี่วินาที")] public float holdTime = 4f;
    [Tooltip("ค้างรูปเครดิตกี่วินาที")] public float creditsHoldTime = 6f;
    [Tooltip("รูปค่อยๆ ขึ้น/จางกี่วินาที")] public float imageFadeTime = 1f;

    // กำลังเล่นฉากจบ = กดหยุดเกมไม่ได้
    public static bool Playing { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay() { Playing = false; }

    void OnTriggerEnter(Collider other)
    {
        if (Playing) return;
        var player = other.GetComponentInParent<PlayerController25D>();
        if (player == null) return;
        if (PlayerRespawn.Instance != null && PlayerRespawn.Instance.IsDead) return;
        Playing = true;
        player.canMove = false;
        StartCoroutine(EndRoutine());
    }

    IEnumerator EndRoutine()
    {
        var root = UIKit.CreateCanvas("EndingCanvas", 200).transform;
        var black = UIKit.Panel(root, new Color(0, 0, 0, 0));
        for (float t = 0f; t < fadeTime; t += Time.deltaTime) { black.color = new Color(0, 0, 0, t / fadeTime); yield return null; }
        black.color = Color.black;

        // 1. รูปฉากจบ (ไม่มีรูป = ข้อความแบบเดิม)
        if (endImage != null) yield return ShowImage(root, endImage, holdTime);
        else { UIKit.Label(root, endText, 110, Vector2.zero, new Vector2(1500, 200)); yield return new WaitForSeconds(holdTime); }

        // 2. เครดิตทีม
        if (creditsImage != null) yield return ShowImage(root, creditsImage, creditsHoldTime);

        SceneManager.LoadScene(PauseMenu.MenuScene);
    }

    // รูปเต็มจอ ค่อยๆ ขึ้น → ค้าง → ค่อยๆ จาง
    IEnumerator ShowImage(Transform root, Sprite sprite, float hold)
    {
        var img = UIKit.Panel(root, new Color(1, 1, 1, 0));
        img.sprite = sprite; img.preserveAspect = false; img.raycastTarget = false;
        for (float t = 0f; t < imageFadeTime; t += Time.deltaTime) { img.color = new Color(1, 1, 1, t / imageFadeTime); yield return null; }
        img.color = Color.white;
        yield return new WaitForSeconds(hold);
        for (float t = 0f; t < imageFadeTime; t += Time.deltaTime) { img.color = new Color(1, 1, 1, 1f - t / imageFadeTime); yield return null; }
        Destroy(img.gameObject);
    }

    void OnDestroy() { Playing = false; }
}
