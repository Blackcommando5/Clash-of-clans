using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Kingdoms.UI
{
    [DefaultExecutionOrder(50), DisallowMultipleComponent]
    public sealed class VillageGameplay : MonoBehaviour
    {
        public GameObject townHallPrefab, goldMinePrefab, pinePrefab;
        public Material footprintMaterial;
        public Font titleFont, bodyFont;
        public Camera viewCamera;
        public VillageCameraController cameraController;
        public VillageState State { get; private set; }
        public bool IsPlacing => preview != null;

        Transform world;
        GameObject hud, shop, placementBar, preview, footprint;
        RectTransform safe, shopPanel;
        Text goldLabel, elixirLabel, gemsLabel, guide, collectLabel, shopInfo, placementInfo;
        Button buyButton, confirmButton, collectButton;
        Material previewMaterial;
        int cellX, cellZ;
        bool dragging, visible;
        float nextTick, nextSave, messageUntil;
        readonly List<RaycastResult> hits = new List<RaycastResult>();
        EventSystem raycastSystem;
        PointerEventData pointer;

        void OnEnable() { EnhancedTouchSupport.Enable(); }

        void Start()
        {
            if (titleFont == null) titleFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (bodyFont == null) bodyFont = titleFont;
            if (EventSystem.current == null)
            {
                var events = new GameObject("Village EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
            }
            BuildUI();
            if (!VillageSave.TryLoad(out var loaded, out string error))
            {
                guide.text = error;
                hud.SetActive(true);
                return;
            }
            State = loaded;
            world = new GameObject("Village Buildings").transform;
            world.SetParent(transform, false);
            foreach (var building in State.buildings) Spawn(building);
            CreateForest();
            nextSave = Time.unscaledTime + 30f;
            RefreshHUD();
            hud.SetActive(PlayerProfile.HasPlayerName);
            visible = hud.activeSelf;
        }

        void Update()
        {
            if (safe != null)
            {
                Rect area = Screen.safeArea;
                safe.anchorMin = new Vector2(area.xMin / Mathf.Max(1,Screen.width), area.yMin / Mathf.Max(1,Screen.height));
                safe.anchorMax = new Vector2(area.xMax / Mathf.Max(1,Screen.width), area.yMax / Mathf.Max(1,Screen.height));
                if (shopPanel != null) shopPanel.localScale = Vector3.one * Mathf.Min(1f, Mathf.Min(safe.rect.width / 860f, safe.rect.height / 650f));
            }
            if (State == null) return;
            bool shouldShow = PlayerProfile.HasPlayerName;
            if (visible != shouldShow) { visible = shouldShow; hud.SetActive(visible); RefreshHUD(); }
            if (Time.unscaledTime >= nextTick)
            {
                nextTick = Time.unscaledTime + 1f;
                State.Accrue(VillageState.Now);
                RefreshHUD();
            }
            if (Time.unscaledTime >= nextSave)
            {
                nextSave = Time.unscaledTime + 30f;
                SaveProgress();
            }
            if (visible && IsPlacing) HandlePlacementInput();
        }

        public void OpenShop()
        {
            if (State == null || !PlayerProfile.HasPlayerName || IsPlacing) return;
            shop.SetActive(true);
            cameraController.InputBlocked = true;
            RefreshHUD();
        }

        public void CloseShop()
        {
            shop.SetActive(false);
            if (!IsPlacing) cameraController.InputBlocked = false;
        }

        public void BeginMinePlacement()
        {
            if (State == null || !PlayerProfile.HasPlayerName || IsPlacing) return;
            if (State.elixir < VillageState.MineCost || State.MineCount >= VillageState.MineLimit) return;
            shop.SetActive(false);
            preview = Instantiate(goldMinePrefab, world);
            preview.name = "Gold Mine Preview";
            foreach (var collider in preview.GetComponentsInChildren<Collider>()) collider.enabled = false;
            footprint = GameObject.CreatePrimitive(PrimitiveType.Plane);
            footprint.name = "Placement Footprint";
            footprint.transform.SetParent(world, false);
            footprint.transform.localScale = new Vector3(.3f,1f,.3f);
            Destroy(footprint.GetComponent<Collider>());
            previewMaterial = new Material(footprintMaterial);
            footprint.GetComponent<Renderer>().sharedMaterial = previewMaterial;
            footprint.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            placementBar.SetActive(true);
            cameraController.InputBlocked = true;
            dragging = false;
            int x = 5, z = -1;
            if (!State.CanPlaceMine(x,z,out _))
                for (int zz=-10;zz<=10;zz+=4)
                {
                    bool found = false;
                    for (int xx=4;xx<=16;xx+=4)
                        if (State.CanPlaceMine(xx,zz,out _)) { x=xx;z=zz;found=true;break; }
                    if (found) break;
                }
            SetPreviewCell(x,z);
        }

        public void SetPreviewCell(int x, int z)
        {
            if (!IsPlacing) return;
            cellX=x;cellZ=z;
            preview.transform.position = new Vector3(x+1.5f,0,z+1.5f);
            footprint.transform.position = new Vector3(x+1.5f,.035f,z+1.5f);
            bool valid = State.CanPlaceMine(x,z,out string reason);
            Color color = valid ? new Color(.30f,.92f,.18f) : new Color(.94f,.13f,.10f);
            previewMaterial.SetColor("_BaseColor",color);
            previewMaterial.SetColor("_Color",color);
            confirmButton.interactable=valid;
            placementInfo.text=reason;
        }

        public void ConfirmPlacement()
        {
            if (!IsPlacing || !PlayerProfile.HasPlayerName) return;
            var candidate=State.Copy();
            if (!candidate.TryPlaceMine(cellX,cellZ,VillageState.Now,out string message)) { placementInfo.text=message;return; }
            if (!VillageSave.TryWrite(candidate,out string error)) { placementInfo.text=error;return; }
            State=candidate;
            Spawn(State.buildings[State.buildings.Count-1]);
            CancelPlacement();
            ShowMessage(message);
            RefreshHUD();
        }

        public void CancelPlacement()
        {
            if (preview!=null) Destroy(preview);
            if (footprint!=null) Destroy(footprint);
            if (previewMaterial!=null) Destroy(previewMaterial);
            preview=null;footprint=null;previewMaterial=null;
            dragging=false;
            if (placementBar!=null) placementBar.SetActive(false);
            if(cameraController!=null) cameraController.InputBlocked=false;
        }

        public void Collect()
        {
            if(State==null || !PlayerProfile.HasPlayerName || IsPlacing) return;
            var candidate=State.Copy();
            int amount=candidate.CollectGold(VillageState.Now);
            if(amount==0) { ShowMessage(State.gold>=VillageState.ResourceCapacity ? "Your gold storage is full." : "Your mines are still producing gold.");return; }
            if(!VillageSave.TryWrite(candidate,out string error)) { ShowMessage(error);return; }
            State=candidate;
            ShowMessage("Collected "+amount+" gold!");
            RefreshHUD();
        }

        void HandlePlacementInput()
        {
            var touches=Touch.activeTouches;
            if(touches.Count>0)
            {
                if(touches.Count!=1) { dragging=false;return; }
                var touch=touches[0];
                if(touch.began) dragging=!OverUI(touch.screenPosition);
                if(touch.inProgress && dragging && !OverUI(touch.screenPosition)) MovePreview(touch.screenPosition);
                if(touch.ended) dragging=false;
                return;
            }
            var mouse=Mouse.current;
            if(mouse==null) return;
            var position=mouse.position.ReadValue();
            if(mouse.leftButton.wasPressedThisFrame) dragging=!OverUI(position);
            if(mouse.leftButton.isPressed && dragging && !OverUI(position)) MovePreview(position);
            if(mouse.leftButton.wasReleasedThisFrame) dragging=false;
        }

        void MovePreview(Vector2 position)
        {
            var plane=new Plane(Vector3.up,Vector3.zero);
            var ray=viewCamera.ScreenPointToRay(position);
            if(plane.Raycast(ray,out float distance))
            {
                var p=ray.GetPoint(distance);
                SetPreviewCell(Mathf.FloorToInt(p.x-1f),Mathf.FloorToInt(p.z-1f));
            }
        }

        bool OverUI(Vector2 p)
        {
            var system=EventSystem.current;
            if(system==null) return false;
            if(raycastSystem!=system) { raycastSystem=system;pointer=new PointerEventData(system); }
            pointer.Reset();pointer.position=p;hits.Clear();system.RaycastAll(pointer,hits);
            foreach(var hit in hits) if(hit.module is GraphicRaycaster) return true;
            return false;
        }

        void Spawn(PlacedBuilding data)
        {
            var prefab=data.kind=="TownHall" ? townHallPrefab : goldMinePrefab;
            var instance=Instantiate(prefab,new Vector3(data.x+data.Size*.5f,0,data.z+data.Size*.5f),Quaternion.identity,world);
            instance.name=data.kind+" ("+data.x+", "+data.z+")";
        }

        void CreateForest()
        {
            if(pinePrefab==null) return;
            var random=new System.Random(42);
            var forest=new GameObject("Village Woodland").transform;forest.SetParent(transform,false);
            for(int i=0;i<52;i++)
            {
                float along=(float)random.NextDouble()*68f-34f;
                float edge=25f+(float)random.NextDouble()*12f;
                Vector3 p=i%4==0 ? new Vector3(along,0,edge) : i%4==1 ? new Vector3(-edge,0,along) : i%4==2 ? new Vector3(along,0,-edge) : new Vector3(edge,0,along);
                var tree=Instantiate(pinePrefab,p,Quaternion.Euler(0,(float)random.NextDouble()*360f,0),forest);
                tree.transform.localScale=Vector3.one*(.8f+(float)random.NextDouble()*.7f);
            }
        }

        void RefreshHUD()
        {
            if(State==null) return;
            goldLabel.text=State.gold.ToString("N0")+" / 10,000";
            elixirLabel.text=State.elixir.ToString("N0")+" / 10,000";
            gemsLabel.text=State.gems.ToString("N0");
            collectLabel.text="COLLECT  "+State.CollectableGold;
            collectButton.interactable=State.CollectableGold>0 && State.gold<VillageState.ResourceCapacity && !IsPlacing;
            buyButton.interactable=State.elixir>=VillageState.MineCost && State.MineCount<VillageState.MineLimit;
            shopInfo.text="60 gold / minute\nStores up to 500 gold\n3 x 3 village cells\n\nBuilt: "+State.MineCount+" / 3";
            if(Time.unscaledTime>=messageUntil)
                guide.text=IsPlacing ? "Tap or drag on the ground to position your mine." : State.MineCount==0 ? "Chief "+PlayerProfile.PlayerName+", open SHOP to build your first Gold Mine." : !State.collectedFirstGold ? "Your mine is working. Tap COLLECT when gold is ready." : "Your village is growing. Drag to explore; pinch or scroll to zoom.";
        }

        void ShowMessage(string message) { guide.text=message;messageUntil=Time.unscaledTime+5f; }
        void SaveProgress() { if(State!=null) { State.Accrue(VillageState.Now);VillageSave.TryWrite(State,out _); } }
        void OnApplicationPause(bool paused) { if(paused) SaveProgress(); }
        void OnApplicationQuit() { SaveProgress(); }
        void OnDisable() { EnhancedTouchSupport.Disable();CancelPlacement(); }

        void BuildUI()
        {
            hud=new GameObject("Village HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            hud.transform.SetParent(transform,false);
            hud.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            hud.GetComponent<Canvas>().sortingOrder=100;
            var scaler=hud.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            safe=Box("Safe Area",hud.transform,Vector2.zero,Vector2.one);
            var resources=Box("Resources",safe,new Vector2(1,1),new Vector2(1,1),new Vector2(350,218),new Vector2(-200,-133));
            goldLabel=Resource("GOLD",resources,new Color(.95f,.70f,.16f),.69f,1f);
            elixirLabel=Resource("ELIXIR",resources,new Color(.78f,.33f,.86f),.345f,.655f);
            gemsLabel=Resource("GEMS",resources,new Color(.36f,.86f,.29f),0,.31f);
            var town=Panel("Town Hall Level",safe,new Vector2(.5f,1),new Vector2(.5f,1),new Color(.16f,.20f,.11f,.92f),new Vector2(235,52),new Vector2(0,-48));
            Label("Level",town.transform,"TOWN HALL 1",28,Color.white);
            var guidePanel=Panel("Guide",safe,new Vector2(.21f,0),new Vector2(.79f,0),new Color(.11f,.15f,.075f,.92f),new Vector2(0,64),new Vector2(0,147));
            guide=Label("Tutorial Guide",guidePanel.transform,"Preparing your village...",26,Color.white);
            collectButton=Button("Collect Gold",safe,"COLLECT  0",new Vector2(0,0),new Vector2(0,0),new Color(.78f,.50f,.10f),new Vector2(280,95),new Vector2(165,70));collectLabel=collectButton.GetComponentInChildren<Text>();collectButton.onClick.AddListener(Collect);
            var shopButton=Button("Shop",safe,"SHOP",new Vector2(1,0),new Vector2(1,0),new Color(.42f,.67f,.16f),new Vector2(235,105),new Vector2(-145,74));shopButton.onClick.AddListener(OpenShop);
            placementBar=Box("Placement Controls",safe,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(790,108),new Vector2(0,69)).gameObject;
            Panel("Placement Background",placementBar.transform,Vector2.zero,Vector2.one,new Color(.14f,.12f,.09f,.97f));
            placementInfo=Label("Placement Status",placementBar.transform,"",22,Color.white,new Vector2(.03f,.53f),new Vector2(.97f,.97f));
            var cancel=Button("Cancel Placement",placementBar.transform,"CANCEL",new Vector2(.19f,.04f),new Vector2(.47f,.49f),new Color(.62f,.27f,.19f));cancel.onClick.AddListener(CancelPlacement);
            confirmButton=Button("Confirm Placement",placementBar.transform,"BUILD",new Vector2(.53f,.04f),new Vector2(.81f,.49f),new Color(.36f,.65f,.13f));confirmButton.onClick.AddListener(ConfirmPlacement);
            placementBar.SetActive(false);
            shop=Box("Building Shop",safe,Vector2.zero,Vector2.one).gameObject;
            Panel("Shop Dimmer",shop.transform,Vector2.zero,Vector2.one,new Color(0,0,0,.57f));
            shopPanel=Box("Shop Window",shop.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(800,590));
            Panel("Shop Frame",shopPanel,Vector2.zero,Vector2.one,new Color(.85f,.81f,.71f));
            Label("Shop Title",shopPanel,"BUILD YOUR VILLAGE",52,new Color(.24f,.17f,.10f),new Vector2(.08f,.82f),new Vector2(.92f,.97f),true);
            var card=Panel("Gold Mine Card",shopPanel,new Vector2(.06f,.23f),new Vector2(.94f,.81f),new Color(.95f,.93f,.86f));
            var coin=Panel("Gold Coin",card.transform,new Vector2(.06f,.32f),new Vector2(.33f,.85f),new Color(.97f,.70f,.15f));coin.radius=100;
            Label("Coin Mark",coin.transform,"G",94,new Color(1,.94f,.54f),title:true);
            Label("Mine Name",card.transform,"GOLD MINE",43,new Color(.25f,.17f,.10f),new Vector2(.38f,.68f),new Vector2(.96f,.95f),true);
            shopInfo=Label("Mine Details",card.transform,"",25,new Color(.34f,.29f,.22f),new Vector2(.40f,.08f),new Vector2(.96f,.69f));
            buyButton=Button("Buy Gold Mine",shopPanel,"BUILD - 150 ELIXIR",new Vector2(.27f,.07f),new Vector2(.90f,.20f),new Color(.38f,.67f,.14f));buyButton.onClick.AddListener(BeginMinePlacement);
            var close=Button("Close Shop",shopPanel,"BACK",new Vector2(.06f,.07f),new Vector2(.24f,.20f),new Color(.57f,.48f,.35f));close.onClick.AddListener(CloseShop);
            shop.SetActive(false);
        }

        Text Resource(string title,Transform parent,Color accent,float bottom,float top)
        {
            var panel=Panel(title,parent,new Vector2(0,bottom),new Vector2(1,top),new Color(.14f,.12f,.09f,.94f));
            Label(title+" Label",panel.transform,title,18,accent,new Vector2(.08f,.59f),new Vector2(.92f,.94f));
            return Label(title+" Value",panel.transform,"0",30,Color.white,new Vector2(.06f,.04f),new Vector2(.94f,.62f));
        }
        RectTransform Box(string name,Transform parent,Vector2 min,Vector2 max,Vector2 size=default,Vector2 position=default)
        {
            var go=new GameObject(name,typeof(RectTransform));go.layer=5;
            var rect=(RectTransform)go.transform;rect.SetParent(parent,false);rect.anchorMin=min;rect.anchorMax=max;rect.sizeDelta=size;rect.anchoredPosition=position;return rect;
        }
        WelcomePanel Panel(string name,Transform parent,Vector2 min,Vector2 max,Color color,Vector2 size=default,Vector2 position=default)
        {
            var rect=Box(name,parent,min,max,size,position);rect.gameObject.AddComponent<CanvasRenderer>();
            var panel=rect.gameObject.AddComponent<WelcomePanel>();panel.topColor=color;panel.bottomColor=new Color(color.r*.78f,color.g*.78f,color.b*.78f,color.a);panel.radius=14;panel.raycastTarget=true;panel.SetVerticesDirty();return panel;
        }
        Text Label(string name,Transform parent,string value,int size,Color color,Vector2? min=null,Vector2? max=null,bool title=false)
        {
            var rect=Box(name,parent,min??new Vector2(.035f,.04f),max??new Vector2(.965f,.96f));
            var text=rect.gameObject.AddComponent<Text>();text.font=title?titleFont:bodyFont;text.fontSize=size;text.resizeTextForBestFit=true;text.resizeTextMinSize=12;text.resizeTextMaxSize=size;text.alignment=TextAnchor.MiddleCenter;text.color=color;text.text=value;text.supportRichText=false;text.raycastTarget=false;return text;
        }
        Button Button(string name,Transform parent,string title,Vector2 min,Vector2 max,Color color,Vector2 size=default,Vector2 position=default)
        {
            var panel=Panel(name,parent,min,max,color,size,position);var button=panel.gameObject.AddComponent<Button>();button.targetGraphic=panel;
            var label=Label("Label",panel.transform,title,36,Color.white,title:true);var outline=label.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.12f,.10f,.06f);outline.effectDistance=new Vector2(1.5f,-1.5f);return button;
        }
    }
}
