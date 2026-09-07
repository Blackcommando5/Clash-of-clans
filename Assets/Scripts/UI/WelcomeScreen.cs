using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Kingdoms.UI
{
    [DisallowMultipleComponent]
    public sealed class WelcomeScreen : MonoBehaviour
    {
        public string villageScene = "Main Scene";
        [Min(0f)] public float minimumDisplayTime = 3f;
        public RectTransform safeArea;
        public RawImage backdrop;
        public RectTransform progressFill;
        public Text statusLabel;
        public Text percentageLabel;
        public Text tipLabel;
        public CanvasGroup canvasGroup;
        public Text versionLabel;
        public string[] tips = { "Drag to explore your village.", "Pinch or scroll to get a closer look.", "Every great kingdom begins with a village." };

        Rect lastSafeArea;
        int lastWidth, lastHeight;
        float shownProgress;
        AsyncOperation loading;

        void Awake()
        {
            ApplyLayout();
            SetProgress(0f);
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (versionLabel != null) versionLabel.text = "v" + Application.version;
        }

        IEnumerator Start()
        {
            // Display a useful error instead of an endless fake loading bar.
            if (!Application.CanStreamedLevelBeLoaded(villageScene))
            {
                statusLabel.text = "Village unavailable";
                tipLabel.text = "Please restart the game.";
                Debug.LogError("Welcome: add '" + villageScene + "' to the enabled build scenes.", this);
                yield break;
            }
            yield return null; // Let the first frame of the welcome screen render.
            float started = Time.realtimeSinceStartup;
            loading = SceneManager.LoadSceneAsync(villageScene, LoadSceneMode.Single);
            if (loading == null)
            {
                statusLabel.text = "Unable to open your village";
                yield break;
            }
            loading.allowSceneActivation = false;
            while (shownProgress < 0.999f || Time.realtimeSinceStartup - started < minimumDisplayTime)
            {
                float elapsed = Time.realtimeSinceStartup - started;
                float target = Mathf.Clamp01(loading.progress / 0.9f);
                shownProgress = Mathf.MoveTowards(shownProgress, target, Time.unscaledDeltaTime * 0.65f);
                SetProgress(shownProgress);
                statusLabel.text = target < 1f ? "Loading your village..." : "Welcome, Chief!";
                if (tips != null && tips.Length > 0)
                    tipLabel.text = tips[Mathf.FloorToInt(elapsed / 3.5f) % tips.Length];
                yield return null;
            }
            SetProgress(1f);
            statusLabel.text = "Your kingdom awaits!";
            yield return new WaitForSecondsRealtime(0.35f);
            // Reveal a clean dark transition before activating the already-loaded village.
            float fade = 0f;
            while (fade < 0.35f)
            {
                fade += Time.unscaledDeltaTime;
                if (canvasGroup != null) canvasGroup.alpha = 1f - Mathf.Clamp01(fade / 0.35f);
                yield return null;
            }
            loading.allowSceneActivation = true;
        }

        void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight || Screen.safeArea != lastSafeArea)
                ApplyLayout();
        }

        void OnDestroy()
        {
            // Never leave Unity's asynchronous scene queue stalled if this object is removed.
            if (loading != null && !loading.isDone) loading.allowSceneActivation = true;
        }

        void SetProgress(float value)
        {
            value = Mathf.Clamp01(value);
            if (progressFill != null) progressFill.anchorMax = new Vector2(value, 1f);
            if (percentageLabel != null) percentageLabel.text = Mathf.RoundToInt(value * 100f) + "%";
        }

        void ApplyLayout()
        {
            lastWidth = Mathf.Max(1, Screen.width);
            lastHeight = Mathf.Max(1, Screen.height);
            lastSafeArea = Screen.safeArea;
            if (safeArea != null)
            {
                safeArea.anchorMin = new Vector2(lastSafeArea.xMin / lastWidth, lastSafeArea.yMin / lastHeight);
                safeArea.anchorMax = new Vector2(lastSafeArea.xMax / lastWidth, lastSafeArea.yMax / lastHeight);
                safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            }
            if (backdrop != null && backdrop.texture != null)
            {
                float imageAspect = (float)backdrop.texture.width / backdrop.texture.height;
                float screenAspect = (float)lastWidth / lastHeight;
                float width = Mathf.Min(1f, screenAspect / imageAspect);
                float height = Mathf.Min(1f, imageAspect / screenAspect);
                backdrop.uvRect = new Rect((1f - width) * 0.5f, (1f - height) * 0.5f, width, height);
            }
        }
    }
}
