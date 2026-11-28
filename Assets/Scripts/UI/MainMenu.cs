using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================
// MainMenu — หน้าเมนูหลัก (อยู่ในฉาก MainMenu)
// ============================================================
public class MainMenu : MonoBehaviour
{
    // --- SETTINGS (ปรับได้) ---
    [Tooltip("💡 รูปโลโก้ (ใส่แล้วจะโชว์แทนชื่อเกม)")] public Sprite logo;
    [Tooltip("โลโก้สูงกี่พิกเซล (จอ 1080)")] public float logoHeight = 440f;
    [Tooltip("💡 รูปตอนเปิดเกม ขึ้นก่อนเมนู 1 ครั้ง (เว้นว่าง = ข้าม)")] public Sprite openingImage;
    [Tooltip("ค้างรูปเปิดเกมกี่วินาที (กดปุ่มไหนก็ได้เพื่อข้าม)")] public float openingTime = 4f;
    static bool openingShown; // ขึ้นครั้งเดียวต่อการเปิดเกม กลับมาเมนูอีกไม่ขึ้นซ้ำ

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay() { openingShown = false; }

    [Tooltip("ชื่อเกม ใช้ตอนไม่มีโลโก้")] public string title = "HIDDEN";
    [Tooltip("🚨 ชื่อฉากที่จะโหลดตอนกดเริ่มเกม (ต้องอยู่ใน Build Settings)")] public string gameScene = "Game";

    void Start()
    {
        Time.timeScale = 1f; AudioListener.pause = false;
        Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
        PlayerCarryOver.Clear(); // เริ่มเกมใหม่ ไม่ต้องจำตำแหน่งเก่า

        UIKit.EnsureEventSystem();
        var canvas = UIKit.CreateCanvas("MainMenuCanvas", 10);
        var root = canvas.transform;
        UIKit.Panel(root, UIKit.Dark);
        if (logo != null)
        {
            var img = UIKit.Panel(root, Color.white);
            img.sprite = logo; img.raycastTarget = false;
            var r = img.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(logoHeight * logo.rect.width / logo.rect.height, logoHeight);
            r.anchoredPosition = new Vector2(0, 160);
        }
        else UIKit.Label(root, title, 130, new Vector2(0, 160), new Vector2(1400, 200));
        var first = UIKit.Button(root, "เริ่มเกม", new Vector2(0, -150), StartGame);
        UIKit.Button(root, "ออกจากเกม", new Vector2(0, -250), Quit);
        UIKit.VolumeSlider(root, new Vector2(0, -360));
        if (openingImage != null && !openingShown) StartCoroutine(Opening(canvas.gameObject, first));
        else first.Select();
    }

    // --- รูปเปิดเกม: ค่อยๆ ขึ้น → ค้าง (กดข้ามได้) → ค่อยๆ จาง แล้วค่อยโชว์เมนู ---
    IEnumerator Opening(GameObject menu, UnityEngine.UI.Button first)
    {
        openingShown = true;
        menu.SetActive(false); // ซ่อนเมนูไว้ก่อน กัน Enter/คลิกโดนปุ่มเริ่มเกม
        var root = UIKit.CreateCanvas("OpeningCanvas", 300).transform;
        UIKit.Panel(root, Color.black);
        var img = UIKit.Panel(root, new Color(1, 1, 1, 0));
        img.sprite = openingImage; img.preserveAspect = false;
        const float fade = 1f;
        for (float t = 0f; t < fade; t += Time.deltaTime) { img.color = new Color(1, 1, 1, t / fade); yield return null; }
        img.color = Color.white;
        for (float t = 0f; t < openingTime; t += Time.deltaTime) { if (t > 0.3f && (Input.anyKeyDown)) break; yield return null; }
        menu.SetActive(true);
        first.Select();
        var group = root.gameObject.AddComponent<CanvasGroup>();
        for (float t = 0f; t < fade; t += Time.deltaTime) { group.alpha = 1f - t / fade; yield return null; }
        Destroy(root.gameObject);
    }

    public void StartGame() { SceneManager.LoadScene(gameScene); }

    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
