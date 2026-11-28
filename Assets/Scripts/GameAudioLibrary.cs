using UnityEngine;

// ============================================================
// GameAudioLibrary — รวมเสียงทั้งเกมไว้ที่เดียว
// 💡 เปลี่ยนเสียง: คลิก Assets/Resources/GameAudioLibrary แล้วลากไฟล์ใหม่ใส่ช่อง (เว้นว่าง = ไม่มีเสียง)
// ============================================================
[CreateAssetMenu(menuName = "Game/Audio Library")]
public class GameAudioLibrary : ScriptableObject
{
    [System.Serializable]
    public class Sound
    {
        public AudioClip[] clips;                  // ใส่หลายอันได้ จะสุ่มเล่น
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Header("เล่นวน")]
    public Sound ambience = new Sound();
    public Sound cratePush = new Sound();

    [Header("มาริสา")]
    public Sound footstep = new Sound();
    [Tooltip("ก้าวเท้าตอนยืนบนลัง/พื้นไม้ (เว้นว่าง = ใช้ footstep)")] public Sound footstepWood = new Sound();
    public Sound jump = new Sound();
    public Sound land = new Sound();
    [Tooltip("เปิดไฟฉาย")] public Sound flashlight = new Sound();
    [Tooltip("ปิดไฟฉาย (เว้นว่าง = ใช้เสียงเปิด)")] public Sound flashlightOff = new Sound();
    public Sound pickup = new Sound();
    public Sound death = new Sound();

    [Header("ปลา")]
    public Sound fishWarn = new Sound();
    public Sound fishLunge = new Sound();

    [Header("แผงรหัส / ตะแกรง")]
    public Sound keyPress = new Sound();
    public Sound codeWrong = new Sound();
    public Sound codeRight = new Sound();
    public Sound gateOpen = new Sound();

    [Header("เมนู")]
    public Sound uiClick = new Sound();
}
