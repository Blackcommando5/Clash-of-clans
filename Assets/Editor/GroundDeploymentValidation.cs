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
public static class GroundDeploymentValidation
{
    const string Pending="GroundDeployment.Pending";
    static GroundDeploymentValidation(){EditorApplication.update+=Tick;}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Invoke(VillageGameplay game,string method,params object[] args)=>typeof(VillageGameplay).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,args);
    static void Rules()
    {
        var b=new PracticeBattle();ulong hash=b.StateHash();
        foreach(var p in new[]{new Vector2Int(-1001,-1000),new Vector2Int(1001,-1000),new Vector2Int(0,-1401),new Vector2Int(0,-799),new Vector2Int(int.MinValue,int.MaxValue)})
            Check(!b.DeployAt(p.x,p.y) && b.StateHash()==hash,"Invalid deployment atomic");
        Check(!b.DeployAt(0,-1000,"Unknown"),"Invalid troop");
        foreach(int x in new[]{-1000,1000})foreach(int z in new[]{-1400,-800})Check(b.DeployAt(x,z),"Inclusive corner");
        b.Buildings[0].X=0;b.Buildings[0].Z=-1000;Check(!b.DeployAt(0,-1000),"Building footprint rejected");
        b.Surrender();Check(!b.DeployAt(-500,-1000),"Terminal guard");
        foreach(bool sealedWalls in new[]{false,true})
        {
            b=new PracticeBattle(sealedWalls,12,4);var hashes=new List<ulong>();
            for(int i=0;i<8;i++)Check(b.DeployAt(-700+i*200,-1000),"Ground Raiders");
            hashes.Add(b.StateHash());
            while(b.Outcome==PracticeOutcome.Running)
            {
                b.Step();if(b.Tick==5)for(int i=0;i<4;i++)Check(b.DeployAt(-600+i*400,-1300,"Archer"),"Delayed ground Archers");
                hashes.Add(b.StateHash());
            }
            var record=b.Record();Check(record.Deployments[8].X==-600 && record.Deployments[8].Z==-1300 && record.Deployments[8].Tick==5,"Exact coordinates recorded");
            var replay=new PracticeReplay(record);Check(replay.Battle.StateHash()==hashes[0],"Initial replay");
            while(!replay.Finished){replay.Step();Check(replay.Battle.StateHash()==hashes[replay.Battle.Tick],"Every replay tick");}
            Check(replay.Matches,"Final replay match");
        }
        b=new PracticeBattle();b.Deploy(0);Check(b.Raiders[0].X==-685 && b.Raiders[0].Z==-1350,"Legacy lane positions preserved");
    }
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').EndsWith("/.utmp/ResourceValidation/Assets") || PlayerSettings.companyName!="KingdomsResourceValidation" || PlayerSettings.productName!="ResourceMilestoneTests")throw new InvalidOperationException("Use isolated validation project.");
        try{Rules();EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");VillageSave.TryWrite(VillageState.Create(VillageState.Now),out _);PlayerProfile.TrySaveName("GroundChief",out _);SessionState.SetInt("GroundDeployment.Step",0);SessionState.SetString("GroundDeployment.Start",DateTime.UtcNow.Ticks.ToString());SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();}
        catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false))return;
        try
        {
            Check((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString("GroundDeployment.Start","0")))).TotalSeconds<120,"Timeout");
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();if(game==null || game.State==null)return;
            if(SessionState.GetInt("GroundDeployment.Step",0)==0){game.OpenPracticeBattle();SessionState.SetInt("GroundDeployment.Step",1);return;}
            var camera=game.viewCamera;
            Vector2 point=camera.WorldToScreenPoint(new Vector3(0,0,-10.13f));
            Check(!game.TryDeployPracticeAtScreen(point),"Scouting guard");game.BeginPracticeAttack();Canvas.ForceUpdateCanvases();
            Check(GameObject.Find("Deployment Zone")!=null,"Visible deployment outline");
            Check(game.TryDeployPracticeAtScreen(point),"Screen to ground placement");
            Check(game.CurrentPracticeBattle.Raiders[0].X==0 && game.CurrentPracticeBattle.Raiders[0].Z==-1000,"Half-cell snap");
            int count=game.CurrentPracticeBattle.Raiders.Count;
            Invoke(game,"BeginGroundGesture",point);Invoke(game,"MoveGroundGesture",point+new Vector2(30,0));Invoke(game,"EndGroundGesture",point);
            Check(game.CurrentPracticeBattle.Raiders.Count==count,"Drag out and back rejected");
            Invoke(game,"BeginGroundGesture",point);Invoke(game,"CancelGroundGesture");Invoke(game,"EndGroundGesture",point);
            Check(game.CurrentPracticeBattle.Raiders.Count==count,"Canceled gesture rejected");
            var button=GameObject.Find("Deploy Raider 0").GetComponent<RectTransform>();Vector2 ui=RectTransformUtility.WorldToScreenPoint(null,button.TransformPoint(button.rect.center));
            Check(!game.TryDeployPracticeAtScreen(ui),"UI press ignored");
            Invoke(game,"BeginGroundGesture",ui);Invoke(game,"EndGroundGesture",point);Check(game.CurrentPracticeBattle.Raiders.Count==count,"UI-origin gesture rejected");
            Check(!game.TryDeployPracticeAtScreen(new Vector2(float.NaN,0)) && !game.TryDeployPracticeAtScreen(camera.WorldToScreenPoint(Vector3.zero)),"Invalid/outside input ignored");
            Invoke(game,"OnApplicationPause",true);Check(!game.TryDeployPracticeAtScreen(point),"Paused input ignored");Invoke(game,"OnApplicationPause",false);
            Invoke(game,"BeginGroundGesture",point);Invoke(game,"EndGroundGesture",point);Check(game.CurrentPracticeBattle.Raiders.Count==count+1,"Single tap deploys once");
            typeof(ResourceMilestoneValidation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{"ground-deployment.png",1600,702});
            game.SurrenderPracticeBattle();Check(!game.TryDeployPracticeAtScreen(point),"Terminal UI guard");game.WatchPracticeReplay();Check(!game.TryDeployPracticeAtScreen(point),"Replay input guard");game.ClosePracticeBattle();
            Finish("PASS: inclusive zone corners; invalid bounds/extreme coordinates/types rejected atomically; live footprint/terminal guards; exact delayed typed coordinates; both layouts replay identically every tick; legacy lane positions; rendered outline; screen projection and half-cell snap; UI/outside/NaN/pause/scouting/replay/end guards; drag-out-and-back cancellation; canceled/UI-origin gesture rejection; single tap deployment. Unity "+Application.unityVersion+". Gesture methods tested directly; physical multitouch and phone performance not validated. No APK rebuilt.",0);
        }
        catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Finish(string message,int code){SessionState.SetBool(Pending,false);File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),"GroundDeploymentValidation.txt"),message);EditorApplication.Exit(code);}
}
