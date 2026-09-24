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
    public sealed partial class VillageGameplay : MonoBehaviour
    {
        public GameObject townHallPrefab, goldMinePrefab, pinePrefab;
        public GameObject elixirCollectorPrefab, goldStoragePrefab, elixirStoragePrefab;
        public GameObject cannonPrefab, archerTowerPrefab;
        public Material footprintMaterial;
        public Font titleFont, bodyFont;
        public Camera viewCamera;
        public VillageCameraController cameraController;
        public VillageState State { get; private set; }
        public bool IsPlacing => preview != null;

        Transform world;
        [SerializeField] GameObject hud, shop, placementBar, preview, footprint;
        [SerializeField] RectTransform safe, shopPanel;
        [SerializeField] Text goldLabel, elixirLabel, gemsLabel, guide, collectLabel, placementInfo;
        [SerializeField] Button confirmButton, collectButton;
        [SerializeField] List<Button> shopButtons = new List<Button>();
        [SerializeField] List<Text> shopDetails = new List<Text>();
        readonly List<Button> producerButtons = new List<Button>();
        readonly List<int> producerIndices = new List<int>();
        string placingKind;
        Material previewMaterial;
        int cellX, cellZ;
        bool dragging, visible, shuttingDown;
        float nextTick, nextSave, messageUntil;
        readonly List<RaycastResult> hits = new List<RaycastResult>();
        EventSystem raycastSystem;
        PointerEventData pointer;

        void OnEnable() { shuttingDown=false;EnhancedTouchSupport.Enable(); }

        void Start()
        {
            if (titleFont == null) titleFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (bodyFont == null) bodyFont = titleFont;
            if (EventSystem.current == null)
            {
                var events = new GameObject("Village EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
            }
            if (hud == null) BuildEditableInterface();
            else BindEditableInterface();
            HideEditorScreens();
            BindWalls();
            BuildScreenshotReferenceInterface();
            BindScreenshotReferenceInterface();
            bool freshVillage=!PlayerPrefs.HasKey(VillageSave.Key);
            if (!VillageSave.TryLoad(out var loaded, out string error))
            {
                guide.text = error;
                hud.SetActive(true);
                return;
            }
            if(freshVillage && editorVillage!=null)
            {
                var p=townHallPrefab.transform.position;
                loaded.buildings[0].x=Mathf.RoundToInt(p.x-2);
                loaded.buildings[0].z=Mathf.RoundToInt(p.z-2);
                AddStarterWalls(loaded);
                if(!loaded.IsValid()){Debug.LogWarning("Starter Town Hall is outside the build area; using the default location.");loaded=VillageState.Create(VillageState.Now);}
            }
            State = loaded;
            BuildArmyPreparation();BuildCampaignInterface();BuildBattleHistoryInterface();BuildTutorialInterface();
            world = new GameObject("Village Buildings").transform;
            world.SetParent(transform, false);
            foreach (var building in State.buildings) Spawn(building);
            if(transform.Find("Village Woodland")==null) CreateForest();
            BuildReferenceScenery();
            if(editorVillage!=null) editorVillage.SetActive(false);
            nextSave = Time.unscaledTime + 30f;
            RefreshHUD();
            hud.SetActive(PlayerProfile.HasPlayerName);
            visible = hud.activeSelf;
        }

        void Update()
        {
            if(PracticeOpen){TickPracticeBattle();return;}
            if (safe != null)
            {
                Rect area = Screen.safeArea;
                safe.anchorMin = new Vector2(area.xMin / Mathf.Max(1,Screen.width), area.yMin / Mathf.Max(1,Screen.height));
                safe.anchorMax = new Vector2(area.xMax / Mathf.Max(1,Screen.width), area.yMax / Mathf.Max(1,Screen.height));
                RefreshLayouts();
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
            else if (visible && !shop.activeSelf && !DetailsOpen && !ProfileOpen) HandleBuildingSelection();
        }

        public void OpenShop()
        {
            if (State == null || !PlayerProfile.HasPlayerName || IsPlacing) return;
            CloseBuildingDetails();
            if(ProfileOpen)CloseProfile();
            shop.SetActive(true);
            DeselectBuilding();
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
            BeginPlacement("GoldMine");
        }

        public void BeginPlacement(string kind)
        {
            if (State == null || !PlayerProfile.HasPlayerName || IsPlacing) return;
            if (!State.CanBuy(kind, out string reason)) { ShowMessage(reason); return; }
            var prefab = PrefabFor(kind);
            if (prefab == null) { ShowMessage("This building is unavailable. Please restart the game."); return; }
            placingKind = kind;
            movingIndex=-1;DeselectBuilding();
            shop.SetActive(false);
            preview = Instantiate(prefab, world);
            preview.name = BuildingCatalog.Find(kind).Name + " Preview";
            foreach (var collider in preview.GetComponentsInChildren<Collider>()) collider.enabled = false;
            footprint = GameObject.CreatePrimitive(PrimitiveType.Plane);
            footprint.name = "Placement Footprint";
            footprint.transform.SetParent(world, false);
            float footprintScale = BuildingCatalog.Find(kind).Size / 10f;
            footprint.transform.localScale = new Vector3(footprintScale,1f,footprintScale);
            Destroy(footprint.GetComponent<Collider>());
            previewMaterial = new Material(footprintMaterial);
            footprint.GetComponent<Renderer>().sharedMaterial = previewMaterial;
            footprint.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            placementBar.SetActive(true);
            cameraController.InputBlocked = true;
            dragging = false;
            int x = 5, z = -1;
            if (!State.CanPlace(kind,x,z,out _))
            {
                float closest = float.MaxValue;
                for (int zz=-22;zz<=19;zz++)
                {
                    for (int xx=-22;xx<=19;xx++)
                    {
                        if (!State.CanPlace(kind,xx,zz,out _)) continue;
                        float distance = (new Vector2(xx+1.5f,zz+1.5f)-new Vector2(cameraController.focus.x,cameraController.focus.z)).sqrMagnitude;
                        if(distance<closest) { closest=distance;x=xx;z=zz; }
                    }
                }
            }
            SetPreviewCell(x,z);
            RefreshHUD();
        }

        public void SetPreviewCell(int x, int z)
        {
            if (!IsPlacing) return;
            cellX=x;cellZ=z;
            float half = BuildingCatalog.Find(placingKind).Size * .5f;
            preview.transform.position = new Vector3(x+half,0,z+half);
            footprint.transform.position = new Vector3(x+half,.035f,z+half);
            string reason;
            bool valid = movingIndex>=0 ? State.CanMove(movingIndex,x,z,out reason) : State.CanPlace(placingKind,x,z,out reason);
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
            string message;
            bool success=movingIndex>=0 ? candidate.TryMove(movingIndex,cellX,cellZ,out message) : candidate.TryPlace(placingKind,cellX,cellZ,VillageState.Now,out message);
            if (!success) { placementInfo.text=message;return; }
            if (!VillageSave.TryWrite(candidate,out string error)) { placementInfo.text=error;return; }
            State=candidate;
            if(movingIndex>=0) buildingInstances[movingIndex].transform.position=new Vector3(cellX+State.buildings[movingIndex].Size*.5f,0,cellZ+State.buildings[movingIndex].Size*.5f);
            else Spawn(State.buildings[State.buildings.Count-1]);
            CancelPlacement();
            ShowMessage(message);
            RefreshHUD();
        }

        public void CancelPlacement()
        {
            if (preview!=null) { preview.SetActive(false);Destroy(preview); }
            if (footprint!=null) { footprint.SetActive(false);Destroy(footprint); }
            if (previewMaterial!=null) Destroy(previewMaterial);
            preview=null;footprint=null;previewMaterial=null;
            placingKind=null;
            if(movingIndex>=0 && movingIndex<buildingInstances.Count && buildingInstances[movingIndex]!=null) buildingInstances[movingIndex].SetActive(true);
            movingIndex=-1;
            dragging=false;
            if (placementBar!=null) placementBar.SetActive(false);
            if(cameraController!=null) cameraController.InputBlocked=false;
            if (State != null && !shuttingDown) RefreshHUD();
        }

        public void Collect()
        {
            if(State==null || !PlayerProfile.HasPlayerName || IsPlacing) return;
            var candidate=State.Copy();
            int gold=candidate.Collect(ResourceKind.Gold,VillageState.Now);
            int elixir=candidate.Collect(ResourceKind.Elixir,VillageState.Now);
            if(gold+elixir==0) { ShowMessage("Nothing to collect. Check production and storage capacity.");return; }
            if(!VillageSave.TryWrite(candidate,out string error)) { ShowMessage(error);return; }
            State=candidate;
            ShowMessage("Collected "+gold+" gold and "+elixir+" elixir!");
            RefreshHUD();
        }

        public void CollectBuilding(int index)
        {
            if (State == null || !PlayerProfile.HasPlayerName || IsPlacing || shop.activeSelf || index < 0 || index >= State.buildings.Count) return;
            var candidate = State.Copy();
            var building = candidate.buildings[index];
            var definition = BuildingCatalog.Find(building.kind);
            int amount = candidate.Collect(definition.Resource, VillageState.Now, building);
            if (amount == 0) { ShowMessage("Nothing to collect. Check production and storage capacity."); return; }
            if (!VillageSave.TryWrite(candidate, out string error)) { ShowMessage(error); return; }
            State = candidate;
            ShowMessage("Collected " + amount + " " + definition.Resource.ToString().ToLowerInvariant() + "!");
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
                float half=BuildingCatalog.Find(placingKind).Size*.5f;
                SetPreviewCell(Mathf.RoundToInt(p.x-half),Mathf.RoundToInt(p.z-half));
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
            var prefab=PrefabFor(data.kind);
            var instance=Instantiate(prefab,new Vector3(data.x+data.Size*.5f,0,data.z+data.Size*.5f),Quaternion.identity,world);
            instance.name=data.kind+" ("+data.x+", "+data.z+")";
            buildingInstances.Add(instance);
            var definition = BuildingCatalog.Find(data.kind);
            if (definition.ProductionPerSecond > 0)
            {
                int index = State.buildings.IndexOf(data);
                var button = Instantiate(collectionBadgeTemplate,hud.transform);
                button.name="Collect "+data.kind+" "+index;
                button.gameObject.SetActive(true);
                button.onClick.AddListener(() => CollectBuilding(index));
                producerButtons.Add(button); producerIndices.Add(index);
                // Keep world collection badges below the HUD and modal overlays.
                button.transform.SetAsFirstSibling();
            }
        }

        GameObject PrefabFor(string kind)
        {
            switch (kind)
            {
                case "Barracks":
                case "ArmyCamp": return Resources.Load<GameObject>("ArmyBuildings/"+kind);
                case "Wall": return wallPrefab;
                case "Cannon": return cannonPrefab;
                case "ArcherTower": return archerTowerPrefab;
                case "TownHall": return townHallPrefab;
                case "GoldMine": return goldMinePrefab;
                case "ElixirCollector": return elixirCollectorPrefab;
                case "GoldStorage": return goldStoragePrefab;
                case "ElixirStorage": return elixirStoragePrefab;
                default: return null;
            }
        }

        void LateUpdate()
        {
            if(PracticeOpen){RefreshPracticeBattle();return;}
            if (State == null) return;
            var canvas = hud.GetComponent<Canvas>();
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            for (int i = 0; i < producerButtons.Count; i++)
            {
                var b = State.buildings[producerIndices[i]];
                Vector3 screen = viewCamera.WorldToScreenPoint(new Vector3(b.x+b.Size*.5f, 3.2f, b.z+b.Size*.5f));
                var button = producerButtons[i];
                button.gameObject.SetActive(!IsPlacing && !shop.activeSelf && !DetailsOpen && !ProfileOpen && selectedIndex<0 && screen.z > 0 && viewCamera.pixelRect.Contains(screen));
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)hud.transform, screen, uiCamera, out Vector2 position);
                ((RectTransform)button.transform).anchoredPosition = position;
            }
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
            if(armySummary!=null && ProfileOpen && profilePages[armyPageIndex].gameObject.activeSelf)RefreshArmyPreparation();
            if(campaignSummary!=null && ProfileOpen && profilePages[campaignPageIndex].gameObject.activeSelf)RefreshCampaignPage();
            goldLabel.text=State.gold.ToString("N0");
            elixirLabel.text=State.elixir.ToString("N0");
            gemsLabel.text=State.gems.ToString("N0");
            RefreshInteractionUI();
            RefreshReferenceUI();
            UpdateResourceBars();
            RefreshWallsHUD();
            collectLabel.text="COLLECT ALL\n"+State.CollectableGold+" gold / "+State.CollectableElixir+" elixir";
            collectButton.interactable=!IsPlacing && ((State.CollectableGold>0 && State.gold<State.GoldCapacity) || (State.CollectableElixir>0 && State.elixir<State.ElixirCapacity));
            for (int i=0;i<shopButtons.Count;i++)
            {
                var d=BuildingCatalog.Shop[i];
                bool canBuy=State.CanBuy(d.Id,out string reason);
                shopButtons[i].interactable=canBuy;
                shopDetails[i].text=d.Description+"\n"+d.Size+" x "+d.Size+" cells | Built "+State.Count(d.Id)+" / "+State.BuildingLimit(d.Id)+"\n"+(canBuy ? "Town Hall "+State.TownHallLevel : reason);
            }
            for (int i=0;i<producerButtons.Count;i++)
            {
                var b=State.buildings[producerIndices[i]];
                var d=BuildingCatalog.Find(b.kind);
                int stored=d.Resource==ResourceKind.Gold ? b.storedGold : b.storedElixir;
                bool full=State.Balance(d.Resource)>=State.Capacity(d.Resource);
                producerButtons[i].GetComponentInChildren<Text>().text=b.upgradeFinishes>0 ? "UPGRADING\n"+Duration(b.upgradeFinishes-VillageState.Now) : stored+" / "+(d.ProductionCapacity*b.level)+" "+d.Resource.ToString().ToLowerInvariant()+"\n"+(full ? "STORAGE FULL" : stored>=d.ProductionCapacity*b.level ? "FULL - COLLECT" : stored>0 ? "TAP TO COLLECT" : "PRODUCING");
                producerButtons[i].interactable=stored>0 && !full && !IsPlacing;
                if (shop.activeSelf || IsPlacing) producerButtons[i].gameObject.SetActive(false);
            }
            if(Time.unscaledTime>=messageUntil)
                guide.text=TutorialHintsVisible ? "Guide "+(State.TutorialStep+1)+"/8: "+VillageTutorial.Title(State.TutorialStep)+" - open Village Guide." : IsPlacing ? "Tap or drag on the ground to position your building." : State.Count("ElixirCollector")==0 ? "Build an Elixir Collector with gold to keep your village growing." : State.MineCount==0 ? "Open SHOP to build a Gold Mine with elixir." : "Collect resources. Build storage to increase capacity.";
            RefreshScreenshotReference();RefreshTutorialInterface();
        }

        void ShowMessage(string message) { guide.text=message;messageUntil=Time.unscaledTime+5f; }
        void SaveProgress() { if(State!=null) { if(CampaignBattleOpen && !ScoutingPractice && !WatchingPracticeReplay){SaveCampaignCheckpoint();return;} State.Accrue(VillageState.Now);if(!VillageSave.TryWrite(State,out string error)) ShowMessage(error); } }
        void OnApplicationPause(bool paused) { practicePaused=paused;if(paused){CancelGroundGesture();SaveProgress();} }
        void OnApplicationQuit() { SaveProgress(); }
        void OnDisable() { if(CampaignBattleOpen)SaveProgress();shuttingDown=true;EnhancedTouchSupport.Disable();CancelPlacement(); }

        void BuildUI()
        {
            hud=new GameObject("Village HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            hud.transform.SetParent(transform,false);
            hud.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            hud.GetComponent<Canvas>().sortingOrder=100;
            var scaler=hud.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            safe=Box("Safe Area",hud.transform,Vector2.zero,Vector2.one);
            var resources=Box("Resources",safe,new Vector2(1,1),new Vector2(1,1),new Vector2(290,200),new Vector2(-165,-115));
            goldLabel=Resource("GOLD",resources,new Color(.95f,.70f,.16f),.69f,1f);
            elixirLabel=Resource("ELIXIR",resources,new Color(.78f,.33f,.86f),.345f,.655f);
            gemsLabel=Resource("GEMS",resources,new Color(.36f,.86f,.29f),0,.31f);
            var guidePanel=Panel("Guide",safe,new Vector2(.21f,0),new Vector2(.79f,0),new Color(.11f,.15f,.075f,.92f),new Vector2(0,64),new Vector2(0,38));
            guide=Label("Tutorial Guide",guidePanel.transform,"Preparing your village...",26,Color.white);
            collectButton=Button("Collect Resources",safe,"COLLECT ALL",new Vector2(0,0),new Vector2(0,0),new Color(.78f,.50f,.10f),new Vector2(180,68),new Vector2(286,59));collectLabel=collectButton.GetComponentInChildren<Text>();collectLabel.resizeTextMaxSize=18;collectButton.onClick.AddListener(Collect);
            var shopButton=Button("Shop",safe,"SHOP",new Vector2(1,0),new Vector2(1,0),new Color(.95f,.82f,.4f),new Vector2(150,134),new Vector2(-104,88));shopButton.onClick.AddListener(OpenShop);
            Icon("Shop",shopButton.transform,new Vector2(.2f,.35f),new Vector2(.8f,.94f));
            shopButton.GetComponentInChildren<Text>().rectTransform.anchorMax=new Vector2(1,.35f);
            placementBar=Box("Placement Controls",safe,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(790,108),new Vector2(0,69)).gameObject;
            Panel("Placement Background",placementBar.transform,Vector2.zero,Vector2.one,new Color(.14f,.12f,.09f,.97f));
            placementInfo=Label("Placement Status",placementBar.transform,"",22,Color.white,new Vector2(.03f,.53f),new Vector2(.97f,.97f));
            var cancel=Button("Cancel Placement",placementBar.transform,"CANCEL",new Vector2(.19f,.04f),new Vector2(.47f,.49f),new Color(.62f,.27f,.19f));cancel.onClick.AddListener(CancelPlacement);
            confirmButton=Button("Confirm Placement",placementBar.transform,"BUILD",new Vector2(.53f,.04f),new Vector2(.81f,.49f),new Color(.36f,.65f,.13f));confirmButton.onClick.AddListener(ConfirmPlacement);
            placementBar.SetActive(false);
            shop=Box("Building Shop",safe,Vector2.zero,Vector2.one).gameObject;
            Panel("Shop Dimmer",shop.transform,Vector2.zero,Vector2.one,new Color(0,0,0,.57f));
            shopPanel=Box("Shop Window",shop.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(1180,750));
            Panel("Shop Frame",shopPanel,Vector2.zero,Vector2.one,new Color(.85f,.81f,.71f));
            Label("Shop Title",shopPanel,"RESOURCES",52,new Color(.24f,.17f,.10f),new Vector2(.08f,.85f),new Vector2(.92f,.97f),true);
            for (int i=0;i<BuildingCatalog.Shop.Count;i++)
            {
                var d=BuildingCatalog.Shop[i];
                float left=i%2==0 ? .04f : .52f;
                float bottom=i<2 ? .49f : .15f;
                var card=Panel(d.Name+" Card",shopPanel,new Vector2(left,bottom),new Vector2(left+.44f,bottom+.32f),new Color(.95f,.93f,.86f));
                Label("Building Name",card.transform,d.Name.ToUpperInvariant(),28,new Color(.25f,.17f,.10f),new Vector2(.02f,.75f),new Vector2(.98f,.98f),true);
                var previewRect=Box("Building Preview",card.transform,new Vector2(.02f,.27f),new Vector2(.4f,.77f));
                var imageRect=Box("Model Image",previewRect,Vector2.zero,Vector2.one);
                var previewImage=imageRect.gameObject.AddComponent<RawImage>();previewImage.texture=Resources.Load<Texture2D>("BuildingIcons/"+d.Id);previewImage.raycastTarget=false;
                var aspect=imageRect.gameObject.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=1;
                if(previewImage.texture==null)previewImage.enabled=false;
                shopDetails.Add(Label("Details",card.transform,"",22,new Color(.34f,.29f,.22f),new Vector2(.4f,.26f),new Vector2(.98f,.75f)));
                var buy=Button("Buy "+d.Id,card.transform,"BUILD - "+d.CostText.ToUpperInvariant(),new Vector2(.04f,.025f),new Vector2(.96f,.245f),new Color(.38f,.67f,.14f));
                buy.GetComponentInChildren<Text>().resizeTextMaxSize=22;
                buy.onClick.AddListener(()=>BeginPlacement(d.Id));shopButtons.Add(buy);
            }
            var close=Button("Close Shop",shopPanel,"BACK",new Vector2(.36f,.025f),new Vector2(.64f,.12f),new Color(.57f,.48f,.35f));close.onClick.AddListener(CloseShop);
            shop.SetActive(false);
        }

        Text Resource(string title,Transform parent,Color accent,float bottom,float top)
        {
            var panel=Box(title,parent,new Vector2(0,bottom),new Vector2(1,top));
            var track=Panel("Resource Track",panel,new Vector2(.02f,.2f),new Vector2(.92f,.82f),new Color(.07f,.085f,.07f,.92f));
            var fill=Panel("Resource Fill",track.transform,Vector2.zero,Vector2.one,new Color(accent.r*.65f,accent.g*.65f,accent.b*.65f,.85f));
            fill.raycastTarget=false;resourceFills.Add(fill.rectTransform);
            var value=Label(title+" Value",panel,"0",25,Color.white,new Vector2(.05f,.2f),new Vector2(.78f,.82f));
            var outline=value.gameObject.AddComponent<Outline>();outline.effectDistance=new Vector2(1.5f,-1.5f);outline.effectColor=Color.black;
            Icon(title=="GOLD" ? "Gold" : title=="ELIXIR" ? "Elixir" : "Gems",panel,new Vector2(.79f,.04f),new Vector2(.99f,.98f));
            return value;
        }        RectTransform Box(string name,Transform parent,Vector2 min,Vector2 max,Vector2 size=default,Vector2 position=default)
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
            var text=rect.gameObject.AddComponent<Text>();text.font=bodyFont;text.fontSize=size;text.resizeTextForBestFit=true;text.resizeTextMinSize=12;text.resizeTextMaxSize=size;text.alignment=TextAnchor.MiddleCenter;text.color=color;text.text=value;text.supportRichText=false;text.raycastTarget=false;return text;
        }
        Button Button(string name,Transform parent,string title,Vector2 min,Vector2 max,Color color,Vector2 size=default,Vector2 position=default)
        {
            var panel=Panel(name,parent,min,max,color,size,position);var button=panel.gameObject.AddComponent<Button>();button.targetGraphic=panel;
            var edge=panel.gameObject.AddComponent<Outline>();edge.effectColor=new Color(.1f,.065f,.035f,.9f);edge.effectDistance=new Vector2(2,-3);
            var label=Label("Label",panel.transform,title,36,Color.white,title:true);var outline=label.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.12f,.10f,.06f);outline.effectDistance=new Vector2(1.5f,-1.5f);return button;
        }
    }
}
