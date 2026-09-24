using System;
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
public static class TutorialValidation
{
    const string Pending="Tutorial.Pending";
    static TutorialValidation(){EditorApplication.update+=Tick;}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Click(string name)=>GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
    static void Rules()
    {
        var s=VillageState.Create(1000);Check(s.TutorialStep==0 && s.TutorialProgress==0,"Fresh guide");
        Check(s.TryPlace("GoldMine",5,5,1000,out _) && s.TutorialStep==1,"Placed mine advances");
        s.Accrue(1005);Check(s.TutorialStep==1,"Production alone does not count as collection");Check(s.Collect(ResourceKind.Gold,1005)>0 && s.TutorialStep==2,"Collection evidence");
        Check(s.TrySetArmyCount("Raider",8,out _) && VillageSave.TryWrite(s,out _),"Save preparation milestone");s.TrySetArmyCount("Raider",0,out _);Check((s.TutorialProgress&32)!=0,"Preparation survives cleared roster");
        s.tutorialHintsPaused=true;Check(VillageSave.TryWrite(s,out _) && VillageSave.TryLoad(out var loaded,out _) && loaded.tutorialHintsPaused && loaded.TutorialStep==2,"Pause and partial progress reload");
        foreach(int invalid in new[]{-1,256,int.MaxValue}){var bad=s.Copy();bad.tutorialMilestones=invalid;Check(!bad.IsValid() && !VillageState.TryDeserialize(JsonUtility.ToJson(bad),out _),"Invalid milestone mask rejected");}
        s.tutorialMilestones=0;s.tutorialHintsPaused=false;string old=JsonUtility.ToJson(s).Replace("\"version\":9","\"version\":7");PlayerPrefs.DeleteKey("Kingdoms.Village.pre-v8");PlayerPrefs.SetString(VillageSave.Key,old);
        Check(VillageSave.TryLoad(out loaded,out _) && loaded.version==VillageState.SaveVersion && loaded.TutorialStep==2 && !loaded.tutorialHintsPaused && PlayerPrefs.GetString("Kingdoms.Village.pre-v8")==old,"v7 migration infers progress and keeps backup");
        loaded.gold=1000;loaded.elixir=1000;Check(loaded.TryStartUpgrade(1,loaded.lastProduction,out _) && (loaded.TutorialProgress&128)==0,"Started upgrade does not count");Check(loaded.TryCancelUpgrade(1,loaded.lastProduction,out _) && (loaded.TutorialProgress&128)==0,"Canceled upgrade does not count");
        Check(loaded.TryStartUpgrade(1,loaded.lastProduction,out _),"Restart upgrade");loaded.Accrue(loaded.buildings[1].upgradeFinishes);Check((loaded.TutorialProgress&128)!=0,"Completed upgrade counts");
        var pending=VillageState.Create(1000);pending.TrySetArmyCount("Raider",8,out _);pending.TryBeginCampaign(0,out _);Check((pending.TutorialProgress&32)!=0 && (pending.TutorialProgress&64)==0,"Committed roster counts; unclaimed battle does not");
        var win=new PracticeBattle();for(int i=0;i<8;i++)win.Deploy(i%3);while(win.Outcome==PracticeOutcome.Running)win.Step();Check(pending.TryFinishCampaign(pending.campaignRunId,win.Record(),out _) && (pending.TutorialProgress&64)==0,"Unclaimed victory does not complete tutorial attack");
        string ticket=pending.campaignRunId;string prior=JsonUtility.ToJson(pending).Replace("\"version\":9","\"version\":7");
        Check(VillageState.TryDeserialize(prior,out pending) && pending.campaignRunId==ticket && pending.campaignOutcome==PracticeOutcome.Victory && pending.campaignStars==3 && (pending.TutorialProgress&64)==0,"v7 pending reward preserved during tutorial migration");
        Check(pending.TryClaimCampaign(pending.campaignRunId,out _) && (pending.TutorialProgress&64)!=0,"Claimed Gate victory counts");
    }
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').EndsWith("/.utmp/ResourceValidation/Assets") || PlayerSettings.companyName!="KingdomsResourceValidation" || PlayerSettings.productName!="ResourceMilestoneTests")throw new InvalidOperationException("Use isolated project.");
        try{Rules();EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");Check(VillageSave.TryWrite(VillageState.Create(VillageState.Now),out _),"UI seed");PlayerProfile.TrySaveName("GuideChief",out _);SessionState.SetInt("Tutorial.Step",0);SessionState.SetString("Tutorial.Start",DateTime.UtcNow.Ticks.ToString());SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();}catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Capture(string name)=>typeof(ResourceMilestoneValidation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{name,1600,702});
    static void Guide(VillageGameplay game,int expected){game.OpenVillageGuide();Check(game.State.TutorialStep==expected,"Guide step "+expected);}
    static void Place(VillageGameplay game,string kind,int x){game.BeginPlacement(kind);Check(game.IsPlacing,"Guide placement "+kind);game.SetPreviewCell(x,-10);game.ConfirmPlacement();}
    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false))return;
        try
        {
            Check((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString("Tutorial.Start","0")))).TotalSeconds<180,"Timeout");if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();if(game==null || game.State==null)return;int step=SessionState.GetInt("Tutorial.Step",0);
            if(step==0)
            {
                Check(GameObject.Find("Village Guide")!=null,"Home guide entry visible");Capture("village-guide-home.png");Click("Village Guide");Check(game.State.TutorialStep==0,"Home entry opens guide");Capture("village-guide-start.png");Click("Toggle Guide Hints");Check(game.State.tutorialHintsPaused,"Pause hints UI");game.CloseProfile();typeof(VillageGameplay).GetMethod("RefreshHUD",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);Check(!game.State.TutorialComplete && GameObject.Find("Tutorial Guide")==null && GameObject.Find("Village Guide")!=null,"Paused hints hidden but guide accessible");SessionState.SetInt("Tutorial.Step",1);SceneManager.LoadScene("Main Scene");return;
            }
            if(step==1)
            {
                Check(game.State.tutorialHintsPaused && game.State.TutorialStep==0,"Pause survives scene reload");Guide(game,0);Click("Toggle Guide Hints");Click("Guide Next Action");Check(GameObject.Find("Catalogue Buy GoldMine")!=null,"Guide resource shop");Place(game,"GoldMine",-10);
                game.State.Accrue(game.State.lastProduction+10);Guide(game,1);Click("Guide Next Action");Check(game.State.collectedFirstGold,"Guide collect action");Guide(game,2);Click("Guide Next Action");Place(game,"ElixirCollector",-5);
                Guide(game,3);Click("Guide Next Action");Check(GameObject.Find("Catalogue Buy Barracks")!=null,"Guide army shop");Place(game,"Barracks",0);
                Guide(game,4);Check(VillageTutorial.Hint(game.State).Contains("collect elixir"),"Insufficient elixir guidance");game.CloseProfile();game.State.Accrue(game.State.lastProduction+200);game.Collect();Guide(game,4);Click("Guide Next Action");Place(game,"ArmyCamp",5);
                Guide(game,5);Click("Guide Next Action");Click("Fill Army");Check(game.State.ArmyHousing==16,"Guide free preparation");Guide(game,6);Capture("village-guide-attack.png");Click("Guide Next Action");Click("Campaign Gate");game.BeginPracticeAttack();for(int i=0;i<16;i++)game.DeployPracticeRaider(i%3);while(game.CurrentPracticeBattle.Outcome==PracticeOutcome.Running)game.CurrentPracticeBattle.Step();SessionState.SetInt("Tutorial.Step",2);return;
            }
            if(step==2)
            {
                if(game.State.campaignOutcome==PracticeOutcome.Running)return;Check(game.State.campaignOutcome==PracticeOutcome.Victory && game.State.TutorialStep==6,"Victory awaits claim");Click("Claim Campaign Battle");game.ClosePracticeBattle();Guide(game,7);Click("Guide Next Action");Click("Confirm Upgrade");Check(game.State.buildings[1].upgradeFinishes>0 && game.State.TutorialStep==7,"Guide starts upgrade without premature completion");
                game.State.Accrue(game.State.buildings[1].upgradeFinishes);Check(VillageSave.TryWrite(game.State,out _),"Save completed upgrade");SessionState.SetInt("Tutorial.Step",3);SceneManager.LoadScene("Main Scene");return;
            }
            Check(game.State.TutorialComplete && game.State.tutorialMilestones==255 && game.State.CampaignCleared(0),"Full loop completion survives reload");game.OpenVillageGuide();Capture("village-guide-complete.png");Click("Guide Next Action");Check(GameObject.Find("Campaign Gate")!=null,"Completed guide explores campaign");
            Finish("PASS: eight evidence-based steps; partial progress; production/collection distinction; sticky preparation after roster clear; pause/resume save and scene reload; v7 migration and backup; invalid masks; upgrades count only on completion; campaign counts only after claimed victory; actual fresh-village guide through resource shop, placement, collection, army facilities, free preparation, Gate attack/result/claim, upgrade and complete-state reload; completed guide remains accessible. Unity "+Application.unityVersion+". No phone testing or APK rebuild.",0);
        }catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Finish(string message,int code){SessionState.SetBool(Pending,false);File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),"TutorialValidation.txt"),message);EditorApplication.Exit(code);}
}
