using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ============================================================
// UIKit — ตัวช่วยสร้าง UI ด้วยโค้ด (ปุ่ม, ข้อความ, แถบเสียง)
// ใช้ร่วมกันทั้งเมนูหลัก, Pause และข้อความ "กด E" ไม่ต้องใส่ที่ object ไหน
// 💡 อยากเปลี่ยนสี/ขนาดทั้งเกม แก้ที่นี่ที่เดียว
// ============================================================
public static class UIKit
{
    public static readonly Color Dark = new Color(0.03f, 0.04f, 0.05f, 1f);
    public static readonly Color Light = new Color(0.92f, 0.9f, 0.82f, 1f);
    public static readonly Color ButtonColor = new Color(0.14f, 0.16f, 0.18f, 1f);
    public static readonly Color Accent = new Color(0.85f, 0.68f, 0.3f, 1f);
    const string VolumeKey = "MasterVolume";

    // ยังไม่มีฟอนต์ไทยของเกมเอง เลยใช้ของ Windows ไปก่อน (วรรณยุกต์แก้ด้วย ThaiToneFix)
    static Font font;
    public static Font UIFont { get { if (font == null) font = Font.CreateDynamicFontFromOSFont(new[] { "Tahoma", "Leelawadee UI", "Arial" }, 34); return font; } }

    public static Canvas CreateCanvas(string name, int order)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    // ปุ่มจะกดไม่ได้ถ้าไม่มี EventSystem ในฉาก
    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    public static Image Panel(Transform parent, Color color)
    {
        var img = new GameObject("Panel", typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        img.color = color;
        var r = img.rectTransform;
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        return img;
    }

    public static Text Label(Transform parent, string text, int size, Vector2 pos, Vector2 box)
    {
        var t = new GameObject("Label", typeof(Text)).GetComponent<Text>();
        t.transform.SetParent(parent, false);
        t.font = UIFont; t.fontSize = size; t.color = Light; t.text = text;
        t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.rectTransform.sizeDelta = box; t.rectTransform.anchoredPosition = pos;
        t.gameObject.AddComponent<ThaiToneFix>();
        return t;
    }

    public static Button Button(Transform parent, string text, Vector2 pos, UnityEngine.Events.UnityAction onClick, bool clickSound = true)
    {
        var img = new GameObject("Button_" + text, typeof(Image), typeof(Button)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        img.color = Color.white;
        img.rectTransform.sizeDelta = new Vector2(460, 78); img.rectTransform.anchoredPosition = pos;
        var b = img.GetComponent<Button>();
        var c = b.colors;
        c.normalColor = ButtonColor; c.highlightedColor = new Color(0.27f, 0.29f, 0.31f); c.selectedColor = new Color(0.27f, 0.29f, 0.31f);
        c.pressedColor = Accent; c.fadeDuration = 0.08f;
        b.colors = c;
        if (clickSound) b.onClick.AddListener(() => GameAudio.PlayUI(l => l.uiClick));
        b.onClick.AddListener(onClick);
        Label(img.transform, text, 34, Vector2.zero, new Vector2(460, 78));
        return b;
    }

    public static Slider VolumeSlider(Transform parent, Vector2 pos)
    {
        Label(parent, "เสียง", 30, pos + new Vector2(-170, 0), new Vector2(120, 50));
        var root = new GameObject("VolumeSlider", typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(parent, false);
        var rr = (RectTransform)root.transform; rr.sizeDelta = new Vector2(310, 40); rr.anchoredPosition = pos + new Vector2(65, 0);

        var bg = Panel(root.transform, new Color(0.25f, 0.27f, 0.29f)); SetBar(bg.rectTransform);
        var fill = Panel(root.transform, Accent); SetBar(fill.rectTransform);
        var handle = Panel(root.transform, Light);
        handle.rectTransform.sizeDelta = new Vector2(22, 0);

        var s = root.GetComponent<Slider>();
        s.targetGraphic = handle; s.fillRect = fill.rectTransform; s.handleRect = handle.rectTransform;
        s.minValue = 0f; s.maxValue = 1f;
        s.value = AudioListener.volume;
        s.onValueChanged.AddListener(v => { AudioListener.volume = v; PlayerPrefs.SetFloat(VolumeKey, v); });
        return s;
    }

    static void SetBar(RectTransform r)
    {
        r.anchorMin = new Vector2(0, 0.35f); r.anchorMax = new Vector2(1, 0.65f); r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
    }

    // เปิดเกมมา ใช้ความดังที่ตั้งไว้ครั้งก่อน
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void LoadVolume() { AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f); }
}
