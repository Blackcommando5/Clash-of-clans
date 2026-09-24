using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Kingdoms;
using Kingdoms.UI;

[InitializeOnLoad]
public static class MixedArmyValidation
{
    const string Pending="MixedArmy.Pending";
    static MixedArmyValidation(){EditorApplication.update+=Tick;}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static Button Button(string name)=>GameObject.Find(name).GetComponent<Button>();
    static void Click(string name)=>Button(name).onClick.Invoke();
    static VillageState Seed()
    {
        var s=VillageState.Create(VillageState.Now);s.elixir=10000;
        Check(s.TryPlace("Barracks",5,5,s.lastProduction,out _) && s.TryPlace("ArmyCamp",9,5,s.lastProduction,out _),"Facility setup");return s;
    }
    static PracticeBattle MixedBattle(bool sealedWalls)
    {
        var b=new PracticeBattle(sealedWalls,12,4);var hashes=new List<ulong>();
        for(int i=0;i<4;i++)b.Deploy(i%3);
        for(int i=0;i<2;i++)b.Deploy(i,"Archer");hashes.Add(b.StateHash());
        while(b.Outcome==PracticeOutcome.Running)
        {
            b.Step();
            if(b.Tick==5)for(int i=0;i<4;i++)b.Deploy(i%3);
            if(b.Tick==10)for(int i=0;i<2;i++)b.Deploy(i,"Archer");
            hashes.Add(b.StateHash());
        }
        Check(b.Outcome==PracticeOutcome.Victory,"Mixed army wins "+sealedWalls);
        var replay=new PracticeReplay(b.Record());Check(replay.Battle.StateHash()==hashes[0],"Typed tick-zero commands");
        while(!replay.Finished){replay.Step();Check(replay.Battle.StateHash()==hashes[replay.Battle.Tick],"Mixed replay per-tick equality");}
        Check(replay.Matches && replay.Battle.ArcherBudget==4 && replay.Battle.RaiderBudget==8,"Mixed replay retains composition");return b;
    }
    static void Rules()
    {
        var locked=VillageState.Create(1000);Check(!locked.TrySetArmyCount("Archer",1,out _) && locked.ArmyHousing==0,"Archers require Barracks");
        locked.army.Add(new ArmyStack{troop="Archer",count=1});Check(!locked.IsValid(),"Locked troop save rejected");
        var s=Seed();Check(s.TrySetArmyCount("Raider",8,out _) && s.TrySetArmyCount("Archer",4,out _) && s.ArmyHousing==16 && s.ArmyCount==12,"Weighted mixed housing");
        string before=JsonUtility.ToJson(s);Check(!s.TrySetArmyCount("Archer",5,out _) && JsonUtility.ToJson(s)==before,"Mixed capacity rejection atomic");
        Check(VillageState.TryDeserialize(before,out s) && s.ArmyCountOf("Archer")==4,"Mixed roster persistence");
        Check(s.TrySetArmyCount("Raider",0,out _) && s.ArmyCountOf("Archer")==4,"Remove selected troop preserves other type");
        s.TrySetArmyCount("Raider",8,out _);
        Check(s.TryBeginCampaign(0,out _) && s.campaignArmy==12 && s.campaignArchers==4 && s.ArmyHousing==0,"Commit composition and clear roster");
        var invalid=s.Copy();invalid.buildings.RemoveAt(1);Check(!invalid.IsValid(),"Committed Archers require Barracks");
        invalid=s.Copy();invalid.campaignArmy=80;Check(!invalid.IsValid(),"Committed roster respects village housing");
        string run=s.campaignRunId;
        var wrong=new PracticeBattle(false,12,3);wrong.Surrender();Check(!s.TryFinishCampaign(run,wrong.Record(),out _),"Same-sized different composition rejected");
        Check(s.TryFinishCampaign(run,MixedBattle(false).Record(),out _) && s.TryClaimCampaign(run,out _),"Mixed victory verified and rewarded");MixedBattle(true);
        var cap=new PracticeBattle(false,3,1);Check(!cap.Deploy(0,"Unknown") && !cap.Deploy(-1,"Archer"),"Typed invalid commands");
        Check(cap.Deploy(0,"Archer") && !cap.Deploy(0,"Archer") && cap.Deploy(0) && cap.Deploy(1) && !cap.Deploy(2),"Independent troop budgets");
        var range=new PracticeBattle(false,1,1);range.Buildings[1].HitPoints=range.Buildings[2].HitPoints=0;
        range.Deploy(0,"Archer");range.Raiders[0].X=0;range.Raiders[0].Z=-175;range.Step();
        Check(range.Buildings[0].HitPoints==586 && range.Raiders[0].Z==-175 && range.Raiders[0].HitPoints==55,"Archer damage and stand-off range");
        range.Step();Check(range.Buildings[0].HitPoints==586,"Archer cooldown");
        var melee=new PracticeBattle(false,1);melee.Buildings[1].HitPoints=melee.Buildings[2].HitPoints=0;melee.Deploy(0);melee.Raiders[0].X=0;melee.Raiders[0].Z=-175;melee.Step();
        Check(melee.Buildings[0].HitPoints==600,"Raider cannot attack at Archer range");
        var wall=new PracticeBattle(true,1,1);wall.Buildings[1].HitPoints=wall.Buildings[2].HitPoints=0;wall.Deploy(0,"Archer");wall.Raiders[0].X=0;wall.Raiders[0].Z=-175;wall.Step();
        Check(wall.Buildings[0].HitPoints==600,"Walls block Archer shots at the hall");
        bool wallHit=false;foreach(var e in wall.Buildings)if(e.Kind=="Wall" && e.HitPoints<e.MaxHitPoints)wallHit=true;Check(wallHit,"Archer targets blocking walls");
        foreach(int archers in new[]{-1,2}){bool threw=false;try{new PracticeBattle(false,1,archers);}catch(ArgumentOutOfRangeException){threw=true;}Check(threw,"Invalid composition constructor");}
        bool over=false;try{new PracticeBattle(false,41,41);}catch(ArgumentOutOfRangeException){over=true;}Check(over,"Combat housing capped at 80");
        var legacy=VillageState.Create(1000);legacy.TrySetArmyCount("Raider",8,out _);legacy.TryBeginCampaign(0,out _);string id=legacy.campaignRunId;
        var victory=new PracticeBattle();for(int i=0;i<8;i++)victory.Deploy(i%3);while(victory.Outcome==PracticeOutcome.Running)victory.Step();
        Check(legacy.TryFinishCampaign(id,victory.Record(),out _),"Seed old pending reward");
        string json=JsonUtility.ToJson(legacy).Replace("\"version\":6","\"version\":5");
        Check(VillageState.TryDeserialize(json,out var migrated) && migrated.version==6 && migrated.campaignRunId==id && migrated.campaignArmy==8 && migrated.campaignArchers==0 && migrated.campaignOutcome==PracticeOutcome.Victory,"v5 pending victory preserved");
        Check(migrated.TryClaimCampaign(id,out _) && !migrated.TryClaimCampaign(id,out _),"Migrated reward once only");
        PlayerPrefs.DeleteKey("Kingdoms.Village.pre-v6");PlayerPrefs.SetString(VillageSave.Key,json);
        Check(VillageSave.TryLoad(out _,out _) && PlayerPrefs.GetString("Kingdoms.Village.pre-v6")==json,"v5 backup preserved");
    }
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').EndsWith("/.utmp/ResourceValidation/Assets") || PlayerSettings.companyName!="KingdomsResourceValidation" || PlayerSettings.productName!="ResourceMilestoneTests")throw new InvalidOperationException("Use isolated validation project and identity.");
        try
        {
            Rules();EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");Check(VillageSave.TryWrite(Seed(),out _) && PlayerProfile.TrySaveName("ArcherChief",out _),"UI seed");
            SessionState.SetInt("MixedArmy.Step",0);SessionState.SetString("MixedArmy.Start",DateTime.UtcNow.Ticks.ToString());SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        }
        catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Capture(string name)=>typeof(ResourceMilestoneValidation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{name,1600,702});
    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false))return;
        try
        {
            Check((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString("MixedArmy.Start","0")))).TotalSeconds<180,"Play timeout");
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();if(game==null || game.State==null)return;
            int step=SessionState.GetInt("MixedArmy.Step",0);
            if(step==0)
            {
                game.OpenArmyPreparation();for(int i=0;i<8;i++)Click("Add Raider");Click("Prepare Archers");Click("Fill Army");
                Check(game.State.ArmyCountOf("Raider")==8 && game.State.ArmyCountOf("Archer")==4 && !Button("Add Raider").interactable,"Mixed preparation controls");Capture("mixed-army-preparation.png");
                Click("Remove Raider");Check(game.State.ArmyCountOf("Archer")==3 && game.State.ArmyCountOf("Raider")==8,"Selected Archer removal");Click("Add Raider");
                SessionState.SetInt("MixedArmy.Step",1);SceneManager.LoadScene("Main Scene");return;
            }
            if(step==1)
            {
                Check(game.State.ArmyCountOf("Archer")==4 && game.State.ArmyCountOf("Raider")==8,"Mixed UI save survives reload");game.OpenCampaign();Click("Campaign Gate");Click("Begin Practice Attack");
                Check(game.State.campaignArmy==12 && game.State.campaignArchers==4,"UI commits composition");
                for(int i=0;i<8;i++)Click("Deploy Raider "+i%3);
                Check(!Button("Deploy Raider 0").interactable && Button("Select Battle Archers").interactable,"Exhausted selected type does not consume other type");
                Click("Select Battle Archers");for(int i=0;i<4;i++)Click("Deploy Raider "+i%3);
                Check(game.CurrentPracticeBattle.Raiders.Count==12 && game.CurrentPracticeBattle.Remaining==0 && GameObject.Find("Archer Bow")!=null,"Archer model and typed deployment");
                SessionState.SetString("MixedArmy.CombatStart",DateTime.UtcNow.Ticks.ToString());SessionState.SetInt("MixedArmy.Step",2);return;
            }
            if(step==2)
            {
                if((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString("MixedArmy.CombatStart","0")))).TotalSeconds<2)return;
                Capture("mixed-army-combat.png");for(int i=0;i<1800 && game.CurrentPracticeBattle.Outcome==PracticeOutcome.Running;i++)game.CurrentPracticeBattle.Step();
                SessionState.SetInt("MixedArmy.Step",3);return;
            }
            if(step==3)
            {
                if(game.State.campaignOutcome==PracticeOutcome.Running)return;
                Check(game.State.campaignOutcome==PracticeOutcome.Victory,"Mixed UI victory recorded");Click("Watch Practice Replay");SessionState.SetInt("MixedArmy.Step",4);return;
            }
            if(step==4)
            {
                if(game.CurrentPracticeBattle.Outcome==PracticeOutcome.Running)return;
                Check(game.PracticeReplayMatches && game.CurrentPracticeBattle.ArcherBudget==4,"Mixed rendered replay matches");Click("Return From Practice");game.OpenCampaign();Click("Resolve Campaign");
                Check(game.State.CampaignCleared(0),"Mixed reward claim");game.OpenArmyPreparation();Click("Prepare Archers");Click("Fill Army");Check(game.State.ArmyCountOf("Archer")==8 && game.State.ArmyHousing==16,"Archer-only fill");Click("Open Campaign");Click("Campaign Gate");Click("Begin Practice Attack");Click("Deploy Raider 0");
                Check(game.CurrentPracticeBattle.Raiders[0].Kind=="Archer" && game.CurrentPracticeBattle.RaiderBudget==0,"Archer-only battle selects Archers automatically");
                Click("Surrender Practice");Click("Claim Campaign Battle");Click("Return From Practice");
                game.OpenArmyPreparation();Click("Prepare Raiders");Click("Add Raider");Click("Prepare Archers");Click("Fill Army");
                Check(game.State.ArmyHousing==15 && game.State.ArmyCountOf("Archer")==7 && !Button("Add Raider").interactable,"One spare space cannot fit another Archer");
                Click("Prepare Raiders");Click("Add Raider");Check(game.State.ArmyHousing==16,"Raider can use remaining single space");
                Click("Clear Army");Check(game.State.ArmyCount==0,"Clear removes entire roster");
                Finish("PASS: Barracks unlock; weighted mixed housing and atomic overflow rejection; independent troop counts and budgets; ranged damage, cooldown, stand-off and wall line-of-sight; both mixed campaign wins; per-tick typed replays; composition mismatch rejection; 80-space bound; v5 pending reward migration/backup/once-only claim; mixed preparation/remove/fill/clear UI; save reload; typed deployment, Archer model, campaign result/replay/claim; Archer-only fill/deployment; odd-space fill and troop-specific button states; committed snapshot unlock/capacity checks. Unity "+Application.unityVersion+". No tank, ground deployment, phone validation or APK.",0);
            }
        }
        catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Finish(string message,int code)
    {SessionState.SetBool(Pending,false);File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),"MixedArmyValidation.txt"),message);EditorApplication.Exit(code);}
}
