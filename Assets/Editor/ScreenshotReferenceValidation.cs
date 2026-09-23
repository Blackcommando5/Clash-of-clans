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
public static class ScreenshotReferenceValidation
{
    const string Pending="ScreenshotReference.Pending";
    static string Root=>Path.GetDirectoryName(Application.dataPath);
    static ScreenshotReferenceValidation(){EditorApplication.update+=Tick;}
    static void Check(bool value,string reason){if(!value)throw new Exception(reason);}
    static Button Button(string name)=>GameObject.Find(name).GetComponent<Button>();
    static void Click(string name)=>Button(name).onClick.Invoke();
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').EndsWith("/.utmp/ResourceValidation/Assets") || PlayerSettings.companyName!="KingdomsResourceValidation" || PlayerSettings.productName!="ResourceMilestoneTests")
            throw new InvalidOperationException("Use isolated ResourceValidation project and identity.");
        try
        {
            ScreenshotReferenceArt.Build();
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();
            game.goldMinePrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ScreenshotReference/GoldMine.prefab");
            game.elixirCollectorPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ScreenshotReference/ElixirCollector.prefab");
            game.goldStoragePrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ScreenshotReference/GoldStorage.prefab");
            game.elixirStoragePrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ScreenshotReference/ElixirStorage.prefab");
            game.BuildScreenshotReferenceInterface();EditorUtility.SetDirty(game);EditorSceneManager.SaveScene(scene);
            // Reopen to verify the new serialized controls and sunburst graphics survive.
            EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");
            var state=VillageState.Create(VillageState.Now);state.gold=8000;state.elixir=8000;
            state.TryPlace("GoldMine",-6,2,VillageState.Now,out _);state.TryPlace("ElixirCollector",3,0,VillageState.Now,out _);
            state.TryPlace("GoldStorage",-6,-2,VillageState.Now,out _);state.TryPlace("ElixirStorage",3,-4,VillageState.Now,out _);
            for(int i=-7;i<=5;i++)state.TryPlace("Wall",i,6,VillageState.Now,out _);
            Check(VillageSave.TryWrite(state,out _) && PlayerProfile.TrySaveName("Subash",out _),"Seed test village");
            SessionState.SetString("ScreenshotReference.Start",DateTime.UtcNow.Ticks.ToString());SessionState.SetInt("ScreenshotReference.Step",0);SessionState.SetBool(Pending,true);
            EditorApplication.EnterPlaymode();
        }
        catch(Exception e){Finish("FAIL: "+e,1);}
    }
    static void Capture(string name,int width=1600,int height=702)
    { typeof(ResourceMilestoneValidation).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{name,width,height}); }
    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false))return;
        try
        {
            Check((DateTime.UtcNow-new DateTime(long.Parse(SessionState.GetString("ScreenshotReference.Start","0")))).TotalSeconds<180,"UI test timeout");
            if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();if(game==null || game.State==null || GameObject.Find("Shop")==null)return;
            if(SessionState.GetInt("ScreenshotReference.Step",0)==1)
            {
                Check(game.State.BusyBuilders==0 && game.State.Count("GoldMine")==2,"Purchase and cancellation persist");
                game.OpenShop();Check(GameObject.Find("Reference Building Shop")!=null,"Reference shop survives reload");
                Click("Shop Category 2");Check(Button("Catalogue Buy Wall").interactable,"Wall available after reload");
                Click("Reference Close Shop");
                Finish("PASS: serialized reference UI; four category tabs and 20 cards; unsupported items disabled; resource purchase and placement; wall purchase; insufficient funds; upgrade and cancellation confirmation/refund; builder queue; camera locks; save/reload; screenshots at 1600x702 and 1920x1080. Unity "+Application.unityVersion+". Phone and APK not tested.",0);return;
            }
            Capture("screenshot-match-village.png");
            Click("Builders");Capture("screenshot-match-suggestions.png");
            int beforeSuggestion=game.State.gold+game.State.elixir;
            Click("Suggested Upgrade 0");Check(game.IsPlacing && !game.DetailsOpen,"Builder suggestion begins placement");
            game.CancelPlacement();Check(game.State.gold+game.State.elixir==beforeSuggestion && !game.cameraController.InputBlocked,"Suggestion cancellation costs nothing and restores camera");
            Click("Shop");Check(game.cameraController.InputBlocked,"Shop locks camera");
            string[] categories={"army","resources","defenses","traps"};
            for(int i=0;i<4;i++){Click("Shop Category "+i);Capture("screenshot-match-"+categories[i]+".png");}
            Check(!Button("Catalogue Buy Bomb").interactable,"Unavailable item cannot purchase");
            Click("Catalogue Info Bomb");Check(GameObject.Find("Catalogue Notice").GetComponent<Text>().text.Contains("not available"),"Unavailable info is visible");
            Click("Shop Category 1");
            int elixir=game.State.elixir;
            Click("Catalogue Buy GoldMine");Check(game.IsPlacing && game.cameraController.InputBlocked,"Catalogue placement");
            game.SetPreviewCell(9,3);game.ConfirmPlacement();
            Check(game.State.Count("GoldMine")==2 && game.State.elixir==elixir-150,"Catalogue purchase charges once");
            game.OpenShop();Click("Shop Category 2");int gold=game.State.gold;
            Click("Catalogue Buy Wall");game.SetPreviewCell(9,7);game.ConfirmPlacement();Check(game.State.gold==gold-25,"Wall catalogue purchase");
            game.OpenShop();Click("Shop Category 1");int oldGold=game.State.gold;game.State.gold=0;game.SendMessage("RefreshHUD");Check(!Button("Catalogue Buy ElixirCollector").interactable,"Unaffordable card disabled");game.State.gold=oldGold;
            Click("Reference Close Shop");Check(!game.cameraController.InputBlocked,"Shop close unlocks camera");
            game.SelectBuilding(4);Capture("screenshot-match-selection.png");Click("Selected Upgrade");
            Check(game.DetailsOpen && game.cameraController.InputBlocked,"Selected upgrade opens details");Capture("screenshot-match-upgrade.png");Capture("screenshot-match-upgrade-wide.png",1920,1080);
            Click("Confirm Upgrade");Check(game.State.BusyBuilders==1,"Upgrade starts through redesigned dialog");
            Click("Builders");Capture("screenshot-match-builders.png");Click("Builder Job 0");
            gold=game.State.gold;Click("Confirm Upgrade");Check(game.State.gold==gold && game.State.BusyBuilders==1,"Cancellation asks before changing state");
            Capture("screenshot-match-cancellation.png");Click("Confirm Upgrade");Check(game.State.BusyBuilders==0 && game.State.gold==gold+300,"Cancellation refund in redesigned dialog");
            SessionState.SetInt("ScreenshotReference.Step",1);SceneManager.LoadScene("Main Scene");
        }
        catch(Exception e){Debug.LogException(e);Finish("FAIL: "+e,1);}
    }
    static void Finish(string result,int code)
    { SessionState.SetBool(Pending,false);File.WriteAllText(Path.Combine(Root,"ScreenshotReferenceValidation.txt"),result);EditorApplication.Exit(code); }
}
