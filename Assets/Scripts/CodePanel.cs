using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ============================================================
// CodePanel — แผงใส่รหัส (ป้าย Ans) เดินเข้าใกล้ กด E แล้วใส่เลข
// ใส่ที่ป้าย ต้องมี Collider ติ๊ก Is Trigger เป็นวงให้เดินเข้า
// ใส่ถูก → เรียก onCorrect (ลากตะแกรงมาใส่ เลือก GateDoor.Open)
// ใช้เมาส์กดปุ่ม หรือพิมพ์เลข / Backspace / Enter / Esc ก็ได้
// ============================================================
[RequireComponent(typeof(Collider))]
public class CodePanel : MonoBehaviour
{
    // --- 1. SETTINGS (ปรับได้) ---
    [Tooltip("🚨 รหัสที่ถูก (ตัวเลขเท่านั้น) ต้องตรงกับเลขลับในโซน 2")] public string code = "472";
    [Tooltip("ปุ่มเปิดแผง")] public KeyCode openKey = KeyCode.E;
    public string promptText = "กด E เพื่อใส่รหัส";
    [Tooltip("โชว์ผลถูก/ผิดกี่วินาที")] public float resultTime = 0.8f;
    public UnityEvent onCorrect = new UnityEvent();
    [Tooltip("💡 ใส่ผิดแล้วให้เกิดอะไร (เช่น เสียงเตือน)")] public UnityEvent onWrong = new UnityEvent();

    // --- 2. สถานะ ---
    public bool IsSolved { get; private set; }
    public bool IsOpen { get; private set; }
    public string Typed { get; private set; } = "";
    static CodePanel current;
    static int closedFrame = -1;
    // กด Esc ปิดแผงแล้ว ต้องไม่เด้งเมนูหยุดเกมซ้อน
    public static bool BlocksPause => (current != null && current.IsOpen) || closedFrame == Time.frameCount;

    PlayerController25D player; // ไม่ใช่ null = ยืนอยู่ในวง
    Canvas canvas;
    Text display;
    bool busy; // กำลังโชว์ผลถูก/ผิด
    readonly List<Behaviour> frozen = new List<Behaviour>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay() { current = null; closedFrame = -1; }

    // --- 3. เดินเข้า/ออกวง ---
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) player = other.GetComponentInParent<PlayerController25D>();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        player = null;
        InteractPrompt.Hide(this);
    }

    // --- 4. ทุกเฟรม ---
    void Update()
    {
        if (IsOpen) { ReadKeyboard(); return; }
        if (player == null || IsSolved) return;
        InteractPrompt.Show(this, promptText);
        if (Input.GetKeyDown(openKey)) Open();
    }

    void ReadKeyboard()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
        for (int i = 0; i <= 9; i++)
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i)) TypeDigit(i);
        if (Input.GetKeyDown(KeyCode.Backspace)) Erase();
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Submit();
    }

    // --- 5. เปิด/ปิดแผง (ตอนเปิด มาริสาหยุดเดิน) ---
    public void Open()
    {
        if (IsOpen || IsSolved) return;
        if (PlayerRespawn.Instance != null && PlayerRespawn.Instance.IsDead) return;
        if (canvas == null) Build();
        IsOpen = true; current = this; Typed = ""; busy = false;
        InteractPrompt.Hide(this);
        var pc = player != null ? player : FindFirstObjectByType<PlayerController25D>();
        if (pc != null)
        {
            Freeze(pc); Freeze(pc.GetComponent<Flashlight>());
            var body = pc.GetComponent<Rigidbody>();
            if (body != null) body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);
        }
        canvas.gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (!IsOpen) return;
        StopAllCoroutines();
        IsOpen = false; busy = false; closedFrame = Time.frameCount;
        if (current == this) current = null;
        if (canvas != null) canvas.gameObject.SetActive(false);
        foreach (var b in frozen) if (b != null) b.enabled = true;
        frozen.Clear();
    }

    void Freeze(Behaviour b) { if (b != null && b.enabled) { b.enabled = false; frozen.Add(b); } }

    // --- 6. ใส่เลข ---
    public void TypeDigit(int d) { if (busy || Typed.Length >= code.Length) return; Typed += d; Refresh(); GameAudio.PlayUI(l => l.keyPress); }
    public void Erase() { if (busy || Typed.Length == 0) return; Typed = Typed.Substring(0, Typed.Length - 1); Refresh(); }
    public void Submit() { if (busy || Typed.Length < code.Length) return; StartCoroutine(Check()); }

    IEnumerator Check()
    {
        busy = true;
        bool ok = Typed == code;
        display.color = ok ? new Color(0.45f, 0.9f, 0.45f) : new Color(0.95f, 0.3f, 0.25f);
        if (ok) IsSolved = true;
        if (ok) GameAudio.PlayUI(l => l.codeRight); else GameAudio.PlayUI(l => l.codeWrong);
        yield return new WaitForSeconds(resultTime);
        if (ok) { Close(); InteractPrompt.Hide(this); onCorrect.Invoke(); }
        else { busy = false; Typed = ""; Refresh(); onWrong.Invoke(); }
    }

    void Refresh()
    {
        display.color = UIKit.Light;
        string s = "";
        for (int i = 0; i < code.Length; i++) s += (i < Typed.Length ? Typed[i].ToString() : "_") + (i < code.Length - 1 ? "  " : "");
        display.text = s;
    }

    // --- 7. สร้างหน้าแผง ---
    void Build()
    {
        UIKit.EnsureEventSystem();
        canvas = UIKit.CreateCanvas("CodePanelCanvas", 80);
        canvas.transform.SetParent(transform, false);
        var root = canvas.transform;
        UIKit.Panel(root, new Color(0f, 0f, 0f, 0.8f));
        display = UIKit.Label(root, "", 110, new Vector2(0, 310), new Vector2(700, 150));
        for (int i = 1; i <= 9; i++)
        {
            int d = i;
            Key(root, d.ToString(), new Vector2(((d - 1) % 3 - 1) * 150, 140 - (d - 1) / 3 * 120), () => TypeDigit(d));
        }
        Key(root, "ลบ", new Vector2(-150, -220), Erase);
        Key(root, "0", new Vector2(0, -220), () => TypeDigit(0));
        Key(root, "ตกลง", new Vector2(150, -220), Submit);
        UIKit.Label(root, "Esc ปิด", 28, new Vector2(0, -340), new Vector2(400, 50)).color = new Color(1, 1, 1, 0.5f);
    }

    void Key(Transform root, string text, Vector2 pos, UnityAction act)
    {
        var b = UIKit.Button(root, text, pos, () =>
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null); // กัน Enter/Space กดปุ่มเดิมซ้ำ
            act();
        }, false);
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        ((RectTransform)b.transform).sizeDelta = new Vector2(136, 106);
    }

    // --- 8. โดนปิด (หยุดเกม / เปลี่ยนฉาก) ---
    void OnDisable() { Close(); InteractPrompt.Hide(this); }
}
