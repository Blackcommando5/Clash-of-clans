using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Kingdoms.UI
{
    public sealed partial class VillageGameplay
    {
        readonly List<GameObject> buildingInstances=new List<GameObject>();
        readonly List<int> visualLevels=new List<int>();
        readonly List<GameObject> constructionMarkers=new List<GameObject>();
        [SerializeField] List<RectTransform> resourceFills=new List<RectTransform>();
        int selectedIndex=-1,movingIndex=-1;
        [SerializeField] GameObject selectedBar,detailModal,selectionFootprint;
        [SerializeField] RectTransform detailPanel;
        [SerializeField] Text selectedTitle,detailTitle,detailStats,upgradeCaption,builderLabel,townLevelLabel;
        [SerializeField] Button upgradeAction;
        Material selectionMaterial;
        Vector2 pressPosition;
        bool selecting;
        bool confirmingUpgradeCancellation;
        bool viewingBuilderQueue;
        readonly List<Button> builderRows=new List<Button>();
        readonly List<int> builderJobIndices=new List<int>();
        public bool DetailsOpen => detailModal!=null && detailModal.activeSelf;
        public int SelectedBuildingIndex => selectedIndex;

        public static string Duration(long seconds)
        {
            seconds=Math.Max(0,seconds);
            return seconds>=3600 ? (seconds/3600)+"h "+((seconds%3600)/60)+"m" : seconds>=60 ? (seconds/60)+"m "+(seconds%60)+"s" : seconds+"s";
        }

        void HandleBuildingSelection()
        {
            var touches=Touch.activeTouches;
            if(touches.Count>0)
            {
                if(touches.Count!=1){selecting=false;return;}
                var t=touches[0];
                if(t.began){selecting=!OverUI(t.screenPosition);pressPosition=t.screenPosition;}
                if((t.screenPosition-pressPosition).sqrMagnitude>144) selecting=false;
                if(t.ended){if(selecting && !OverUI(t.screenPosition)) SelectAt(t.screenPosition);selecting=false;}
                return;
            }
            var mouse=Mouse.current;if(mouse==null)return;
            Vector2 point=mouse.position.ReadValue();
            if(mouse.leftButton.wasPressedThisFrame){selecting=!OverUI(point);pressPosition=point;}
            if((point-pressPosition).sqrMagnitude>144) selecting=false;
            if(mouse.leftButton.wasReleasedThisFrame){if(selecting && !OverUI(point)) SelectAt(point);selecting=false;}
        }

        public void SelectAt(Vector2 point)
        {
            if(State==null || IsPlacing || DetailsOpen || shop.activeSelf) return;
            if(Physics.Raycast(viewCamera.ScreenPointToRay(point),out RaycastHit hit,500f))
                for(int i=0;i<buildingInstances.Count;i++)
                    if(hit.transform==buildingInstances[i].transform || hit.transform.IsChildOf(buildingInstances[i].transform)) { SelectBuilding(i);return; }
            DeselectBuilding();
        }

        public void SelectBuilding(int index)
        {
            if(State==null || !PlayerProfile.HasPlayerName || IsPlacing || index<0 || index>=State.buildings.Count)return;
            selectedIndex=index;
            if(selectionFootprint==null)
            {
                selectionFootprint=GameObject.CreatePrimitive(PrimitiveType.Plane);
                selectionFootprint.name="Selected Building Footprint";selectionFootprint.transform.SetParent(transform,false);
                Destroy(selectionFootprint.GetComponent<Collider>());
                selectionMaterial=new Material(footprintMaterial);
                selectionMaterial.SetColor("_BaseColor",new Color(.68f,.96f,.16f));
                selectionMaterial.SetColor("_Color",new Color(.68f,.96f,.16f));
                selectionFootprint.GetComponent<Renderer>().sharedMaterial=selectionMaterial;
            }
            var b=State.buildings[index];
            selectionFootprint.SetActive(true);selectionFootprint.transform.position=new Vector3(b.x+b.Size*.5f,.028f,b.z+b.Size*.5f);
            selectionFootprint.transform.localScale=new Vector3((b.Size+.25f)/10f,1,(b.Size+.25f)/10f);
            RefreshDefenseRange(b);
            RefreshHUD();
        }

        void RefreshDefenseRange(PlacedBuilding building)
        {
            var definition=BuildingCatalog.Find(building.kind);
            var existing=selectionFootprint.transform.Find("Defense Range");
            if(!definition.IsDefense){if(existing!=null)existing.gameObject.SetActive(false);return;}
            var ring=existing!=null ? existing.GetComponent<LineRenderer>() : new GameObject("Defense Range",typeof(LineRenderer)).GetComponent<LineRenderer>();
            ring.transform.SetParent(selectionFootprint.transform,false);ring.gameObject.SetActive(true);
            ring.useWorldSpace=true;ring.loop=true;ring.positionCount=96;ring.widthMultiplier=.065f;ring.sharedMaterial=selectionMaterial;
            ring.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;ring.receiveShadows=false;
            for(int i=0;i<96;i++)
            {float a=i*Mathf.PI*2/96;ring.SetPosition(i,new Vector3(building.x+building.Size*.5f+Mathf.Cos(a)*definition.Range,.055f,building.z+building.Size*.5f+Mathf.Sin(a)*definition.Range));}
        }

        public void DeselectBuilding()
        {
            selectedIndex=-1;if(selectionFootprint!=null)selectionFootprint.SetActive(false);
            if(selectedBar!=null)selectedBar.SetActive(false);
        }

        public void BeginSelectedMove()
        {
            if(selectedIndex<0 || IsPlacing || DetailsOpen)return;
            movingIndex=selectedIndex;var b=State.buildings[movingIndex];placingKind=b.kind;
            preview=Instantiate(PrefabFor(b.kind),world);preview.name="Move Preview";
            foreach(var collider in preview.GetComponentsInChildren<Collider>())collider.enabled=false;
            footprint=GameObject.CreatePrimitive(PrimitiveType.Plane);footprint.transform.SetParent(world,false);
            footprint.transform.localScale=new Vector3(b.Size/10f,1,b.Size/10f);Destroy(footprint.GetComponent<Collider>());
            previewMaterial=new Material(footprintMaterial);footprint.GetComponent<Renderer>().sharedMaterial=previewMaterial;
            buildingInstances[movingIndex].SetActive(false);placementBar.SetActive(true);cameraController.InputBlocked=true;
            DeselectBuilding();SetPreviewCell(b.x,b.z);RefreshHUD();
        }

        public void OpenBuildingDetails()
        {
            if(selectedIndex<0 || IsPlacing)return;
            SetBuilderQueueVisible(false);
            confirmingUpgradeCancellation=false;
            detailModal.SetActive(true);cameraController.InputBlocked=true;RefreshHUD();
        }

        public void CloseBuildingDetails()
        {
            SetBuilderQueueVisible(false);
            confirmingUpgradeCancellation=false;
            detailModal.SetActive(false);cameraController.InputBlocked=false;RefreshHUD();
        }

        public void StartSelectedUpgrade()
        {
            if(selectedIndex<0 || IsPlacing || !DetailsOpen || viewingBuilderQueue)return;
            if(confirmingUpgradeCancellation || State.buildings[selectedIndex].upgradeFinishes>0)
            {
                if(!confirmingUpgradeCancellation)
                { confirmingUpgradeCancellation=true;RefreshHUD();return; }
                var cancelled=State.Copy();
                if(!cancelled.TryCancelUpgrade(selectedIndex,VillageState.Now,out string cancellationMessage))
                { confirmingUpgradeCancellation=false;ShowMessage(cancellationMessage);RefreshHUD();return; }
                if(!VillageSave.TryWrite(cancelled,out string cancellationError)){ShowMessage(cancellationError);return;}
                State=cancelled;ShowMessage(cancellationMessage);CloseBuildingDetails();RefreshHUD();return;
            }
            var candidate=State.Copy();
            if(!candidate.TryStartUpgrade(selectedIndex,VillageState.Now,out string message)){ShowMessage(message);RefreshHUD();return;}
            if(!VillageSave.TryWrite(candidate,out string error)){ShowMessage(error);return;}
            State=candidate;ShowMessage(message);CloseBuildingDetails();RefreshHUD();
        }

        void OpenBuilders()
        {
            if(State==null || IsPlacing || !PlayerProfile.HasPlayerName)return;
            if(ProfileOpen)CloseProfile();
            if(shop.activeSelf)CloseShop();
            State.Accrue(VillageState.Now);
            DeselectBuilding();confirmingUpgradeCancellation=false;
            if(builderRows.Count==0)
                for(int i=0;i<VillageState.BuilderCount;i++)
                {
                    int row=i;
                    var button=Button("Builder Job "+i,detailPanel,"",new Vector2(.06f,.53f-i*.25f),new Vector2(.94f,.75f-i*.25f),new Color(.37f,.49f,.23f));
                    button.GetComponentInChildren<Text>().resizeTextMaxSize=30;
                    button.onClick.AddListener(()=>OpenBuilderJob(row));
                    builderRows.Add(button);builderJobIndices.Add(-1);
                }
            SetBuilderQueueVisible(true);
            detailModal.SetActive(true);cameraController.InputBlocked=true;RefreshHUD();
        }

        void SetBuilderQueueVisible(bool visible)
        {
            viewingBuilderQueue=visible;
            foreach(var row in builderRows)row.gameObject.SetActive(visible);
            if(upgradeAction!=null)upgradeAction.gameObject.SetActive(!visible);
        }

        void OpenBuilderJob(int row)
        {
            if(!viewingBuilderQueue || row<0 || row>=builderJobIndices.Count)return;
            int index=builderJobIndices[row];
            State.Accrue(VillageState.Now);
            if(index<0 || index>=State.buildings.Count || State.buildings[index].upgradeFinishes==0)
            { RefreshHUD();return; }
            SelectBuilding(index);OpenBuildingDetails();
        }

        void RefreshBuilderQueue()
        {
            detailTitle.text="BUILDERS  "+(VillageState.BuilderCount-State.BusyBuilders)+" / "+VillageState.BuilderCount+" FREE";
            detailStats.text="";
            var jobs=new List<int>();
            for(int i=0;i<State.buildings.Count;i++)if(State.buildings[i].upgradeFinishes>0)jobs.Add(i);
            jobs.Sort((a,b)=>{int order=State.buildings[a].upgradeFinishes.CompareTo(State.buildings[b].upgradeFinishes);return order!=0 ? order : a.CompareTo(b);});
            for(int i=0;i<builderRows.Count;i++)
            {
                int index=i<jobs.Count ? jobs[i] : -1;builderJobIndices[i]=index;
                builderRows[i].interactable=index>=0;
                string caption="BUILDER AVAILABLE\nSelect a building in your village to upgrade.";
                if(index>=0)
                {
                    var b=State.buildings[index];
                    caption=BuildingCatalog.Find(b.kind).Name+"  ("+b.x+", "+b.z+")  •  Level "+b.level+" → "+(b.level+1)+"\n"+Duration(b.upgradeFinishes-VillageState.Now)+" remaining  •  VIEW UPGRADE";
                }
                builderRows[i].GetComponentInChildren<Text>().text=caption;
            }
        }

        void RefreshInteractionUI()
        {
            if(builderLabel==null)return;
            builderLabel.text=(VillageState.BuilderCount-State.BusyBuilders)+" / "+VillageState.BuilderCount;
            townLevelLabel.text="TOWN HALL "+State.TownHallLevel;
            selectedBar.SetActive(selectedIndex>=0 && !IsPlacing && !shop.activeSelf && !DetailsOpen);
            if(guide!=null)guide.transform.parent.gameObject.SetActive(selectedIndex<0 || IsPlacing || DetailsOpen);
            RefreshLayouts();
            for(int i=0;i<buildingInstances.Count;i++) UpdateBuildingVisual(i);
            if(viewingBuilderQueue){RefreshBuilderQueue();return;}
            if(selectedIndex<0 || selectedIndex>=State.buildings.Count)return;
            var b=State.buildings[selectedIndex];var d=BuildingCatalog.Find(b.kind);
            selectedTitle.text=d.Name+" (Level "+b.level+")";
            detailTitle.text=selectedTitle.text;
            string stats=d.ProductionPerSecond>0 ? "Production: "+(d.ProductionPerSecond*b.level*60)+" "+d.Resource.ToString().ToLowerInvariant()+" / minute\nProducer capacity: "+(d.ProductionCapacity*b.level).ToString("N0") : d.StorageBonus>0 ? "Village capacity: +"+(d.StorageBonus*b.level).ToString("N0")+" "+d.Resource.ToString().ToLowerInvariant() : "Village level: "+b.level+"\nUnlocks more resource buildings";
            if(b.kind=="Wall")
            {
                detailStats.text="One grid cell. Adjacent wall segments connect automatically.\n\nWall upgrades and combat damage are under development.";
                upgradeCaption.text="UPGRADES COMING LATER";upgradeAction.interactable=false;return;
            }
            bool allowed=State.CanUpgrade(selectedIndex,out string reason);
            if(b.kind=="ArmyCamp")stats="Army capacity: +"+(VillageState.ArmySpacesPerCampLevel*b.level)+" spaces\nKeeps capacity while upgrading.\nFree, instant Raider preparation.";
            if(b.kind=="Barracks")stats="Unlocks Archers and Army Camps.\nPreparation remains free and instant.\nBarracks upgrades are not available yet.";
            if(d.IsDefense)stats="Hit points: "+(d.HitPoints*b.level)+"\nDamage / second: "+(d.DamagePerSecond*b.level)+"\nRange: "+d.Range+" cells\nTry practice combat from Attack.";
            if(b.upgradeFinishes>0)
            {
                detailStats.text=stats+"\n\nUPGRADING TO LEVEL "+(b.level+1)+"\n"+Duration(b.upgradeFinishes-VillageState.Now)+" remaining\n"+
                    (confirmingUpgradeCancellation ? "Keep level "+b.level+" and free the builder?\nRefund: "+State.UpgradeCancellationRefund(selectedIndex).ToString("N0")+" "+VillageState.UpgradeResource(b).ToString().ToLowerInvariant()+"\n50% of cost; excess over storage is lost.\nClose this window to keep upgrading." : "Cancel for 50% back, limited by storage.");
                upgradeCaption.text=confirmingUpgradeCancellation ? "CONFIRM CANCELLATION" : "CANCEL UPGRADE";upgradeAction.interactable=true;
            }
            else
            {
                detailStats.text=stats+"\n\n"+(b.level>=3 ? "Maximum available level" : "Next level: "+(b.level+1)+"\nTime: "+Duration(VillageState.UpgradeSeconds(b))+"\n"+reason);
                upgradeCaption.text=confirmingUpgradeCancellation ? "UPGRADE COMPLETED" : b.level>=3 ? "MAX LEVEL" : "UPGRADE\n"+VillageState.UpgradeCost(b).ToString("N0")+" "+VillageState.UpgradeResource(b).ToString().ToUpperInvariant();
                upgradeAction.interactable=allowed && !confirmingUpgradeCancellation;
            }
        }

        void UpdateBuildingVisual(int index)
        {
            while(visualLevels.Count<=index){visualLevels.Add(0);constructionMarkers.Add(null);}
            var b=State.buildings[index];var instance=buildingInstances[index];
            if(visualLevels[index]!=b.level)
            {
                visualLevels[index]=b.level;
                var existing=instance.transform.Find("Level Banner");if(existing!=null)Destroy(existing.gameObject);
                if(b.level>1)
                {
                    var flag=GameObject.CreatePrimitive(PrimitiveType.Cube);flag.name="Level Banner";flag.transform.SetParent(instance.transform,false);
                    flag.transform.localPosition=new Vector3(0,2.7f,0);flag.transform.localScale=new Vector3(.6f*b.level,.18f,.2f);
                    Destroy(flag.GetComponent<Collider>());flag.GetComponent<Renderer>().sharedMaterial=goldStoragePrefab.GetComponentInChildren<Renderer>().sharedMaterial;
                }
            }
            if(b.upgradeFinishes>0 && constructionMarkers[index]==null)
            {
                var marker=new GameObject("Construction Scaffold");marker.transform.SetParent(instance.transform,false);constructionMarkers[index]=marker;
                var material=goldStoragePrefab.GetComponentInChildren<Renderer>().sharedMaterial;
                foreach(float side in new[]{-1f,1f})
                {
                    var beam=GameObject.CreatePrimitive(PrimitiveType.Cube);beam.transform.SetParent(marker.transform,false);beam.transform.localPosition=new Vector3(side*(b.Size*.5f-.15f),.9f,0);beam.transform.localScale=new Vector3(.18f,1.8f,b.Size);
                    Destroy(beam.GetComponent<Collider>());beam.GetComponent<Renderer>().sharedMaterial=material;
                }
            }
            else if(b.upgradeFinishes==0 && constructionMarkers[index]!=null){Destroy(constructionMarkers[index]);constructionMarkers[index]=null;}
        }

        void BuildInteractionUI()
        {
            var builders=Button("Builders",safe,"",new Vector2(.5f,1),new Vector2(.5f,1),new Color(.27f,.22f,.15f,.92f),new Vector2(190,78),new Vector2(-120,-64));
            Icon("Builder",builders.transform,new Vector2(.02f,.08f),new Vector2(.42f,.92f));
            builderLabel=builders.GetComponentInChildren<Text>();builderLabel.rectTransform.anchorMin=new Vector2(.4f,0);builders.onClick.AddListener(OpenBuilders);
            var level=Panel("Village Level",safe,new Vector2(.5f,1),new Vector2(.5f,1),new Color(.19f,.24f,.1f,.82f),new Vector2(215,60),new Vector2(110,-65));
            townLevelLabel=Label("Town Hall Tier",level.transform,"",25,Color.white);
            selectedBar=Panel("Building Actions",safe,new Vector2(.5f,0),new Vector2(.5f,0),new Color(.19f,.15f,.09f,.94f),new Vector2(640,160),new Vector2(0,92)).gameObject;
            selectedTitle=Label("Selected Name",selectedBar.transform,"",31,Color.white,new Vector2(.02f,.66f),new Vector2(.98f,.98f),true);
            var info=Button("Building Info",selectedBar.transform,"INFO / UPGRADE",new Vector2(.05f,.07f),new Vector2(.49f,.61f),new Color(.37f,.66f,.15f));info.onClick.AddListener(OpenBuildingDetails);
            var move=Button("Move Building",selectedBar.transform,"MOVE",new Vector2(.53f,.07f),new Vector2(.83f,.61f),new Color(.68f,.5f,.22f));move.onClick.AddListener(BeginSelectedMove);
            var close=Button("Deselect Building",selectedBar.transform,"X",new Vector2(.86f,.07f),new Vector2(.97f,.61f),new Color(.64f,.22f,.13f));close.onClick.AddListener(()=>{DeselectBuilding();RefreshHUD();});
            selectedBar.SetActive(false);
            detailModal=Box("Building Details",safe,Vector2.zero,Vector2.one).gameObject;
            Panel("Details Dimmer",detailModal.transform,Vector2.zero,Vector2.one,new Color(0,0,0,.65f));
            detailPanel=Box("Details Window",detailModal.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(860,650));
            Panel("Details Frame",detailPanel,Vector2.zero,Vector2.one,new Color(.86f,.82f,.73f));
            detailTitle=Label("Details Title",detailPanel,"",42,new Color(.22f,.15f,.08f),new Vector2(.04f,.83f),new Vector2(.9f,.98f),true);
            detailStats=Label("Details Stats",detailPanel,"",30,new Color(.25f,.21f,.14f),new Vector2(.06f,.26f),new Vector2(.94f,.81f));
            upgradeAction=Button("Confirm Upgrade",detailPanel,"UPGRADE",new Vector2(.28f,.045f),new Vector2(.72f,.23f),new Color(.36f,.66f,.12f));upgradeCaption=upgradeAction.GetComponentInChildren<Text>();upgradeCaption.resizeTextMaxSize=30;upgradeAction.onClick.AddListener(StartSelectedUpgrade);
            var back=Button("Close Details",detailPanel,"X",new Vector2(.91f,.86f),new Vector2(.98f,.97f),new Color(.76f,.2f,.12f));back.onClick.AddListener(CloseBuildingDetails);
            detailModal.SetActive(false);
        }

        void Icon(string kind,Transform parent,Vector2 min,Vector2 max)
        {
            var rect=Box(kind+" Icon",parent,min,max);var icon=rect.gameObject.AddComponent<VillageIcon>();icon.kind=kind;icon.raycastTarget=false;icon.SetVerticesDirty();
        }

        void RefreshLayouts()
        {
            if(safe==null)return;
            if(State!=null)RefreshReferenceUI();
            if(referenceShopRoot==null && fitWindowsToScreen && shopPanel!=null)shopPanel.localScale=Vector3.one*Mathf.Min(1f,Mathf.Min(safe.rect.width/1250f,safe.rect.height/800f));
            if(fitWindowsToScreen && detailPanel!=null)detailPanel.localScale=Vector3.one*Mathf.Min(1f,Mathf.Min(safe.rect.width/(referenceShopRoot==null ? 920f : 1260f),safe.rect.height/(referenceShopRoot==null ? 710f : 790f)));
        }

        void UpdateResourceBars()
        {
            if(resourceFills.Count<3)return;
            resourceFills[0].anchorMax=new Vector2(Mathf.Clamp01((float)State.gold/State.GoldCapacity),1);
            resourceFills[1].anchorMax=new Vector2(Mathf.Clamp01((float)State.elixir/State.ElixirCapacity),1);
            resourceFills[2].anchorMax=new Vector2(1,1);
        }

        void OnDestroy(){if(selectionMaterial!=null)Destroy(selectionMaterial);ReleaseReferenceScenery();foreach(var material in practiceMaterials)if(material!=null)Destroy(material);}
    }
}
