using UnityEngine;

namespace AshenCanvas.Game.Cam
{
    /// <summary>Изометрическая камера над героиней: плавное следование и тряска.</summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public static CameraRig I { get; private set; }

        public Transform Target;
        public Vector3 Offset = new Vector3(0f, 15f, -10.5f);
        public float Follow = 8f;

        Camera cam;
        float trauma;

        public Camera Camera => cam;

        void Awake()
        {
            I = this;
            cam = GetComponent<Camera>();
            cam.fieldOfView = 45f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 200f;
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        public static void Shake(float amount)
        {
            if (I != null) I.trauma = Mathf.Min(1f, I.trauma + amount);
        }

        public void Snap()
        {
            if (Target == null) return;
            transform.position = Target.position + Offset;
            transform.LookAt(Target.position + Vector3.up * 1f);
        }

        void LateUpdate()
        {
            if (Target == null) return;
            var want = Target.position + Offset;
            transform.position = Vector3.Lerp(transform.position, want, 1f - Mathf.Exp(-Follow * Time.unscaledDeltaTime));
            transform.rotation = Quaternion.LookRotation((Target.position + Vector3.up * 1f) - want);

            if (trauma > 0f)
            {
                float s = trauma * trauma * 0.6f;
                float t = Time.unscaledTime * 40f;
                transform.position += transform.right * (Mathf.PerlinNoise(t, 0f) - 0.5f) * s
                                    + transform.up * (Mathf.PerlinNoise(0f, t) - 0.5f) * s;
                trauma = Mathf.Max(0f, trauma - Time.unscaledDeltaTime * 1.8f);
            }
        }

        /// <summary>Точка на полу (y = 0) под курсором.</summary>
        public bool GroundPoint(Vector2 screen, out Vector3 point)
        {
            var ray = cam.ScreenPointToRay(screen);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float d)) { point = ray.GetPoint(d); return true; }
            point = Vector3.zero;
            return false;
        }
    }
}
