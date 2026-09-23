using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kingdoms.UI
{
    public sealed partial class VillageGameplay
    {
        PracticeBattle practiceBattle;
        PracticeReplay practiceReplay;
        GameObject practiceWorld,practiceCanvas;
        RectTransform practiceSafe;
        Text practiceStatus,practiceInstructions;
        Button practiceRetry,practiceWatch;
        readonly List<Button> practiceDeployButtons=new List<Button>();
        readonly Dictionary<int,GameObject> practiceModels=new Dictionary<int,GameObject>();
        readonly Dictionary<int,Text> practiceHealth=new Dictionary<int,Text>();
        readonly List<GameObject> practiceShots=new List<GameObject>();
        readonly List<Material> practiceMaterials=new List<Material>();
        Material raiderCloth,raiderSkin,shotMaterial;
        Vector3 homeCameraFocus;
        float homeCameraZoom,practiceAccumulator;
        bool practicePaused;
        public bool PracticeOpen=>practiceBattle!=null;
        public PracticeBattle CurrentPracticeBattle=>practiceBattle;
        public bool WatchingPracticeReplay=>practiceReplay!=null;
        public bool PracticeReplayMatches=>practiceReplay!=null && practiceReplay.Matches;

        public void OpenPracticeBattle()
        {
            if(PracticeOpen || State==null || IsPlacing || !PlayerProfile.HasPlayerName)return;
            CloseProfile();CloseShop();CloseBuildingDetails();DeselectBuilding();
            homeCameraFocus=cameraController.focus;homeCameraZoom=viewCamera.orthographicSize;
            cameraController.focus=new Vector3(0,0,-2);viewCamera.orthographicSize=14;cameraController.InputBlocked=true;
            world.gameObject.SetActive(false);hud.SetActive(false);
            practiceCanvas=new GameObject("Practice Battle UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));practiceCanvas.transform.SetParent(transform,false);
            var canvas=practiceCanvas.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=200;
            var scaler=practiceCanvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,702);scaler.matchWidthOrHeight=.5f;
            practiceSafe=Box("Practice Safe Area",practiceCanvas.transform,Vector2.zero,Vector2.one);
            var header=Panel("Practice Header",practiceSafe,new Vector2(.18f,.875f),new Vector2(.82f,.985f),new Color(.14f,.2f,.08f,.94f));
            practiceStatus=Label("Practice Status",header.transform,"",29,Color.white);ReferenceText(practiceStatus,29,Color.white);
            practiceInstructions=Label("Practice Instructions",practiceSafe,"Deploy eight raiders from the south. Destroy the three buildings. No loot or village damage.",22,Color.white,new Vector2(.13f,.8f),new Vector2(.87f,.87f));ReferenceText(practiceInstructions,22,Color.white);
            for(int i=0;i<3;i++)
            {
                int lane=i;
                var button=Button("Deploy Raider "+i,practiceSafe,new[]{"DEPLOY LEFT","DEPLOY CENTER","DEPLOY RIGHT"}[i],new Vector2(.25f+i*.17f,.025f),new Vector2(.405f+i*.17f,.13f),new Color(.53f,.77f,.2f));
                ReferenceText(button.GetComponentInChildren<Text>(),23,Color.white);button.onClick.AddListener(()=>DeployPracticeRaider(lane));practiceDeployButtons.Add(button);
            }
            var back=Button("Return From Practice",practiceSafe,"RETURN HOME",new Vector2(.02f,.03f),new Vector2(.19f,.12f),new Color(.8f,.3f,.15f));ReferenceText(back.GetComponentInChildren<Text>(),23,Color.white);back.onClick.AddListener(ClosePracticeBattle);
            practiceRetry=Button("Retry Practice",practiceSafe,"RETRY",new Vector2(.81f,.03f),new Vector2(.98f,.12f),new Color(.45f,.64f,.24f));practiceRetry.onClick.AddListener(ResetPracticeBattle);
            practiceWatch=Button("Watch Practice Replay",practiceSafe,"WATCH REPLAY",new Vector2(.83f,.89f),new Vector2(.985f,.975f),new Color(.22f,.45f,.65f));
            ReferenceText(practiceWatch.GetComponentInChildren<Text>(),21,Color.white);practiceWatch.onClick.AddListener(WatchPracticeReplay);
            raiderCloth=PracticeMaterial("Raider tunic",new Color(.1f,.55f,.8f));raiderSkin=PracticeMaterial("Raider skin",new Color(.9f,.65f,.4f));shotMaterial=PracticeMaterial("Practice shots",new Color(1,.8f,.16f),true);
            ResetPracticeBattle();
        }

        Material PracticeMaterial(string name,Color color,bool unlit=false)
        {
            var material=new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")){name=name,color=color};practiceMaterials.Add(material);return material;
        }

        public void ResetPracticeBattle()
        {StartPracticeBattle(null);}
        public void WatchPracticeReplay()
        {
            if(practiceBattle==null || practiceBattle.Outcome==PracticeOutcome.Running)return;
            StartPracticeBattle(practiceBattle.Record());
        }
        void StartPracticeBattle(PracticeBattle.Recording recording)
        {
            if(practiceCanvas==null)return;
            if(practiceWorld!=null){practiceWorld.SetActive(false);Destroy(practiceWorld);}
            foreach(var label in practiceHealth.Values){label.gameObject.SetActive(false);Destroy(label.gameObject);}
            practiceModels.Clear();practiceHealth.Clear();practiceShots.Clear();practiceAccumulator=0;practicePaused=false;
            practiceReplay=recording==null ? null : new PracticeReplay(recording);
            practiceBattle=practiceReplay==null ? new PracticeBattle() : practiceReplay.Battle;
            practiceWorld=new GameObject("Practice Battlefield");practiceWorld.transform.SetParent(transform,false);
            foreach(var building in practiceBattle.Buildings)
            {
                var model=Instantiate(PrefabFor(building.Kind),practiceWorld.transform);model.name="Practice "+building.Kind;model.SetActive(true);
                model.transform.position=PracticePosition(building);practiceModels.Add(building.Id,model);AddPracticeHealth(building);
            }
            practiceInstructions.text="Raiders use openings or break sealed walls. Destroy all three buildings to win.";
            if(WatchingPracticeReplay)practiceInstructions.text="Watching your recorded attack. Deployments play automatically.";
            foreach(var raider in practiceBattle.Raiders)CreatePracticeRaider(raider);
            foreach(var wall in practiceWorld.GetComponentsInChildren<WallSegment>())wall.RefreshConnections();
            RefreshPracticeBattle();
        }
        static Vector3 PracticePosition(PracticeBattle.Entity entity)=>new Vector3(entity.X*.01f,0,entity.Z*.01f);
        void AddPracticeHealth(PracticeBattle.Entity entity)
        {
            var text=Label("Practice Health "+entity.Id,practiceSafe,"",17,Color.white);ReferenceText(text,17,Color.white);
            text.rectTransform.anchorMin=text.rectTransform.anchorMax=new Vector2(.5f,.5f);text.rectTransform.sizeDelta=new Vector2(150,30);practiceHealth.Add(entity.Id,text);
            if(entity.Kind=="Raider" || entity.Kind=="Wall")
            {
                text.rectTransform.sizeDelta=new Vector2(30,7);
                var track=Panel("Raider Health Track",text.transform,Vector2.zero,Vector2.one,new Color(.42f,.08f,.04f));track.raycastTarget=false;
                var fill=Panel("Raider Health Fill",track.transform,Vector2.zero,Vector2.one,new Color(.4f,.95f,.2f));fill.raycastTarget=false;
            }
        }

        public void DeployPracticeRaider(int lane)
        {
            if(practiceBattle==null || WatchingPracticeReplay || !practiceBattle.Deploy(lane))return;
            var raider=practiceBattle.Raiders[practiceBattle.Raiders.Count-1];
            CreatePracticeRaider(raider);RefreshPracticeBattle();
        }
        void CreatePracticeRaider(PracticeBattle.Entity raider)
        {
            var root=new GameObject("Practice Raider "+raider.Id);root.transform.SetParent(practiceWorld.transform,false);
            root.transform.position=PracticePosition(raider);
            var body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.transform.SetParent(root.transform,false);body.transform.localPosition=new Vector3(0,.55f,0);body.transform.localScale=new Vector3(.43f,.48f,.43f);body.GetComponent<Renderer>().sharedMaterial=raiderCloth;Destroy(body.GetComponent<Collider>());
            var head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.transform.SetParent(root.transform,false);head.transform.localPosition=new Vector3(0,1.12f,0);head.transform.localScale=Vector3.one*.38f;head.GetComponent<Renderer>().sharedMaterial=raiderSkin;Destroy(head.GetComponent<Collider>());
            var spear=GameObject.CreatePrimitive(PrimitiveType.Cube);spear.transform.SetParent(root.transform,false);spear.transform.localPosition=new Vector3(.3f,.73f,0);spear.transform.localScale=new Vector3(.06f,1.25f,.06f);spear.GetComponent<Renderer>().sharedMaterial=shotMaterial;Destroy(spear.GetComponent<Collider>());
            practiceModels.Add(raider.Id,root);AddPracticeHealth(raider);
        }

        void TickPracticeBattle()
        {
            if(practicePaused)return;
            Rect area=Screen.safeArea;practiceSafe.anchorMin=new Vector2(area.xMin/Screen.width,area.yMin/Screen.height);practiceSafe.anchorMax=new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);
            practiceAccumulator+=Mathf.Min(Time.unscaledDeltaTime,.5f);
            while(practiceAccumulator>=.1f)
            {
                practiceAccumulator-=.1f;
                if(practiceReplay==null)practiceBattle.Step();else practiceReplay.Step();
                foreach(var raider in practiceBattle.Raiders)if(!practiceModels.ContainsKey(raider.Id))CreatePracticeRaider(raider);
                foreach(var strike in practiceBattle.Strikes)ShowPracticeStrike(strike);
            }
            practiceShots.RemoveAll(shot=>shot==null);
            RefreshPracticeBattle();
        }
        void ShowPracticeStrike(PracticeBattle.Strike strike)
        {
            if(!practiceModels.TryGetValue(strike.From,out var from) || !practiceModels.TryGetValue(strike.To,out var to))return;
            var shot=new GameObject("Practice Shot",typeof(LineRenderer));shot.transform.SetParent(practiceWorld.transform,false);
            var line=shot.GetComponent<LineRenderer>();line.sharedMaterial=shotMaterial;line.positionCount=2;line.widthMultiplier=strike.From<100 ? .09f : .04f;
            line.SetPosition(0,from.transform.position+Vector3.up*(strike.From==2 ? 3.3f : 1));line.SetPosition(1,to.transform.position+Vector3.up*.7f);
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;practiceShots.Add(shot);Destroy(shot,.16f);
        }
        void RefreshPracticeBattle()
        {
            int alive=0;foreach(var raider in practiceBattle.Raiders){RefreshPracticeEntity(raider);if(raider.Alive)alive++;}
            foreach(var building in practiceBattle.Buildings)RefreshPracticeEntity(building);
            bool running=practiceBattle.Outcome==PracticeOutcome.Running;
            practiceStatus.text=(running ? "PRACTICE BATTLE" : practiceBattle.Outcome.ToString().ToUpperInvariant())+"   |   "+practiceBattle.Destruction+"% destroyed\n"+practiceBattle.Remaining+" ready   •   "+alive+" fighting   •   "+Duration((PracticeBattle.TimeLimitTicks-practiceBattle.Tick)/10);
            if(WatchingPracticeReplay)practiceStatus.text="REPLAY  |  "+practiceStatus.text;
            foreach(var button in practiceDeployButtons)button.interactable=running && !WatchingPracticeReplay && practiceBattle.Remaining>0;
            practiceRetry.interactable=!running;
            practiceWatch.interactable=!running;
            if(!running)practiceInstructions.text=WatchingPracticeReplay
                ? (PracticeReplayMatches ? "Replay complete. Result matches the original attack." : "Replay mismatch detected. Retry to start a new practice attack.")
                : "Practice complete. Watch Replay, retry, or return home. No resources were spent or awarded.";
        }
        void RefreshPracticeEntity(PracticeBattle.Entity entity)
        {
            var model=practiceModels[entity.Id];model.SetActive(entity.Alive);model.transform.position=Vector3.Lerp(model.transform.position,PracticePosition(entity),1-Mathf.Exp(-22*Time.unscaledDeltaTime));
            var text=practiceHealth[entity.Id];text.gameObject.SetActive(entity.Alive && (entity.Kind!="Wall" || entity.HitPoints<entity.MaxHitPoints));
            text.text=entity.HitPoints+" / "+entity.MaxHitPoints;
            if(entity.Kind=="Raider" || entity.Kind=="Wall")
            {
                text.text="";
                var fill=(RectTransform)text.transform.Find("Raider Health Track/Raider Health Fill");fill.anchorMax=new Vector2((float)entity.HitPoints/entity.MaxHitPoints,1);
            }
            Vector3 screen=viewCamera.WorldToScreenPoint(model.transform.position+Vector3.up*(entity.Kind=="ArcherTower" ? 4 : entity.Kind=="TownHall" ? 3.8f : entity.Kind=="Raider" ? 1.7f : 2));
            var canvas=practiceCanvas.GetComponent<Canvas>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(practiceSafe,screen,canvas.renderMode==RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,out var point);text.rectTransform.anchoredPosition=point;
        }
        public void ClosePracticeBattle()
        {
            if(!PracticeOpen)return;
            practiceBattle.Surrender();practiceBattle=null;practiceReplay=null;
            if(practiceWorld!=null){practiceWorld.SetActive(false);Destroy(practiceWorld);}
            if(practiceCanvas!=null){practiceCanvas.SetActive(false);Destroy(practiceCanvas);}
            practiceCanvas=null;practiceModels.Clear();practiceHealth.Clear();practiceShots.Clear();practiceDeployButtons.Clear();
            foreach(var material in practiceMaterials)Destroy(material);practiceMaterials.Clear();
            world.gameObject.SetActive(true);hud.SetActive(PlayerProfile.HasPlayerName);
            cameraController.focus=homeCameraFocus;viewCamera.orthographicSize=homeCameraZoom;cameraController.InputBlocked=false;RefreshHUD();
        }
    }
}
