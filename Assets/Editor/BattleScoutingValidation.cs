using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Kingdoms;
using Kingdoms.UI;

[InitializeOnLoad]
public static class BattleScoutingValidation
{
    const string Pending="BattleScouting.Pending";
    static BattleScoutingValidation(){EditorApplication.update+=Tick;}
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Call(VillageGameplay game,string method,params object[] args)=>typeof(VillageGameplay).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,args);
    static Button Button(string name)=>GameObject.Find(name).GetComponent<Button>();
    static void Click(string name)=>Button(name).onClick.Invoke();
    static string Summary=>GameObject.Find("Practice Scout Text").GetComponent<Text>().text;
    static void Capture(string name,int width=1600,int height=702)=>typeof(ResourceMilestoneValidation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{name,width,height});
    static void CheckDefense(VillageGameplay game,PracticeBattle.Entity defense)
    {
        Check(game.InspectScoutBuilding(defense.Id) && game.InspectedScoutBuildingId==defense.Id,"Select authored defense");
        Check(Summary.Contains("Health: "+defense.HitPoints+" / "+defense.MaxHitPoints) && Summary.Contains("Damage: "+defense.Damage+" / second"),"Stats from selected entity");
        var ring=GameObject.Find("Scout Defense Range").GetComponent<LineRenderer>();
        Check(ring.loop && ring.positionCount==96,"Closed range circle");
        for(int i=0;i<96;i++)
        {
            Vector3 point=ring.GetPosition(i);float radius=Vector2.Distance(new Vector2(point.x,point.z),new Vector2(defense.X*.01f,defense.Z*.01f));
            Check(Mathf.Abs(radius-defense.Range*.01f)<.001f,"Every circle sample matches center-based combat range");
        }
    }
    static void InspectLayout(VillageGameplay game)
    {
        string village=JsonUtility.ToJson(game.State);ulong battle=game.CurrentPracticeBattle.StateHash();string disk=PlayerPrefs.GetString(VillageSave.Key);
        var defenses=new List<PracticeBattle.Entity>();
        foreach(var entity in game.CurrentPracticeBattle.Buildings)if(entity.Damage>0)defenses.Add(entity);
        game.ClearScoutInspection();
        foreach(var defense in defenses){Click("Next Scout Defense");Check(game.InspectedScoutBuildingId==defense.Id,"Defense cycle follows authored order");CheckDefense(game,defense);}
        Click("Next Scout Defense");Check(game.InspectedScoutBuildingId==defenses[0].Id,"Defense cycle wraps");
        Check(!game.InspectScoutBuilding(-1) && !game.InspectScoutBuilding(999) && game.InspectedScoutBuildingId==defenses[0].Id,"Unknown IDs preserve selection");
        Check(game.InspectScoutBuilding(0) && Summary.Contains("one star") && GameObject.Find("Scout Defense Range")==null,"Town Hall role without range");
        foreach(var entity in game.CurrentPracticeBattle.Buildings)
        {
            if(entity.Kind=="Wall"){Check(game.InspectScoutBuilding(entity.Id) && Summary.Contains("do not count") && GameObject.Find("Scout Defense Range")==null,"Wall role without range");break;}
        }
        foreach(var entity in game.CurrentPracticeBattle.Buildings)
            if(entity.Kind=="GoldStorage")Check(game.InspectScoutBuilding(entity.Id) && Summary.Contains("No attack") && GameObject.Find("Scout Defense Range")==null,"Storage contributes to victory without range");
        Click("Clear Scout Inspection");Check(game.InspectedScoutBuildingId==-1 && Summary.Contains("buildings |") && GameObject.Find("Scout Building Outline")==null,"Clear restores overview");
        Check(JsonUtility.ToJson(game.State)==village && game.CurrentPracticeBattle.StateHash()==battle && game.CurrentPracticeBattle.Tick==0 && PlayerPrefs.GetString(VillageSave.Key)==disk,"Scouting cannot spend army/resources, advance combat or write save");
    }
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').EndsWith("/.utmp/ResourceValidation/Assets") || PlayerSettings.companyName!="KingdomsResourceValidation" || PlayerSettings.productName!="ResourceMilestoneTests")throw new InvalidOperationException("Use isolated validation project and identity.");
        try
        {
            typeof(GroundDeploymentValidation).GetMethod("Rules",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
            typeof(BattleRecoveryValidation).GetMethod("Rules",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
            var state=VillageState.Create(VillageState.Now);state.campaignCleared=15;state.TrySetArmyCount("Raider",8,out _);
            Check(VillageSave.TryWrite(state,out _) && PlayerProfile.TrySaveName("ScoutChief",out _),"Seed UI");
            EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");SessionState.SetInt("BattleScouting.Step",0);SessionState.SetString("BattleScouting.Start",DateTime.UtcNow.Ticks.ToString());SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        }catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false))return;
        try
        {
            Check((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString("BattleScouting.Start","0")))).TotalSeconds<180,"UI timeout");
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();if(game==null || game.State==null)return;
            int step=SessionState.GetInt("BattleScouting.Step",0);
            if(step==0){Check(!game.InspectScoutBuilding(1),"Closed battle guard");game.OpenPracticeBattle();SessionState.SetInt("BattleScouting.Step",1);return;}
            if(step==1)
            {
                InspectLayout(game);Physics.SyncTransforms();Canvas.ForceUpdateCanvases();
                var cannon=GameObject.Find("Practice Cannon");Vector2 point=game.viewCamera.WorldToScreenPoint(cannon.GetComponentInChildren<Collider>().bounds.center);
                Check(game.TryInspectScoutAtScreen(point) && game.InspectedScoutBuildingId==1,"Screen tap picks actual enemy collider");
                Capture("scout-cannon-range.png");Capture("scout-cannon-range-16x9.png",1280,720);
                game.ClearScoutInspection();
                Call(game,"BeginScoutGesture",point);Call(game,"MoveScoutGesture",point+new Vector2(30,0));Call(game,"EndScoutGesture",point);Check(game.InspectedScoutBuildingId==-1,"Drag out and back rejected");
                Call(game,"BeginScoutGesture",point);Call(game,"OnApplicationFocus",false);Call(game,"EndScoutGesture",point);Check(game.InspectedScoutBuildingId==-1,"Focus loss cancels pending tap");
                Call(game,"BeginScoutGesture",point);Call(game,"OnApplicationPause",true);Call(game,"EndScoutGesture",point);Check(!game.InspectScoutBuilding(1) && game.InspectedScoutBuildingId==-1,"Pause cancels gesture and blocks selection");Call(game,"OnApplicationPause",false);
                var button=Button("Next Scout Defense").GetComponent<RectTransform>();Vector2 ui=RectTransformUtility.WorldToScreenPoint(null,button.TransformPoint(button.rect.center));
                Check(!game.TryInspectScoutAtScreen(ui) && !game.TryInspectScoutAtScreen(new Vector2(float.NaN,0)) && !game.TryInspectScoutAtScreen(new Vector2(float.PositiveInfinity,0)) && !game.TryInspectScoutAtScreen(new Vector2(-1,-1)),"UI, nonfinite and offscreen input ignored");
                Call(game,"BeginScoutGesture",ui);Call(game,"EndScoutGesture",point);Check(game.InspectedScoutBuildingId==-1,"UI-origin gesture rejected");
                Call(game,"BeginScoutGesture",point);Call(game,"EndScoutGesture",point);Check(game.InspectedScoutBuildingId==1,"Single valid tap selects");
                Vector2 empty=new Vector2(Screen.width*.9f,Screen.height*.55f);Check(!game.TryInspectScoutAtScreen(empty) && game.InspectedScoutBuildingId==-1,"Empty ground clears inspection");
                game.InspectNextDefense();Check(game.SelectPracticeChallenge(true) && game.InspectedScoutBuildingId==-1 && GameObject.Find("Scout Defense Range")==null,"Challenge switch clears old selection");InspectLayout(game);
                game.InspectNextDefense();game.BeginPracticeAttack();Check(!game.InspectScoutBuilding(1) && GameObject.Find("Next Scout Defense")==null && GameObject.Find("Scout Defense Range")==null && GameObject.Find("Scout Building Outline")==null,"Attack removes inspection controls and geometry");
                game.DeployPracticeRaider(1);Check(game.CurrentPracticeBattle.Raiders.Count==1,"Live deployment still works");game.SurrenderPracticeBattle();game.WatchPracticeReplay();Check(!game.InspectScoutBuilding(1) && GameObject.Find("Next Scout Defense")==null,"Replay rejects scouting input");
                game.ClosePracticeBattle();Check(GameObject.Find("Scout Defense Range")==null && !game.cameraController.InputBlocked,"Home cleanup");game.OpenCampaignBattle(0);SessionState.SetInt("BattleScouting.Step",2);return;
            }
            if(step>=2 && step<=5)
            {
                int mission=step-2;Check(game.ScoutingPractice && game.CurrentPracticeBattle.Layout.Id==mission,"Campaign scout");InspectLayout(game);game.InspectNextDefense();Check(Summary.Contains("reward already claimed"),"Inspection retains reward status");
                if(mission==2){game.InspectNextDefense();Capture("scout-crossfire-range.png");}
                if(mission==3)
                {
                    game.InspectScoutBuilding(4);Capture("scout-storage-details.png");
                    game.State.elixir=-1;game.BeginPracticeAttack();Check(game.ScoutingPractice && game.InspectedScoutBuildingId==4 && GameObject.Find("Practice Instructions").GetComponent<Text>().text.Contains("save"),"Failed army-commit save retains scouting and error");game.State.elixir=500;
                    game.BeginPracticeAttack();Check(game.CampaignBattleOpen && !game.ScoutingPractice && game.State.HasCampaignRun && game.State.ArmyCount==0 && GameObject.Find("Scout Defense Range")==null,"Campaign start commits normally");game.ClosePracticeBattle();
                    game.ResumeCampaignAttack();Check(!game.ScoutingPractice && GameObject.Find("Next Scout Defense")==null && !game.InspectScoutBuilding(1),"Resume does not reopen scouting");game.SurrenderPracticeBattle();game.ClosePracticeBattle();
                    Finish("PASS: tap enemy-model colliders; next-defense cycling/wrap; health/damage from selected entity; all 96 circle samples match actual center-based range; Town Hall, wall and storage roles; clear/empty-ground overview restoration; frozen battle hash/tick and unchanged army/economy/save while inspecting; both practice challenges and all four campaign layouts; UI/nonfinite/offscreen/drag/focus/pause gesture guards; challenge/attack/replay/home cleanup; normal deployment and army commitment; failed-commit save error retained; resumed attacks reject scouting. Ground-deployment, recovery, saved-replay, history, campaign, mixed-army, Tank, tutorial and resource/progression rule regressions passed. Unity "+Application.unityVersion+". Screenshots at 1600x702 and 1280x720. Physical multitouch and phone performance remain untested; no APK rebuilt.",0);return;
                }
                int army=game.State.ArmyCount;game.ClosePracticeBattle();Check(game.State.ArmyCount==army && !game.State.HasCampaignRun,"Returning from scout preserves prepared army");game.OpenCampaignBattle(mission+1);SessionState.SetInt("BattleScouting.Step",step+1);return;
            }
        }catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Finish(string message,int code){SessionState.SetBool(Pending,false);File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),"BattleScoutingValidation.txt"),message);EditorApplication.Exit(code);}
}
