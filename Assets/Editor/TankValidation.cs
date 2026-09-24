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
public static class TankValidation
{
    const string Pending="TankValidation.Pending";
    static TankValidation(){EditorApplication.update+=Tick;}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Click(string name)=>GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
    static VillageState Seed()
    {
        var s=VillageState.Create(VillageState.Now);s.elixir=10000;
        Check(s.TryPlace("Barracks",5,5,s.lastProduction,out _) && s.TryPlace("ArmyCamp",9,5,s.lastProduction,out _),"Facilities");
        Check(!s.TrySetArmyCount("Tank",1,out _),"Level 1 Barracks locks Tank");
        int index=s.buildings.FindIndex(b=>b.kind=="Barracks");long now=s.lastProduction;
        Check(s.TryStartUpgrade(index,now,out _) && !s.TroopUnlocked("Tank"),"Tank remains locked during upgrade");
        Check(s.TryCancelUpgrade(index,now,out _) && !s.TroopUnlocked("Tank"),"Canceled upgrade keeps Tank locked");
        Check(s.TryStartUpgrade(index,now,out _),"Restart Barracks upgrade");s.Accrue(now+30);
        Check(s.TroopUnlocked("Tank") && !s.CanUpgrade(index,out _),"Completion unlocks Tank and caps Barracks level");return s;
    }
    static void Rules()
    {
        var s=Seed();Check(s.TrySetArmyCount("Tank",2,out _) && s.TrySetArmyCount("Raider",4,out _) && s.TrySetArmyCount("Archer",2,out _) && s.ArmyHousing==16 && s.ArmyCount==8,"Three-type housing");
        string json=JsonUtility.ToJson(s);Check(!s.TrySetArmyCount("Tank",3,out _) && JsonUtility.ToJson(s)==json,"Overflow atomic");
        Check(VillageState.TryDeserialize(json,out var loaded) && loaded.ArmyCountOf("Tank")==2,"Tank save reload");
        loaded.buildings.Find(b=>b.kind=="Barracks").level=1;Check(!loaded.IsValid(),"Locked Tank save rejected");
        foreach(bool sealedWalls in new[]{false,true})
        {
            var b=new PracticeBattle(sealedWalls,8,2,2);var hashes=new List<ulong>();
            Check(b.DeployAt(-200,-900,"Tank") && b.DeployAt(200,-900,"Tank") && !b.DeployAt(0,-900,"Tank"),"Tank budget");
            for(int i=0;i<4;i++)b.Deploy(i%3);hashes.Add(b.StateHash());
            while(b.Outcome==PracticeOutcome.Running){b.Step();if(b.Tick==5){b.DeployAt(-400,-1000,"Archer");b.DeployAt(400,-1000,"Archer");}hashes.Add(b.StateHash());}
            Check(b.Outcome==PracticeOutcome.Victory,"Tank composition victory "+sealedWalls);
            var replay=new PracticeReplay(b.Record());Check(replay.Battle.StateHash()==hashes[0],"Tank initial replay");
            while(!replay.Finished){replay.Step();Check(replay.Battle.StateHash()==hashes[replay.Battle.Tick],"Tank replay every tick");}Check(replay.Matches && replay.Battle.TankBudget==2,"Tank replay final");
            if(!sealedWalls)
            {
                Check(s.TryBeginCampaign(0,out _) && s.campaignTanks==2 && s.ArmyCount==0,"Tank commitment");
                Check(VillageState.TryDeserialize(JsonUtility.ToJson(s),out var committed) && committed.campaignTanks==2,"Committed Tank snapshot reload");
                committed.buildings.Find(building=>building.kind=="Barracks").level=1;Check(!committed.IsValid(),"Committed Tank unlock required");
                var wrong=new PracticeBattle(false,8,2,1);wrong.Surrender();Check(!s.TryFinishCampaign(s.campaignRunId,wrong.Record(),out _),"Tank composition mismatch rejected");
                Check(s.TryFinishCampaign(s.campaignRunId,b.Record(),out _),"Tank result verified");string id=s.campaignRunId;
                Check(s.TryClaimCampaign(id,out _) && !s.TryClaimCampaign(id,out _),"Tank reward once");
            }
        }
        var tank=new PracticeBattle(false,1,0,1);tank.Buildings[1].HitPoints=tank.Buildings[2].HitPoints=0;tank.DeployAt(0,-1000,"Tank");tank.Step();tank.Step();var unit=tank.Raiders[0];int distance=unit.X*unit.X+(unit.Z+1000)*(unit.Z+1000);Check(unit.HitPoints==300 && distance>0 && distance<=18*18,"Tank HP and slower movement");
        tank.Raiders[0].X=0;tank.Raiders[0].Z=0;tank.Buildings[1].HitPoints=tank.Buildings[2].HitPoints=0;tank.Step();Check(tank.Buildings[0].HitPoints==600,"Tank melee range");
        tank.Raiders[0].Z=100;tank.Step();Check(tank.Buildings[0].HitPoints==584,"Tank melee damage");tank.Step();Check(tank.Buildings[0].HitPoints==584,"Tank cooldown");
        var preference=new PracticeBattle(false,1,0,1);preference.DeployAt(0,-1000,"Tank");preference.Raiders[0].Z=100;preference.Step();Check(preference.Buildings[0].HitPoints==600,"Tank prioritizes live defenses over adjacent hall");
        foreach(int count in new[]{-1,21,int.MaxValue}){bool rejected=false;try{new PracticeBattle(false,Math.Max(1,count),0,count);}catch(ArgumentOutOfRangeException){rejected=true;}Check(rejected,"Invalid Tank budgets");}
        var old=Seed();old.TrySetArmyCount("Raider",8,out _);old.TryBeginCampaign(0,out _);var win=new PracticeBattle();for(int i=0;i<8;i++)win.Deploy(i%3);while(win.Outcome==PracticeOutcome.Running)win.Step();old.TryFinishCampaign(old.campaignRunId,win.Record(),out _);
        json=JsonUtility.ToJson(old).Replace("\"version\":8","\"version\":6");PlayerPrefs.DeleteKey("Kingdoms.Village.pre-v7");PlayerPrefs.SetString(VillageSave.Key,json);
        Check(VillageSave.TryLoad(out loaded,out _) && loaded.version==8 && loaded.campaignTanks==0 && loaded.campaignOutcome==PracticeOutcome.Victory && loaded.campaignRunId==old.campaignRunId && PlayerPrefs.GetString("Kingdoms.Village.pre-v7")==json,"v6 pending reward migration and backup");
        Check(loaded.TryClaimCampaign(loaded.campaignRunId,out _),"Migrated reward claim");
    }
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').EndsWith("/.utmp/ResourceValidation/Assets") || PlayerSettings.companyName!="KingdomsResourceValidation" || PlayerSettings.productName!="ResourceMilestoneTests")throw new InvalidOperationException("Use isolated project.");
        try{Rules();EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");Check(VillageSave.TryWrite(Seed(),out _),"Seed save");PlayerProfile.TrySaveName("TankChief",out _);SessionState.SetInt("TankValidation.Step",0);SessionState.SetString("TankValidation.Start",DateTime.UtcNow.Ticks.ToString());SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();}catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Capture(string name)=>typeof(ResourceMilestoneValidation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{name,1600,702});
    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false))return;
        try
        {
            Check((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString("TankValidation.Start","0")))).TotalSeconds<120,"Timeout");if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();if(game==null || game.State==null)return;
            int step=SessionState.GetInt("TankValidation.Step",0);
            if(step==0){game.OpenArmyPreparation();Click("Prepare Tanks");Click("Fill Army");Check(game.State.ArmyCountOf("Tank")==4 && game.State.ArmyHousing==16,"Tank-only fill UI");Click("Remove Raider");Check(game.State.ArmyCountOf("Tank")==3,"Remove selected Tank");Click("Add Raider");Capture("tank-preparation.png");Click("Open Campaign");Click("Campaign Gate");SessionState.SetInt("TankValidation.Step",1);return;}
            if(step==1)
            {
                game.BeginPracticeAttack();Check(game.State.campaignTanks==4 && game.CurrentPracticeBattle.TankBudget==4,"Tank-only snapshot");
                Check(game.TryDeployPracticeAtScreen(game.viewCamera.WorldToScreenPoint(new Vector3(0,0,-10))),"Tank default ground deployment");Click("Deploy Raider 0");Check(game.CurrentPracticeBattle.Raiders.Count==2 && game.CurrentPracticeBattle.Raiders[0].Kind=="Tank" && GameObject.Find("Tank Shield")!=null,"Tank ground/lane/model");Capture("tank-deployment.png");game.SurrenderPracticeBattle();game.WatchPracticeReplay();SessionState.SetInt("TankValidation.Step",2);return;
            }
            Check(game.PracticeReplayMatches && game.CurrentPracticeBattle.TankBudget==4,"Tank UI replay match");game.ClosePracticeBattle();game.OpenCampaign();Click("Resolve Campaign");Check(!game.State.HasCampaignRun,"Tank result dismissed");
            Finish("PASS: Barracks upgrade/cancellation/completion and level cap; level-2 Tank unlock; four-space Tank housing and atomic overflow; Tank roster save/reload; locked-save rejection; 300 HP, slower movement, defense priority, melee range/damage/cooldown; independent typed budgets; both three-type wins and every-tick ground/lane replay equality; snapshot reload, unlock and composition rejection; campaign commitment and once-only reward; v6 pending reward migration/backup/claim; Tank-only fill/remove/add UI; ground/lane deployment and shield model; rendered replay and result dismissal. Unity "+Application.unityVersion+". No phone testing or APK rebuild.",0);
        }catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Finish(string message,int code){SessionState.SetBool(Pending,false);File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),"TankValidation.txt"),message);EditorApplication.Exit(code);}
}
