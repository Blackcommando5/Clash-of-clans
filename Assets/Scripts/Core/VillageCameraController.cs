using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Kingdoms
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class VillageCameraController : MonoBehaviour
    {
        [Header("View")]
        [Range(25f, 80f)] public float elevation = 45f;
        public float azimuth = 45f;
        [Min(10f)] public float distance = 100f;
        public Vector3 focus = Vector3.zero;
        public float groundHeight;
        [Header("Ground bounds (world X / Z)")]
        public Vector2 groundMin = new Vector2(-60f, -60f);
        public Vector2 groundMax = new Vector2(60f, 60f);
        [Header("Zoom (orthographic half-height)")]
        [Min(1f)] public float minimumZoom = 8f;
        [Min(1f)] public float maximumZoom = 32f;
        [Min(1f)] public float startingZoom = 24f;
        [Min(0f)] public float wheelSensitivity = 0.0015f;
        [Header("Release glide")]
        [Min(0f)] public float damping = 10f;
        [Min(0f)] public float maximumGlideSpeed = 45f;

        Camera view;
        Vector3 velocity;
        Vector2 previousPosition;
        float previousSpan;
        int previousCount, previousFirst = -1, previousSecond = -1;
        bool gestureBlocked;
        readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        PointerEventData uiPointer;
        EventSystem pointerSystem;

        void OnEnable()
        {
            view = GetComponent<Camera>();
            EnhancedTouchSupport.Enable();
            view.orthographic = true;
            view.orthographicSize = startingZoom;
            view.nearClipPlane = 0.1f;
            view.farClipPlane = Mathf.Max(300f, distance * 3f);
            ResetGesture();
            ApplyView();
        }

        void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            ResetGesture();
        }

        void OnApplicationFocus(bool hasFocus) { if (!hasFocus) ResetGesture(); }

        void ResetGesture()
        {
            previousCount = 0;
            previousFirst = previousSecond = -1;
            gestureBlocked = false;
            velocity = Vector3.zero;
        }

        public bool InputBlocked { get; set; }

        void LateUpdate()
        {
            ApplyView(); // Respond to aspect-ratio and Inspector changes.
            if (InputBlocked) { ResetGesture(); return; }
            var touches = Touch.activeTouches;
            int count = 0, first = -1, second = -1;
            Vector2 a = default, b = default;
            bool overUI = false;
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (!touch.inProgress) continue;
                if (count == 0) { a = touch.screenPosition; first = touch.touchId; }
                else if (count == 1) { b = touch.screenPosition; second = touch.touchId; }
                overUI |= IsOverUI(touch.screenPosition);
                count++;
            }
            if (count > 0)
            {
                Gesture(count, first, second, count == 1 ? a : (a + b) * 0.5f,
                    count == 2 ? Vector2.Distance(a, b) : 0f, overUI);
                return;
            }

            var mouse = Mouse.current;
            if (previousFirst >= 0)
            {
                // Retain release glide, but don't carry a touch gesture into mouse input.
                previousCount = 0;
                previousFirst = previousSecond = -1;
                gestureBlocked = false;
            }
            if (mouse != null)
            {
                Vector2 p = mouse.position.ReadValue();
                if (mouse.leftButton.isPressed || mouse.middleButton.isPressed)
                {
                    Gesture(1, -2, -1, p, 0f, IsOverUI(p));
                    return;
                }
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f && !IsOverUI(p))
                {
                    ZoomAt(p, Mathf.Exp(-scroll * wheelSensitivity));
                    velocity = Vector3.zero;
                }
            }
            previousCount = 0;
            gestureBlocked = false;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            // Integrate exponential decay instead of a frame-dependent multiplier.
            float decay = Mathf.Exp(-Mathf.Max(0.01f, damping) * dt);
            focus += velocity * ((1f - decay) / Mathf.Max(0.01f, damping));
            velocity *= decay;
            if (velocity.sqrMagnitude < 0.0001f) velocity = Vector3.zero;
            ApplyView();
        }

        void Gesture(int count, int first, int second, Vector2 p, float span, bool overUI)
        {
            bool changed = count != previousCount || first != previousFirst || second != previousSecond;
            if (changed)
            {
                // Rebase one/two-finger transitions to avoid jumps.
                gestureBlocked |= overUI || count > 2;
                velocity = Vector3.zero;
            }
            else if (!gestureBlocked)
            {
                Vector3 before = focus;
                focus += GroundPoint(previousPosition) - GroundPoint(p);
                ApplyView();
                float dt = Mathf.Max(Time.unscaledDeltaTime, 0.001f);
                velocity = Vector3.ClampMagnitude((focus - before) / dt, maximumGlideSpeed);
                if (count == 2 && previousSpan > 1f && span > 1f)
                {
                    ZoomAt(p, previousSpan / span);
                    velocity = Vector3.zero;
                }
            }
            previousPosition = p;
            previousSpan = span;
            previousCount = count;
            previousFirst = first;
            previousSecond = second;
        }

        void ZoomAt(Vector2 screenPosition, float factor)
        {
            Vector3 anchor = GroundPoint(screenPosition);
            view.orthographicSize *= factor;
            ApplyView();
            focus += anchor - GroundPoint(screenPosition);
            ApplyView();
        }

        Vector3 GroundPoint(Vector2 screenPosition)
        {
            Ray ray = view.ScreenPointToRay(screenPosition);
            var plane = new Plane(Vector3.up, new Vector3(0f, groundHeight, 0f));
            return plane.Raycast(ray, out float length) ? ray.GetPoint(length) : focus;
        }

        bool IsOverUI(Vector2 position)
        {
            var system = EventSystem.current;
            if (system == null) return false;
            if (uiPointer == null || pointerSystem != system)
            {
                uiPointer = new PointerEventData(system);
                pointerSystem = system;
            }
            uiPointer.Reset();
            uiPointer.position = position;
            uiHits.Clear();
            system.RaycastAll(uiPointer, uiHits);
            // World colliders must not block panning.
            for (int i = 0; i < uiHits.Count; i++)
                if (uiHits[i].module is UnityEngine.UI.GraphicRaycaster) return true;
            return false;
        }

        void ApplyView()
        {
            transform.rotation = Quaternion.Euler(Mathf.Clamp(elevation, 25f, 80f), azimuth, 0f);
            Vector3 forward = transform.forward;
            Vector3 projectedUp = transform.up - forward * (transform.up.y / forward.y);
            Vector3 right = transform.right * Mathf.Max(0.01f, view.aspect);
            // Ground footprint of all four screen corners, for one unit of zoom.
            Vector2 footprint = new Vector2(Mathf.Abs(right.x) + Mathf.Abs(projectedUp.x),
                Mathf.Abs(right.z) + Mathf.Abs(projectedUp.z));
            Vector2 lo = Vector2.Min(groundMin, groundMax);
            Vector2 hi = Vector2.Max(groundMin, groundMax);
            Vector2 half = Vector2.Max((hi - lo) * 0.5f, Vector2.one * 0.1f);
            float fitZoom = Mathf.Min(half.x / footprint.x, half.y / footprint.y) * 0.999f;
            float upper = Mathf.Max(0.01f, Mathf.Min(Mathf.Max(minimumZoom, maximumZoom), fitZoom));
            view.orthographicSize = Mathf.Clamp(view.orthographicSize, Mathf.Min(minimumZoom, upper), upper);
            Vector2 extent = footprint * view.orthographicSize;
            Vector3 unclamped = focus;
            focus.x = Mathf.Clamp(focus.x, lo.x + extent.x, hi.x - extent.x);
            focus.z = Mathf.Clamp(focus.z, lo.y + extent.y, hi.y - extent.y);
            focus.y = groundHeight;
            if (!Mathf.Approximately(unclamped.x, focus.x)) velocity.x = 0f;
            if (!Mathf.Approximately(unclamped.z, focus.z)) velocity.z = 0f;
            transform.position = focus - forward * Mathf.Max(distance, 10f);
        }
    }
}
