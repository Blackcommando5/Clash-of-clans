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
        Text practiceStatus,practiceInstructions,practiceResultsText;
        GameObject practiceResults,practiceScoutInfo;
        Text practiceScoutText;
        Button practiceRetry,practiceWatch,practiceChallenge,practiceSurrender,practiceBegin,deployRaiders,deployArchers,deployTanks;
        string deployedTroop="Raider";
        bool practiceSealedChallenge,practiceScouting;
        readonly List<Button> practiceDeployButtons=new List<Button>();
        readonly Dictionary<int,GameObject> practiceModels=new Dictionary<int,GameObject>();
        readonly Dictionary<int,Text> practiceHealth=new Dictionary<int,Text>();
        readonly List<GameObject> practiceShots=new List<GameObject>();
        readonly List<Material> practiceMaterials=new List<Material>();
        Material raiderCloth,archerCloth,tankArmor,raiderSkin,shotMaterial;
        Vector3 homeCameraFocus;
        float homeCameraZoom,practiceAccumulator;
        bool practicePaused;
        bool historyReplayOpen;
        public bool PracticeOpen=>practiceBattle!=null;
        public PracticeBattle CurrentPracticeBattle=>practiceBattle;
        public bool WatchingPracticeReplay=>practiceReplay!=null;
        public bool ScoutingPractice=>PracticeOpen && practiceScouting;
        public bool PracticeReplayMatches=>practiceReplay!=null && practiceReplay.Matches;

        public void OpenPracticeBattle() => OpenBattle(-1);
        void OpenBattle(int mission, PracticeBattle.Recording savedRecording=null)
        {
            if(PracticeOpen || State==null || IsPlacing || !PlayerProfile.HasPlayerName)return;
            historyReplayOpen=savedRecording!=null;
            battleMission=mission;battleRunId="";campaignResultSaved=false;campaignSaveAttempted=false;campaignMessage="";
            CloseProfile();CloseShop();CloseBuildingDetails();DeselectBuilding();
            homeCameraFocus=cameraController.focus;homeCameraZoom=viewCamera.orthographicSize;
            bool wide=(savedRecording?.Layout.Id ?? mission)>=2;
            cameraController.focus=new Vector3(0,0,wide ? 0 : -2);viewCamera.orthographicSize=wide ? 16 : 14;cameraController.InputBlocked=true;
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
            deployRaiders=Button("Select Battle Raiders",practiceSafe,"RAIDERS",new Vector2(.25f,.14f),new Vector2(.405f,.22f),new Color(.28f,.55f,.76f));
            deployArchers=Button("Select Battle Archers",practiceSafe,"ARCHERS",new Vector2(.42f,.14f),new Vector2(.575f,.22f),new Color(.35f,.60f,.25f));
            deployTanks=Button("Select Battle Tanks",practiceSafe,"TANKS",new Vector2(.59f,.14f),new Vector2(.745f,.22f),new Color(.5f,.45f,.6f));
            deployTanks.onClick.AddListener(()=>SelectDeploymentTroop("Tank"));
            deployRaiders.onClick.AddListener(()=>SelectDeploymentTroop("Raider"));deployArchers.onClick.AddListener(()=>SelectDeploymentTroop("Archer"));
            var back=Button("Return From Practice",practiceSafe,"RETURN HOME",new Vector2(.02f,.03f),new Vector2(.19f,.12f),new Color(.8f,.3f,.15f));ReferenceText(back.GetComponentInChildren<Text>(),23,Color.white);back.onClick.AddListener(ClosePracticeBattle);
            practiceSurrender=Button("Surrender Practice",practiceSafe,"SURRENDER",new Vector2(.02f,.15f),new Vector2(.19f,.23f),new Color(.65f,.25f,.16f));
            ReferenceText(practiceSurrender.GetComponentInChildren<Text>(),22,Color.white);practiceSurrender.onClick.AddListener(SurrenderPracticeBattle);
            practiceResults=Panel("Practice Results",practiceSafe,new Vector2(.25f,.17f),new Vector2(.75f,.39f),new Color(.12f,.18f,.08f,.96f)).gameObject;
            practiceResultsText=Label("Practice Results Text",practiceResults.transform,"",25,Color.white);ReferenceText(practiceResultsText,25,Color.white);
            practiceScoutInfo=Panel("Practice Scout Info",practiceSafe,new Vector2(.25f,.17f),new Vector2(.75f,.34f),new Color(.12f,.18f,.08f,.96f)).gameObject;
            practiceScoutText=Label("Practice Scout Text",practiceScoutInfo.transform,"",22,Color.white);ReferenceText(practiceScoutText,22,Color.white);
            practiceBegin=Button("Begin Practice Attack",practiceSafe,"START ATTACK",new Vector2(.38f,.025f),new Vector2(.62f,.13f),new Color(.53f,.77f,.2f));
            ReferenceText(practiceBegin.GetComponentInChildren<Text>(),25,Color.white);practiceBegin.onClick.AddListener(BeginPracticeAttack);
            practiceRetry=Button("Retry Practice",practiceSafe,"RETRY",new Vector2(.81f,.03f),new Vector2(.98f,.12f),new Color(.45f,.64f,.24f));practiceRetry.onClick.AddListener(ResetPracticeBattle);
            practiceWatch=Button("Watch Practice Replay",practiceSafe,"WATCH REPLAY",new Vector2(.83f,.89f),new Vector2(.985f,.975f),new Color(.22f,.45f,.65f));
            ReferenceText(practiceWatch.GetComponentInChildren<Text>(),21,Color.white);practiceWatch.onClick.AddListener(WatchPracticeReplay);
            practiceChallenge=Button("Switch Practice Challenge",practiceSafe,"",new Vector2(.015f,.89f),new Vector2(.17f,.975f),new Color(.48f,.35f,.19f));
            ReferenceText(practiceChallenge.GetComponentInChildren<Text>(),20,Color.white);
            practiceChallenge.onClick.AddListener(()=>SelectPracticeChallenge(!practiceSealedChallenge));
            raiderCloth=PracticeMaterial("Raider tunic",new Color(.1f,.55f,.8f));raiderSkin=PracticeMaterial("Raider skin",new Color(.9f,.65f,.4f));shotMaterial=PracticeMaterial("Practice shots",new Color(1,.8f,.16f),true);
            campaignBattleClaim=Button("Claim Campaign Battle",practiceSafe,"CLAIM",new Vector2(.81f,.03f),new Vector2(.98f,.12f),new Color(.45f,.64f,.24f));
            campaignBattleClaim.onClick.AddListener(ClaimCampaignBattle);campaignBattleClaim.gameObject.SetActive(false);
            tankArmor=PracticeMaterial("Tank armor",new Color(.38f,.32f,.48f));
            archerCloth=PracticeMaterial("Archer tunic",new Color(.22f,.62f,.18f));
            practiceSealedChallenge=mission>=0 && CampaignCatalog.Find(mission).Sealed;
            if(savedRecording!=null)StartPracticeBattle(savedRecording);else ResetPracticeBattle();
        }

        Material PracticeMaterial(string name,Color color,bool unlit=false)
        {
            var material=new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")){name=name,color=color};practiceMaterials.Add(material);return material;
        }

        public void ResetPracticeBattle()
        {if(historyReplayOpen || (battleMission>=0 && !string.IsNullOrEmpty(battleRunId)))return;StartPracticeBattle(null);}
        public void BeginPracticeAttack()
        {
            if(!ScoutingPractice || WatchingPracticeReplay)return;
            if(battleMission>=0 && !CommitCampaignArmy()){RefreshPracticeBattle();return;}
            practiceScouting=false;practiceAccumulator=0;CancelGroundGesture();deploymentInputAfterFrame=Time.frameCount;
            practiceInstructions.text=practiceSealedChallenge
                ? "WALL BREACH: Break through the sealed enclosure and destroy all three buildings."
                : "OPEN GATE: Deploy from the south. Raiders use the entrance to reach the Town Hall.";
            RefreshPracticeBattle();
        }
        public void SurrenderPracticeBattle()
        {
            if(practiceBattle==null || ScoutingPractice || WatchingPracticeReplay || practiceBattle.Outcome!=PracticeOutcome.Running)return;
            practiceBattle.Surrender();RefreshPracticeBattle();
        }
        public bool SelectPracticeChallenge(bool sealedWalls)
        {
            if(historyReplayOpen || battleMission>=0 || practiceBattle==null || (practiceBattle.Outcome==PracticeOutcome.Running && !ScoutingPractice))return false;
            practiceSealedChallenge=sealedWalls;StartPracticeBattle(null);return true;
        }
        public void WatchPracticeReplay()
        {
            if(practiceBattle==null || practiceBattle.Outcome==PracticeOutcome.Running || (battleMission>=0 && !campaignResultSaved))return;
            StartPracticeBattle(practiceBattle.Record());
        }
        void StartPracticeBattle(PracticeBattle.Recording recording)
        {
            if(practiceCanvas==null)return;
            if(practiceWorld!=null){practiceWorld.SetActive(false);Destroy(practiceWorld);}
            foreach(var label in practiceHealth.Values){label.gameObject.SetActive(false);Destroy(label.gameObject);}
            practiceModels.Clear();practiceHealth.Clear();practiceShots.Clear();practiceAccumulator=0;practicePaused=false;
            practiceReplay=recording==null ? null : new PracticeReplay(recording);
            practiceScouting=recording==null;
            practiceBattle=practiceReplay==null ? new PracticeBattle(battleMission>=0 ? CampaignCatalog.Find(battleMission).Layout : practiceSealedChallenge ? EnemyLayoutCatalog.Keep : EnemyLayoutCatalog.Gate,battleMission>=0 ? State.ArmyCount : PracticeBattle.ArmySize,battleMission>=0 ? State.ArmyCountOf("Archer") : 0,battleMission>=0 ? State.ArmyCountOf("Tank") : 0) : practiceReplay.Battle;
            practiceSealedChallenge=practiceBattle.SealedEnclosure;
            deployedTroop=practiceBattle.RaiderBudget>0 ? "Raider" : practiceBattle.ArcherBudget>0 ? "Archer" : "Tank";
            practiceWorld=new GameObject("Practice Battlefield");practiceWorld.transform.SetParent(transform,false);
            CreateDeploymentZone();
            foreach(var building in practiceBattle.Buildings)
            {
                var model=Instantiate(PrefabFor(building.Kind),practiceWorld.transform);model.name="Practice "+building.Kind;model.SetActive(true);
                model.transform.position=PracticePosition(building);practiceModels.Add(building.Id,model);AddPracticeHealth(building);
            }
            practiceInstructions.text=practiceSealedChallenge
                ? "WALL BREACH: Break through the sealed enclosure and destroy all three buildings."
                : "OPEN GATE: Raiders use the entrance. Switch challenges before deploying, or destroy all three buildings.";
            if(WatchingPracticeReplay)practiceInstructions.text="Watching your recorded attack. Deployments play automatically.";
            else practiceInstructions.text="SCOUTING: Inspect the defenses and choose a challenge. The timer starts when you press Start Attack.";
            foreach(var raider in practiceBattle.Raiders)CreatePracticeRaider(raider);
            foreach(var wall in practiceWorld.GetComponentsInChildren<WallSegment>())wall.RefreshConnections();
            RefreshPracticeBattle();
        }
        static Vector3 PracticePosition(PracticeBattle.Entity entity)=>new Vector3(entity.X*.01f,0,entity.Z*.01f);
        void AddPracticeHealth(PracticeBattle.Entity entity)
        {
            var text=Label("Practice Health "+entity.Id,practiceSafe,"",17,Color.white);ReferenceText(text,17,Color.white);
            text.rectTransform.anchorMin=text.rectTransform.anchorMax=new Vector2(.5f,.5f);text.rectTransform.sizeDelta=new Vector2(150,30);practiceHealth.Add(entity.Id,text);
            if(TroopCatalog.Find(entity.Kind)!=null || entity.Kind=="Wall")
            {
                text.rectTransform.sizeDelta=new Vector2(30,7);
                var track=Panel("Raider Health Track",text.transform,Vector2.zero,Vector2.one,new Color(.42f,.08f,.04f));track.raycastTarget=false;
                var fill=Panel("Raider Health Fill",track.transform,Vector2.zero,Vector2.one,new Color(.4f,.95f,.2f));fill.raycastTarget=false;
            }
        }

        void SelectDeploymentTroop(string troop)
        {if(!ScoutingPractice && !WatchingPracticeReplay && practiceBattle.RemainingOf(troop)>0){deployedTroop=troop;RefreshPracticeBattle();}}

        public void DeployPracticeRaider(int lane)
        {
            if(practiceBattle==null || ScoutingPractice || WatchingPracticeReplay || !practiceBattle.Deploy(lane,deployedTroop))return;
            var raider=practiceBattle.Raiders[practiceBattle.Raiders.Count-1];
            CreatePracticeRaider(raider);RefreshPracticeBattle();
        }
        void CreatePracticeRaider(PracticeBattle.Entity raider)
        {
            var root=new GameObject("Practice "+raider.Kind+" "+raider.Id);root.transform.SetParent(practiceWorld.transform,false);
            root.transform.position=PracticePosition(raider);
            var body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.transform.SetParent(root.transform,false);body.transform.localPosition=new Vector3(0,.55f,0);body.transform.localScale=new Vector3(.43f,.48f,.43f);body.GetComponent<Renderer>().sharedMaterial=raider.Kind=="Archer" ? archerCloth : raider.Kind=="Tank" ? tankArmor : raiderCloth;Destroy(body.GetComponent<Collider>());
            var head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.transform.SetParent(root.transform,false);head.transform.localPosition=new Vector3(0,1.12f,0);head.transform.localScale=Vector3.one*.38f;head.GetComponent<Renderer>().sharedMaterial=raiderSkin;Destroy(head.GetComponent<Collider>());
            if(raider.Kind=="Archer")
            {
                var bow=new GameObject("Archer Bow",typeof(LineRenderer));bow.transform.SetParent(root.transform,false);
                var line=bow.GetComponent<LineRenderer>();line.useWorldSpace=false;line.sharedMaterial=shotMaterial;line.widthMultiplier=.055f;line.positionCount=4;
                line.SetPositions(new[]{new Vector3(.32f,.32f,0),new Vector3(.58f,.7f,0),new Vector3(.32f,1.1f,0),new Vector3(.32f,.32f,0)});
                var quiver=GameObject.CreatePrimitive(PrimitiveType.Cylinder);quiver.name="Archer Quiver";quiver.transform.SetParent(root.transform,false);quiver.transform.localPosition=new Vector3(0,.7f,.25f);quiver.transform.localScale=new Vector3(.17f,.29f,.17f);quiver.GetComponent<Renderer>().sharedMaterial=shotMaterial;Destroy(quiver.GetComponent<Collider>());
            }
            else
            {
            var spear=GameObject.CreatePrimitive(PrimitiveType.Cube);spear.transform.SetParent(root.transform,false);spear.transform.localPosition=new Vector3(.3f,.73f,0);spear.transform.localScale=new Vector3(.06f,1.25f,.06f);spear.GetComponent<Renderer>().sharedMaterial=shotMaterial;Destroy(spear.GetComponent<Collider>());
            }
            if(raider.Kind=="Tank")
            {
                root.transform.localScale=Vector3.one*1.4f;
                var shield=GameObject.CreatePrimitive(PrimitiveType.Cube);shield.name="Tank Shield";shield.transform.SetParent(root.transform,false);
                shield.transform.localPosition=new Vector3(-.32f,.65f,-.18f);shield.transform.localScale=new Vector3(.55f,.75f,.16f);
                shield.GetComponent<Renderer>().sharedMaterial=tankArmor;Destroy(shield.GetComponent<Collider>());
            }
            practiceModels.Add(raider.Id,root);AddPracticeHealth(raider);
        }

        void TickPracticeBattle()
        {
            if(practicePaused)return;
            Rect area=Screen.safeArea;practiceSafe.anchorMin=new Vector2(area.xMin/Screen.width,area.yMin/Screen.height);practiceSafe.anchorMax=new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);
            if(ScoutingPractice){RefreshPracticeBattle();return;}
            HandleGroundDeployment();
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
            line.SetPosition(0,from.transform.position+Vector3.up*(from.name.Contains("ArcherTower") ? 3.3f : 1));line.SetPosition(1,to.transform.position+Vector3.up*.7f);
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;practiceShots.Add(shot);Destroy(shot,.16f);
        }
        void RefreshPracticeBattle()
        {
            int alive=0;foreach(var raider in practiceBattle.Raiders){RefreshPracticeEntity(raider);if(raider.Alive)alive++;}
            foreach(var building in practiceBattle.Buildings)RefreshPracticeEntity(building);
            bool running=practiceBattle.Outcome==PracticeOutcome.Running;
            practiceStatus.text=(running ? "PRACTICE BATTLE" : practiceBattle.Outcome.ToString().ToUpperInvariant())+"   |   "+practiceBattle.Destruction+"% destroyed\n"+practiceBattle.Remaining+" ready   •   "+alive+" fighting   •   "+Duration((PracticeBattle.TimeLimitTicks-practiceBattle.Tick)/10);
            if(WatchingPracticeReplay)practiceStatus.text="REPLAY  |  "+practiceStatus.text;
            if(ScoutingPractice)practiceStatus.text="SCOUTING  |  "+(practiceSealedChallenge ? "WALL BREACH" : "OPEN GATE")+"\n8 raiders ready  |  3-minute attack  |  No resource cost";
            foreach(var button in practiceDeployButtons)
            {
                button.gameObject.SetActive(!ScoutingPractice);
                button.interactable=running && !ScoutingPractice && !WatchingPracticeReplay && practiceBattle.RemainingOf(deployedTroop)>0;
            }
            bool selectTroops=running && !ScoutingPractice && !WatchingPracticeReplay && (practiceBattle.ArcherBudget>0 || practiceBattle.TankBudget>0);
            deployRaiders.gameObject.SetActive(selectTroops);deployArchers.gameObject.SetActive(selectTroops);deployTanks.gameObject.SetActive(selectTroops);
            deployTanks.interactable=practiceBattle.RemainingOf("Tank")>0;
            deployTanks.GetComponentInChildren<Text>().text=(deployedTroop=="Tank" ? "> " : "")+"TANKS: "+practiceBattle.RemainingOf("Tank");
            deployRaiders.interactable=practiceBattle.RemainingOf("Raider")>0;deployArchers.interactable=practiceBattle.RemainingOf("Archer")>0;
            deployRaiders.GetComponentInChildren<Text>().text=(deployedTroop=="Raider" ? "> " : "")+"RAIDERS: "+practiceBattle.RemainingOf("Raider");
            deployArchers.GetComponentInChildren<Text>().text=(deployedTroop=="Archer" ? "> " : "")+"ARCHERS: "+practiceBattle.RemainingOf("Archer");
            if(selectTroops){deployRaiders.transform.SetAsLastSibling();deployArchers.transform.SetAsLastSibling();deployTanks.transform.SetAsLastSibling();}
            practiceBegin.gameObject.SetActive(ScoutingPractice);
            practiceScoutInfo.SetActive(ScoutingPractice);
            if(ScoutingPractice)
            {
                int defenses=0;var stats=new List<string>();var kinds=new HashSet<string>();
                foreach(var building in practiceBattle.Buildings)if(building.Damage>0)
                {defenses++;if(kinds.Add(building.Kind))stats.Add(BuildingCatalog.Find(building.Kind).Name+": "+building.Damage+" dmg/s, "+(building.Range/100f).ToString("0.#")+" cells");}
                practiceScoutText.text=practiceBattle.TotalBuildings+" buildings | "+defenses+" defenses | Destroy all buildings to win"
                    +"\n"+string.Join(" | ",stats)+"\n"+practiceBattle.Layout.Approach;
                if(battleMission>=0){var mission=CampaignCatalog.Find(battleMission);practiceScoutText.text+=State.CampaignCleared(battleMission) ? "\nFirst-clear reward already claimed." : "\nFirst clear: "+mission.Gold+" gold + "+mission.Elixir+" elixir";}

            }
            practiceRetry.interactable=!running;
            practiceWatch.interactable=!running;
            practiceSurrender.gameObject.SetActive(running && !ScoutingPractice && !WatchingPracticeReplay);
            practiceResults.SetActive(!running);
            if(!running)
            {
                practiceResults.transform.SetAsLastSibling();
                practiceResultsText.text=(practiceSealedChallenge ? "WALL BREACH" : "OPEN GATE")+"  |  "+practiceBattle.Stars+" / 3 STARS"
                    +"\n"+practiceBattle.DestroyedBuildings+" / "+practiceBattle.TotalBuildings+" buildings destroyed  |  "+practiceBattle.Destruction+"%"
                    +"\n"+practiceBattle.Raiders.Count+" deployed  |  "+practiceBattle.Survivors+" survived  |  "+Duration(practiceBattle.Tick/10)+" elapsed";
            }
            practiceChallenge.interactable=!running || ScoutingPractice;
            practiceChallenge.GetComponentInChildren<Text>().text=practiceSealedChallenge ? "TRY OPEN GATE" : "TRY WALL BREACH";
            if(!running)practiceInstructions.text=WatchingPracticeReplay
                ? (PracticeReplayMatches ? "Replay complete. Result matches the original attack." : "Replay mismatch detected. Retry to start a new practice attack.")
                : "Practice complete. Watch Replay, retry, or return home. No resources were spent or awarded.";
            if(battleMission>=0)RefreshCampaignBattle();
            if(historyReplayOpen)
            {
                practiceRetry.gameObject.SetActive(false);practiceChallenge.gameObject.SetActive(false);
                foreach(var button in practiceDeployButtons)button.gameObject.SetActive(false);
                practiceStatus.text="SAVED REPLAY | "+practiceBattle.Layout.Name+"\n"+practiceBattle.Destruction+"% destroyed | "+Duration(practiceBattle.Tick/10)+" elapsed";
                if(!running)practiceResultsText.text=practiceResultsText.text.Replace(practiceSealedChallenge ? "WALL BREACH" : "OPEN GATE",practiceBattle.Layout.Name.ToUpperInvariant());
                practiceSafe.Find("Return From Practice").GetComponentInChildren<Text>().text="BACK TO HISTORY";
            }
            RefreshGroundDeployment();
        }
        void RefreshPracticeEntity(PracticeBattle.Entity entity)
        {
            var model=practiceModels[entity.Id];model.SetActive(entity.Alive);model.transform.position=Vector3.Lerp(model.transform.position,PracticePosition(entity),1-Mathf.Exp(-22*Time.unscaledDeltaTime));
            var text=practiceHealth[entity.Id];text.gameObject.SetActive(entity.Alive && (entity.Kind!="Wall" || entity.HitPoints<entity.MaxHitPoints));
            text.text=entity.HitPoints+" / "+entity.MaxHitPoints;
            if(TroopCatalog.Find(entity.Kind)!=null || entity.Kind=="Wall")
            {
                text.text="";
                var fill=(RectTransform)text.transform.Find("Raider Health Track/Raider Health Fill");fill.anchorMax=new Vector2((float)entity.HitPoints/entity.MaxHitPoints,1);
            }
            Vector3 screen=viewCamera.WorldToScreenPoint(model.transform.position+Vector3.up*(entity.Kind=="ArcherTower" ? 4 : entity.Kind=="TownHall" ? 3.8f : TroopCatalog.Find(entity.Kind)!=null ? 1.7f : 2));
            var canvas=practiceCanvas.GetComponent<Canvas>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(practiceSafe,screen,canvas.renderMode==RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,out var point);text.rectTransform.anchoredPosition=point;
        }
        public void ClosePracticeBattle()
        {
            if(!PracticeOpen)return;
            if(battleMission>=0 && !ScoutingPractice && !WatchingPracticeReplay)
            {practiceBattle.Surrender();if(!SaveCampaignResult()){RefreshPracticeBattle();return;}}
            CancelGroundGesture();
            battleMission=-1;battleRunId="";
            practiceBattle.Surrender();practiceBattle=null;practiceReplay=null;
            if(practiceWorld!=null){practiceWorld.SetActive(false);Destroy(practiceWorld);}
            if(practiceCanvas!=null){practiceCanvas.SetActive(false);Destroy(practiceCanvas);}
            practiceCanvas=null;practiceModels.Clear();practiceHealth.Clear();practiceShots.Clear();practiceDeployButtons.Clear();
            foreach(var material in practiceMaterials)Destroy(material);practiceMaterials.Clear();deploymentMaterial=null;deploymentZone=null;
            world.gameObject.SetActive(true);hud.SetActive(PlayerProfile.HasPlayerName);
            cameraController.focus=homeCameraFocus;viewCamera.orthographicSize=homeCameraZoom;cameraController.InputBlocked=false;RefreshHUD();
            if(historyReplayOpen){historyReplayOpen=false;OpenProfile(historyPageIndex);RefreshBattleHistory();}
        }
    }
}
