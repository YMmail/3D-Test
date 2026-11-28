using UnityEngine;

// ============================================================
// WalkBob — ตัวโยกขึ้นลงตอนเดิน/วิ่ง (แทนท่าเดิน ระหว่างรอโมเดลที่มี rig)
// ใส่ที่ตัว Marisa แล้วลากโมเดลตัวละคร + ไฟฉายในมือ ใส่ช่อง Targets
// ============================================================
public class WalkBob : MonoBehaviour
{
    [Tooltip("ของที่จะโยก (โมเดลตัว, ไฟฉายในมือ)")] public Transform[] targets;
    [Tooltip("โยกสูงสุดกี่เมตร")] public float bobHeight = 0.035f;
    [Tooltip("ก้าวต่อเมตรที่เดิน (มาก = ก้าวถี่)")] public float stepsPerMeter = 1.6f;
    [Tooltip("ไฟฉายในมือแกว่งกี่องศา")] public float swingAngle = 6f;
    [Tooltip("ไฟฉายในมือ (แกว่งเพิ่ม)")] public Transform swingTarget;

    Vector3[] basePos;
    Quaternion swingBase;
    Rigidbody body;
    PlayerController25D controller;
    float phase, amount;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        controller = GetComponent<PlayerController25D>();
        basePos = new Vector3[targets.Length];
        for (int i = 0; i < targets.Length; i++) if (targets[i] != null) basePos[i] = targets[i].localPosition;
        if (swingTarget != null) swingBase = swingTarget.localRotation;
    }

    void LateUpdate()
    {
        Vector3 v = body != null ? body.linearVelocity : Vector3.zero;
        float speed = new Vector2(v.x, v.z).magnitude;
        bool grounded = controller == null || controller.IsGrounded;
        float want = grounded && speed > 0.2f ? Mathf.Clamp01(speed / 3f) : 0f;
        amount = Mathf.MoveTowards(amount, want, Time.deltaTime * 4f);
        phase += speed * stepsPerMeter * Mathf.PI * Time.deltaTime;
        float y = Mathf.Abs(Mathf.Sin(phase)) * bobHeight * amount;
        for (int i = 0; i < targets.Length; i++) if (targets[i] != null) targets[i].localPosition = basePos[i] + Vector3.up * y;
        if (swingTarget != null) swingTarget.localRotation = swingBase * Quaternion.Euler(Mathf.Sin(phase) * swingAngle * amount, 0f, 0f);
    }
}
