using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Kingdoms;
using Kingdoms.UI;

[InitializeOnLoad]
public static class ArmyPreparationValidation
{
    const string Pending = "ArmyValidation.Pending";
    static ArmyPreparationValidation() { EditorApplication.update += Tick; }
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Run()
    {
        if (!Application.dataPath.Replace('\\','/').EndsWith("/.utmp/ResourceValidation/Assets") ||
            PlayerSettings.companyName != "KingdomsResourceValidation" || PlayerSettings.productName != "ResourceMilestoneTests")
            throw new InvalidOperationException("Use the isolated validation project and identity.");
        try
        {
            var state = VillageState.Create(VillageState.Now);
            Check(state.IsValid() && !state.ArmyReady && state.ArmyHousing == 0, "Fresh army is empty");
            Check(state.TrySetArmyCount("Raider", 8, out _) && state.ArmyReady && state.ArmyHousing == 8, "Full roster ready");
            string full = JsonUtility.ToJson(state);
            foreach (int count in new[] { -1, 9, int.MaxValue })
                Check(!state.TrySetArmyCount("Raider", count, out _) && JsonUtility.ToJson(state) == full, "Invalid count is atomic");
            Check(!state.TrySetArmyCount("Unknown", 1, out _), "Unknown troop rejected");
            Check(VillageState.TryDeserialize(full, out var restored) && restored.ArmyHousing == 8, "Roster round trip");
            restored.army.Add(new ArmyStack { troop = "Raider", count = 1 });
            Check(!restored.IsValid() && !VillageState.TryDeserialize(JsonUtility.ToJson(restored), out _), "Duplicate stack rejected");
            foreach (int count in new[] { -1, 0, 9, int.MaxValue })
            {
                restored = state.Copy(); restored.army[0].count = count;
                Check(!VillageState.TryDeserialize(JsonUtility.ToJson(restored), out _), "Malformed saved count rejected");
            }
            restored = state.Copy(); restored.army[0].troop = "Unknown";
            Check(!restored.IsValid(), "Unknown saved troop rejected");
            restored = state.Copy(); restored.army = null; Check(!restored.IsValid(), "Null roster rejected");
            restored = state.Copy(); restored.army.Add(null); Check(!restored.IsValid(), "Null stack rejected");
            Check(state.TrySetArmyCount("Raider", 0, out _) && !state.ArmyReady && state.army.Count == 0, "Clear removes stack");
            for (int version = 1; version <= 3; version++)
            {
                string legacy = JsonUtility.ToJson(state).Replace("\"version\":8", "\"version\":" + version).Replace(",\"army\":[]", "");
                Check(VillageState.TryDeserialize(legacy, out var migrated) && migrated.version == 8 && migrated.IsValid() && !migrated.ArmyReady && migrated.gold == state.gold, "Legacy migration " + version);
            }
            string v3 = JsonUtility.ToJson(state).Replace("\"version\":8", "\"version\":3");
            PlayerPrefs.DeleteKey("Kingdoms.Village.pre-v4");
            PlayerPrefs.SetString(VillageSave.Key, v3);
            Check(VillageSave.TryLoad(out var loaded, out _) && loaded.version == 8 && PlayerPrefs.GetString("Kingdoms.Village.pre-v4") == v3, "Original save backed up");
            Check(VillageSave.TryWrite(state, out _) && PlayerProfile.TrySaveName("ArmyChief", out _), "Set isolated save");
            EditorSceneManager.OpenScene("Assets/Scenes/Main Scene.unity");
            SessionState.SetBool(Pending, true);
            SessionState.SetString("ArmyValidation.Start", DateTime.UtcNow.Ticks.ToString());
            EditorApplication.EnterPlaymode();
        }
        catch (Exception e) { Finish("FAIL: " + e, 1); }
    }
    static void Click(string name) => GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
    static void Tick()
    {
        if (!SessionState.GetBool(Pending, false)) return;
        try
        {
            if ((DateTime.UtcNow - new DateTime(long.Parse(SessionState.GetString("ArmyValidation.Start", "0")))).TotalSeconds > 90)
                throw new Exception("Play mode timed out");
            if (!EditorApplication.isPlaying) return;
            var game = UnityEngine.Object.FindFirstObjectByType<VillageGameplay>();
            if (game == null || game.State == null) return;
            game.OpenProfile(4); Click("Prepare Army");
            Check(GameObject.Find("Army Summary").GetComponent<Text>().text.Contains("EMPTY"), "Empty UI");
            int gold = game.State.gold, elixir = game.State.elixir;
            Click("Add Raider"); Check(game.State.ArmyHousing == 1, "Add button");
            Click("Fill Army"); Check(game.State.ArmyHousing == 8 && !GameObject.Find("Add Raider").GetComponent<Button>().interactable, "Full UI");
            Check(VillageSave.TryLoad(out var loaded, out _) && loaded.ArmyHousing == 8, "UI saves roster");
            Check(game.State.gold == gold && game.State.elixir == elixir, "Preparation is free");
            Click("Remove Raider"); Check(game.State.ArmyHousing == 7, "Remove button");
            Click("Clear Army"); Check(!game.State.ArmyReady && !GameObject.Find("Remove Raider").GetComponent<Button>().interactable, "Clear UI");
            Click("Fill Army"); game.CloseProfile(); game.OpenArmyPreparation();
            Check(GameObject.Find("Army Summary").GetComponent<Text>().text.Contains("READY"), "Reopen readiness");
            string before = JsonUtility.ToJson(game.State);
            game.CloseProfile(); game.OpenPracticeBattle(); game.BeginPracticeAttack(); game.DeployPracticeRaider(1); game.SurrenderPracticeBattle(); game.ClosePracticeBattle();
            Check(JsonUtility.ToJson(game.State) == before, "Practice preserves owned roster");
            Finish("PASS: empty/full readiness; atomic invalid counts and overflow; unknown/duplicate/null stacks; saved-roster validation; v1/v2/v3 migration and v3 backup; JSON round trip; real scene Buildings > Prepare Army; add/fill/remove/clear; capacity button states; immediate save reload; free preparation; reopen; practice roster isolation. Unity " + Application.unityVersion + ". No APK or phone testing.", 0);
        }
        catch (Exception e) { Finish("FAIL: " + e, 1); }
    }
    static void Finish(string message, int code)
    {
        SessionState.SetBool(Pending, false);
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "ArmyPreparationValidation.txt"), message);
        EditorApplication.Exit(code);
    }
}
