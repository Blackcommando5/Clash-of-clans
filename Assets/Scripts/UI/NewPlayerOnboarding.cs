using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Kingdoms.UI
{
    [DefaultExecutionOrder(100), DisallowMultipleComponent]
    public sealed class NewPlayerOnboarding : MonoBehaviour
    {
        public Font titleFont;
        public Font bodyFont;
        public Behaviour cameraController;

        [SerializeField] GameObject canvasObject, modal;
        [SerializeField] RectTransform safe, dialog;
        [SerializeField] InputField input;
        [SerializeField] Text heading, explanation, error, buttonLabel, chosenName;
        [SerializeField] Button primaryButton, editButton;
        bool confirming, ownsCameraLock, cameraWasEnabled;
        string pendingName;
        EventSystem ownedEventSystem;
        readonly Color ink = new Color(.22f, .18f, .15f);

        public bool IsAskingForName => modal != null && modal.activeSelf;

        void Start()
        {
            if (titleFont == null) titleFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (bodyFont == null) bodyFont = titleFont;
            if(canvasObject==null)CreateCanvas();
            else
            {
                primaryButton.onClick.AddListener(Continue);
                editButton.onClick.AddListener(EditName);
                input.onValueChanged.AddListener(ValidateInput);
                modal.SetActive(false);dialog.gameObject.SetActive(false);
            }
            if (PlayerProfile.HasPlayerName) ShowPlayerName();
            else ShowNameDialog();
        }

        public void BuildEditableDialog()
        {
            if(canvasObject!=null)return;
            if(titleFont==null)titleFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if(bodyFont==null)bodyFont=titleFont;
            var controller=cameraController;cameraController=null;
            CreateCanvas();ShowNameDialog();cameraController=controller;
            modal.SetActive(false);dialog.gameObject.SetActive(false);
        }

        public void PreviewEditorDialog(bool show)
        {
            if(modal!=null)modal.SetActive(show);
            if(dialog!=null)dialog.gameObject.SetActive(show);
        }

        void CreateCanvas()
        {
            canvasObject = new GameObject("Player Identity UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            safe = Rect("Safe Area", canvasObject.transform, Vector2.zero, Vector2.one);
        }

        void ShowNameDialog()
        {
            if (cameraController != null)
            {
                cameraWasEnabled = cameraController.enabled;
                ownsCameraLock = true;
                cameraController.enabled = false;
            }
            if (EventSystem.current == null)
            {
                var events = new GameObject("Onboarding EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(canvasObject.transform, false);
                ownedEventSystem = events.GetComponent<EventSystem>();
            }
            if(modal!=null)
            {
                modal.SetActive(true);dialog.gameObject.SetActive(true);
                ValidateInput(input.text);UpdateLayout();
                if(!Application.isMobilePlatform)StartCoroutine(FocusInput());
                return;
            }
            modal = Rect("Name Dialog", canvasObject.transform, Vector2.zero, Vector2.one).gameObject;
            var shade = Panel("Village Dimmer", modal.transform, new Color(0,0,0,.52f), new Color(0,0,0,.52f), Vector2.zero, Vector2.one, 0);
            shade.raycastTarget = true;
            // Keep the dimmer full-screen, and only the popup inside the safe area.
            dialog = Rect("Chief Name Popup", safe, new Vector2(.5f,.5f), new Vector2(.5f,.5f), new Vector2(940,590));
            var shadow = Panel("Panel Shadow", dialog, new Color(.07f,.05f,.035f,.8f), new Color(.07f,.05f,.035f,.8f), Vector2.zero, Vector2.one, 32);
            shadow.rectTransform.anchoredPosition = new Vector2(0,-12);
            var shell = Panel("Stone Frame", dialog, new Color(.90f,.86f,.76f), new Color(.55f,.51f,.44f), Vector2.zero, Vector2.one, 28);
            shell.raycastTarget = true;
            var paper = Panel("Parchment", shell.transform, new Color(.96f,.94f,.86f), new Color(.84f,.82f,.74f), Vector2.zero, Vector2.one, 22);
            paper.rectTransform.sizeDelta = new Vector2(-14,-14);
            Panel("Header", paper.transform, new Color(.88f,.86f,.80f), new Color(.72f,.70f,.65f), new Vector2(0,.79f), Vector2.one, 20);
            heading = Label("Heading", paper.transform, "My name is...", 65, Color.white, new Vector2(.04f,.8f), new Vector2(.96f,.985f), true);
            var outline = heading.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.19f,.16f,.13f);
            outline.effectDistance = new Vector2(2,-2);
            explanation = Label("Chief Greeting", paper.transform, "Welcome, Chief! What should we call you?", 30, ink, new Vector2(.06f,.65f), new Vector2(.94f,.77f));
            var inputFrame = Panel("Name Input", paper.transform, new Color(.39f,.36f,.30f), new Color(.59f,.55f,.47f), new Vector2(.14f,.43f), new Vector2(.86f,.62f), 14);
            inputFrame.raycastTarget = true;
            var inputPaper = Panel("Input Paper", inputFrame.transform, Color.white, new Color(.96f,.95f,.90f), Vector2.zero, Vector2.one, 10);
            inputPaper.rectTransform.sizeDelta = new Vector2(-8,-8);
            inputPaper.raycastTarget = false;
            var content = Label("Typed Name", inputFrame.transform, "", 39, ink, new Vector2(.035f,.06f), new Vector2(.965f,.94f));
            content.resizeTextForBestFit = false;
            content.alignment = TextAnchor.MiddleLeft;
            var placeholder = Label("Placeholder", inputFrame.transform, "Enter your name", 34, new Color(.55f,.53f,.48f), new Vector2(.035f,.06f), new Vector2(.965f,.94f));
            placeholder.alignment = TextAnchor.MiddleLeft;
            input = inputFrame.gameObject.AddComponent<InputField>();
            input.targetGraphic = inputFrame;
            input.textComponent = content;
            input.placeholder = placeholder;
            input.characterLimit = PlayerProfile.MaximumNameLength;
            input.lineType = InputField.LineType.SingleLine;
            input.contentType = InputField.ContentType.Standard;
            input.keyboardType = TouchScreenKeyboardType.Default;
            input.shouldHideMobileInput = false;
            input.customCaretColor = true;
            input.caretColor = ink;
            input.onValueChanged.AddListener(ValidateInput);
            chosenName = Label("Confirmed Name", paper.transform, "", 54, ink, new Vector2(.1f,.44f), new Vector2(.9f,.62f), true);
            chosenName.gameObject.SetActive(false);
            error = Label("Name Guidance", paper.transform, "Choose a name with 2 to 15 characters.", 24, new Color(.40f,.37f,.31f), new Vector2(.06f,.33f), new Vector2(.94f,.43f));
            primaryButton = MakeButton("Done", paper.transform, "DONE", new Vector2(.34f,.13f), new Vector2(.66f,.31f), true, out buttonLabel);
            primaryButton.onClick.AddListener(Continue);
            Text ignored;
            editButton = MakeButton("Edit Name", paper.transform, "EDIT", new Vector2(.12f,.13f), new Vector2(.31f,.31f), false, out ignored);
            editButton.onClick.AddListener(EditName);
            editButton.gameObject.SetActive(false);
            Label("Footer", paper.transform, "Choose a nickname for your Chief.", 23, new Color(.43f,.40f,.34f), new Vector2(.07f,.025f), new Vector2(.93f,.105f));
            safe.SetAsLastSibling();
            ValidateInput("");
            UpdateLayout();
            if (Application.isPlaying && !Application.isMobilePlatform) StartCoroutine(FocusInput());
        }

        IEnumerator FocusInput() { yield return null; if (input != null && !confirming) input.ActivateInputField(); }

        void ValidateInput(string value)
        {
            bool valid = PlayerProfile.TryValidateName(value, out _, out string message);
            primaryButton.interactable = valid;
            error.text = valid ? "Looking good, Chief!" : message;
            error.color = !valid && value.Length > 0 ? new Color(.67f,.19f,.13f) : new Color(.40f,.37f,.31f);
        }

        public void Continue()
        {
            if (!IsAskingForName) return;
            if (confirming)
            {
                if (!PlayerProfile.TrySaveName(pendingName, out string message)) { error.text = message; return; }
                modal.SetActive(false);
                dialog.gameObject.SetActive(false);
                RestoreCamera();
                if (ownedEventSystem != null) Destroy(ownedEventSystem.gameObject);
                ShowPlayerName();
                return;
            }
            if (!PlayerProfile.TryValidateName(input.text, out pendingName, out string problem))
            {
                error.text = problem;
                return;
            }
            confirming = true;
            input.DeactivateInputField();
            input.gameObject.SetActive(false);
            chosenName.gameObject.SetActive(true);
            chosenName.text = pendingName;
            heading.text = "Your name is...";
            explanation.text = "Ready to lead your village with this name?";
            error.text = "You can edit it before confirming.";
            error.color = new Color(.40f,.37f,.31f);
            buttonLabel.text = "CONFIRM";
            editButton.gameObject.SetActive(true);
        }

        public void EditName()
        {
            confirming = false;
            chosenName.gameObject.SetActive(false);
            input.gameObject.SetActive(true);
            heading.text = "My name is...";
            explanation.text = "Welcome, Chief! What should we call you?";
            buttonLabel.text = "DONE";
            editButton.gameObject.SetActive(false);
            ValidateInput(input.text);
            StartCoroutine(FocusInput());
        }

        void ShowPlayerName()
        {
            if (FindFirstObjectByType<VillageGameplay>() != null) return;
            var badge = Panel("Chief Nameplate", safe, new Color(.19f,.24f,.13f,.9f), new Color(.10f,.14f,.08f,.9f), new Vector2(0,1), new Vector2(0,1), 13);
            badge.rectTransform.pivot = new Vector2(0,1);
            badge.rectTransform.sizeDelta = new Vector2(310,95);
            badge.rectTransform.anchoredPosition = new Vector2(24,-24);
            Label("Chief Label", badge.transform, "CHIEF", 20, new Color(1,.79f,.35f), new Vector2(.07f,.58f), new Vector2(.93f,.91f));
            Label("Player Name", badge.transform, PlayerProfile.PlayerName, 35, Color.white, new Vector2(.06f,.06f), new Vector2(.94f,.60f));
        }

        void Update() { UpdateLayout(); }

        void UpdateLayout()
        {
            if (safe == null) return;
            float w = Mathf.Max(1,Screen.width), h = Mathf.Max(1,Screen.height);
            Rect area = Screen.safeArea;
            if (IsAskingForName && TouchScreenKeyboard.visible && TouchScreenKeyboard.area.height > 0)
                area.yMin = Mathf.Max(area.yMin, TouchScreenKeyboard.area.yMax);
            safe.anchorMin = new Vector2(area.xMin/w,area.yMin/h);
            safe.anchorMax = new Vector2(area.xMax/w,area.yMax/h);
            if (dialog != null)
            {
                float scale = Mathf.Min(1f, Mathf.Min(safe.rect.width/990f, safe.rect.height/640f));
                dialog.localScale = Vector3.one * Mathf.Max(.1f,scale);
            }
        }

        void RestoreCamera()
        {
            if (!ownsCameraLock) return;
            ownsCameraLock = false;
            if (cameraController != null) cameraController.enabled = cameraWasEnabled;
        }

        void OnDisable()
        {
            RestoreCamera();
            if (canvasObject != null) canvasObject.SetActive(false);
        }
        void OnEnable()
        {
            if (canvasObject == null) return;
            canvasObject.SetActive(true);
            if (IsAskingForName && cameraController != null)
            {
                cameraWasEnabled = cameraController.enabled;
                ownsCameraLock = true;
                cameraController.enabled = false;
            }
        }

        RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 size = default)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent,false);
            rect.anchorMin = min; rect.anchorMax = max;
            rect.sizeDelta = size;
            return rect;
        }

        WelcomePanel Panel(string name, Transform parent, Color top, Color bottom, Vector2 min, Vector2 max, float radius)
        {
            var rect = Rect(name,parent,min,max);
            rect.gameObject.AddComponent<CanvasRenderer>();
            var graphic = rect.gameObject.AddComponent<WelcomePanel>();
            graphic.topColor = top; graphic.bottomColor = bottom; graphic.radius = radius;
            graphic.raycastTarget = false;
            graphic.SetVerticesDirty();
            return graphic;
        }

        Text Label(string name, Transform parent, string value, int size, Color color, Vector2 min, Vector2 max, bool title = false)
        {
            var label = Rect(name,parent,min,max).gameObject.AddComponent<Text>();
            label.font = title ? titleFont : bodyFont;
            label.text = value; label.color = color; label.fontSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true; label.resizeTextMinSize = 12; label.resizeTextMaxSize = size;
            label.supportRichText = false; label.raycastTarget = false;
            return label;
        }

        Button MakeButton(string name, Transform parent, string label, Vector2 min, Vector2 max, bool green, out Text text)
        {
            var frame = Panel(name,parent,green ? new Color(.46f,.76f,.16f) : new Color(.65f,.63f,.56f),green ? new Color(.20f,.43f,.06f) : new Color(.43f,.41f,.36f),min,max,13);
            frame.raycastTarget = true;
            var gloss = Panel("Highlight",frame.transform,new Color(1,1,1,.23f),new Color(1,1,1,0),new Vector2(.015f,.55f),new Vector2(.985f,.98f),8);
            text = Label("Button Label",frame.transform,label,39,Color.white,new Vector2(.03f,.04f),new Vector2(.97f,.96f),true);
            var outline = text.gameObject.AddComponent<Outline>();outline.effectColor = new Color(.12f,.20f,.06f);outline.effectDistance = new Vector2(1.5f,-1.5f);
            var button = frame.gameObject.AddComponent<Button>();button.targetGraphic = frame;
            var colors = button.colors; colors.highlightedColor = new Color(1.08f,1.08f,1.08f);colors.pressedColor = new Color(.8f,.8f,.8f);colors.disabledColor = new Color(.65f,.65f,.65f,.75f);button.colors = colors;
            return button;
        }
    }
}
