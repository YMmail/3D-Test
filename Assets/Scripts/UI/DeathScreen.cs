using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ============================================================
// DeathScreen — หน้าตาย (PlayerRespawn สร้างและสั่งให้เอง ไม่ต้องใส่ฉาก)
// ============================================================
public class DeathScreen : MonoBehaviour
{
    Image black, art;
    GameObject content;
    Button retry;
    Sprite artSprite;
    public bool Chosen { get; private set; } // ผู้เล่นกด "ลองใหม่" แล้ว
    public bool MenuShown => content.activeSelf;

    public static DeathScreen Create(string title, Sprite background)
    {
        UIKit.EnsureEventSystem();
        var canvas = UIKit.CreateCanvas("DeathCanvas", 150);
        var d = canvas.gameObject.AddComponent<DeathScreen>();
        d.artSprite = background;
        d.black = UIKit.Panel(canvas.transform, Color.clear);
        d.content = new GameObject("Content", typeof(RectTransform));
        d.content.transform.SetParent(canvas.transform, false);
        var r = (RectTransform)d.content.transform;
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        if (background != null) { d.art = UIKit.Panel(r, Color.white); d.art.sprite = background; d.art.preserveAspect = false; }
        // ข้อความว่าง = ไม่ต้องเขียนทับ (รูป Game Over มีตัวหนังสือในรูปแล้ว)
        if (!string.IsNullOrEmpty(title))
            UIKit.Label(r, title, 110, new Vector2(0, 140), new Vector2(1500, 180)).color = new Color(0.75f, 0.12f, 0.1f);
        // มีรูป = ปุ่มลงไปอยู่ใต้คำว่า Game Over กลางจอ
        float y = background != null ? -230f : -70f;
        d.retry = UIKit.Button(r, "ลองใหม่", new Vector2(0, y), () => d.Chosen = true);
        UIKit.Button(r, "เมนูหลัก", new Vector2(0, y - 100f), () => SceneManager.LoadScene(PauseMenu.MenuScene));
        d.content.SetActive(false);
        d.gameObject.SetActive(false);
        return d;
    }

    // ความทึบของจอดำ 0–1
    public void SetFade(float a)
    {
        gameObject.SetActive(a > 0f);
        black.color = new Color(0f, 0f, 0f, a);
    }

    public void ShowMenu()
    {
        Chosen = false;
        Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
        content.SetActive(true);
        retry.Select();
    }

    public void HideMenu() { content.SetActive(false); }
}
