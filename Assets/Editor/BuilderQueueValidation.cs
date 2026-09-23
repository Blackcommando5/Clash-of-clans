using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Kingdoms;
using Kingdoms.UI;

[InitializeOnLoad]
public static class BuilderQueueValidation
{
    const string Pending="Builders.Validation.Pending";
    static string Root => Path.GetDirectoryName(Application.dataPath);
    static BuilderQueueValidation(){EditorApplication.update+=Tick;}
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static Button Button(string name)=>GameObject.Find(name).GetComponent<Button>();
    static void Click(string name)=>Button(name).onClick.Invoke();

    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').EndsWith("/.utmp/ResourceValidation/Assets") ||
            PlayerSettings.companyName!="KingdomsResourceValidation" || PlayerSettings.productName!="ResourceMilestoneTests")
            throw new InvalidOperationException("Use the isolated ResourceValidation project and test save identity.");
        var state=VillageState.Create(VillageState.Now);state.elixir=10000;
        Check(state.TryPlace("GoldMine",6,6,VillageState.Now,out _),"Seed mine");
        Check(state.TryStartUpgrade(0,VillageState.Now,out _) && state.TryStartUpgrade(1,VillageState.Now,out _),"Seed two jobs");
        Check(VillageSave.TryWrite(state,out _) && PlayerProfile.TrySaveName("BuilderChief",out _),"Seed isolated save");
        EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");
        SessionState.SetInt("Builders.Step",0);
        SessionState.SetString("Builders.Start",DateTime.UtcNow.Ticks.ToString());
        SessionState.SetBool(Pending,true);
        EditorApplication.EnterPlaymode();
    }

    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false))return;
        try
        {
            Check((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString("Builders.Start","0")))).TotalSeconds<120,"Play validation timeout");
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();
            if(game==null || game.State==null || GameObject.Find("Builders")==null)return;
            int step=SessionState.GetInt("Builders.Step",0);
            if(step==0)
            {
                Click("Builders");
                Check(game.DetailsOpen && game.cameraController.InputBlocked,"Queue camera lock");
                Check(Button("Builder Job 0").GetComponentInChildren<Text>().text.Contains("Gold Mine") && Button("Builder Job 1").GetComponentInChildren<Text>().text.Contains("Town Hall"),"Both jobs sorted by completion");
                Capture("builder-queue.png");
                Click("Builder Job 1");Check(game.SelectedBuildingIndex==0 && game.DetailsOpen,"Second job opens Town Hall");
                Click("Close Details");Click("Builders");Click("Builder Job 0");
                Check(game.SelectedBuildingIndex==1,"First job opens mine");
                int balance=game.State.elixir;
                Click("Confirm Upgrade");
                Check(game.State.BusyBuilders==2 && game.State.elixir==balance,"Cancellation first click changes no economy");
                Check(Button("Confirm Upgrade").GetComponentInChildren<Text>().text=="CONFIRM CANCELLATION","Confirmation caption");
                Capture("upgrade-cancellation.png");
                Click("Close Details");
                Check(game.State.BusyBuilders==2 && !game.cameraController.InputBlocked,"Closing confirmation keeps job and unlocks camera");
                Click("Builders");Click("Builder Job 0");
                Check(Button("Confirm Upgrade").GetComponentInChildren<Text>().text=="CANCEL UPGRADE","Reopening resets confirmation");
                Click("Confirm Upgrade");Click("Confirm Upgrade");
                Check(game.State.BusyBuilders==1 && game.State.elixir==balance+150 && !game.DetailsOpen,"Confirmed cancellation refunds and closes");
                game.StartSelectedUpgrade();
                Check(game.State.BusyBuilders==1 && game.State.elixir==balance+150,"Stale hidden action cannot restart upgrade");
                Click("Builders");
                Check(Button("Builder Job 0").GetComponentInChildren<Text>().text.Contains("Town Hall") && !Button("Builder Job 1").interactable,"Queue refresh after cancellation");
                Click("Close Details");
                SessionState.SetInt("Builders.Step",1);SceneManager.LoadScene("Main Scene");return;
            }
            Check(game.State.BusyBuilders==1 && game.State.buildings[1].upgradeFinishes==0,"Cancellation survives scene reload");
            Click("Builders");
            game.State.Accrue(game.State.buildings[0].upgradeFinishes);
            game.SendMessage("RefreshHUD");
            Check(!Button("Builder Job 0").interactable && !Button("Builder Job 1").interactable,"Completed jobs become available rows live");
            Check(GameObject.Find("Details Title").GetComponent<Text>().text.Contains("2 / 2 FREE"),"Empty queue availability");
            Click("Close Details");Check(!game.cameraController.InputBlocked,"Queue close restores camera");
            Finish("PASS: Play mode builder queue; both job callbacks; finish ordering; cancellation confirmation/back-out/refund; hidden action guard; available rows; completion refresh; camera locks; scene reload. Unity "+Application.unityVersion+". Physical phone not tested.",0);
        }
        catch(Exception e){Debug.LogException(e);Finish("FAIL: "+e,1);}
    }
    static void Capture(string name)
    {
        typeof(ResourceMilestoneValidation).GetMethod("Capture",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{name,1600,900});
    }
    static void Finish(string result,int code)
    {
        SessionState.SetBool(Pending,false);File.WriteAllText(Path.Combine(Root,"BuilderQueueValidation.txt"),result);EditorApplication.Exit(code);
    }
}
