using UnityEngine;
using UnityEngine.UI;

namespace Kingdoms.UI
{
    public sealed partial class VillageGameplay
    {
        Text armySummary, armyRules;
        Button armyAdd, armyRemove, armyFill, armyClear, armyRaiders, armyArchers, armyTanks, armyLast;
        string preparedTroop="Raider";
        int armyPageIndex;

        void BuildArmyPreparation()
        {
            var inventoryShop = profilePages[4].Find("Inventory Shop").GetComponent<RectTransform>();
            inventoryShop.anchorMin = new Vector2(.08f, .04f);
            inventoryShop.anchorMax = new Vector2(.47f, .19f);
            var entry = Button("Prepare Army", profilePages[4], "PREPARE ARMY", new Vector2(.53f,.04f), new Vector2(.92f,.19f), new Color(.28f,.55f,.76f));
            entry.onClick.AddListener(OpenArmyPreparation);
            armyPageIndex = profilePages.Count;
            var page = Box("Army Preparation Page", profilePages[0].parent, Vector2.zero, Vector2.one);
            profilePages.Add(page);
            Label("Army Title", page, "Prepare your army", 40, new Color(.23f,.25f,.3f), new Vector2(.05f,.84f), new Vector2(.95f,.98f));
            armySummary = Label("Army Summary", page, "", 30, new Color(.23f,.25f,.3f), new Vector2(.06f,.44f), new Vector2(.94f,.72f));
            armyRules = Label("Army Rules", page, "", 23, new Color(.23f,.25f,.3f), new Vector2(.06f,.22f), new Vector2(.94f,.43f));
            armyRaiders=Button("Prepare Raiders",page,"RAIDERS",new Vector2(.05f,.73f),new Vector2(.33f,.83f),new Color(.28f,.55f,.76f));
            armyArchers=Button("Prepare Archers",page,"ARCHERS",new Vector2(.36f,.73f),new Vector2(.64f,.83f),new Color(.35f,.60f,.25f));
            armyTanks=Button("Prepare Tanks",page,"TANKS",new Vector2(.67f,.73f),new Vector2(.95f,.83f),new Color(.5f,.45f,.6f));
            armyTanks.onClick.AddListener(()=>SelectPreparedTroop("Tank"));
            armyRaiders.onClick.AddListener(()=>SelectPreparedTroop("Raider"));armyArchers.onClick.AddListener(()=>SelectPreparedTroop("Archer"));
            armyRemove = ArmyButton(page, "Remove Raider", "REMOVE 1", .04f, () => ChangePreparedArmy(State.ArmyCountOf(preparedTroop) - 1));
            armyAdd = ArmyButton(page, "Add Raider", "ADD RAIDER", .28f, () => ChangePreparedArmy(State.ArmyCountOf(preparedTroop) + 1));
            armyFill = ArmyButton(page, "Fill Army", "FILL ARMY", .52f, () => ChangePreparedArmy(State.ArmyCountOf(preparedTroop)+(State.ArmyCapacity-State.ArmyHousing)/TroopCatalog.Find(preparedTroop).Housing));
            armyClear = ArmyButton(page, "Clear Army", "CLEAR ARMY", .76f, () => ChangePreparedArmy(0,true));
            armyLast=Button("Prepare Last Army",page,"LAST ARMY",new Vector2(.48f,.85f),new Vector2(.71f,.98f),new Color(.35f,.60f,.25f));
            armyLast.onClick.AddListener(()=>PrepareArmyFromHistory(0));
            page.gameObject.SetActive(false);
        }

        Button ArmyButton(Transform page, string name, string caption, float left, UnityEngine.Events.UnityAction action)
        {
            var button = Button(name, page, caption, new Vector2(left,.06f), new Vector2(left+.20f,.21f), new Color(.35f,.60f,.25f));
            button.onClick.AddListener(action);
            return button;
        }

        public void OpenArmyPreparation()
        {
            if (State == null || PracticeOpen || IsPlacing) return;
            OpenProfile(armyPageIndex);
            RefreshArmyPreparation();
        }

        void SelectPreparedTroop(string troop)
        {if(State.TroopUnlocked(troop)){preparedTroop=troop;RefreshArmyPreparation();}}

        void ChangePreparedArmy(int count,bool clear=false)
        {
            if (!ProfileOpen || !profilePages[armyPageIndex].gameObject.activeSelf || PracticeOpen) return;
            var candidate = State.Copy();
            if(clear)candidate.army.Clear();
            if (!clear && !candidate.TrySetArmyCount(preparedTroop, count, out string reason))
            { armySummary.text = reason; return; }
            if (!VillageSave.TryWrite(candidate, out string error))
            { armySummary.text = error; return; }
            State = candidate;
            RefreshArmyPreparation();
        }

        void RefreshArmyPreparation()
        {
            armyLast.interactable=State.CanPrepareHistoryArmy(0,out var repeatReason);
            int count = State.ArmyHousing;
            armySummary.text = "Raiders: " + State.ArmyCountOf("Raider") + "   |   Archers: " + State.ArmyCountOf("Archer") + "   |   Tanks: " + State.ArmyCountOf("Tank") + "\nArmy space: " + count + " / " + State.ArmyCapacity +
                "\n" + (State.ArmyReady ? "READY - Your roster is prepared." : "EMPTY - Add troops to prepare your army.");
            var troop=TroopCatalog.Find(preparedTroop);
            armyAdd.GetComponentInChildren<Text>().text="ADD "+preparedTroop.ToUpperInvariant();
            armyFill.GetComponentInChildren<Text>().text="FILL "+preparedTroop.ToUpperInvariant()+"S";
            armyAdd.interactable = armyFill.interactable = State.ArmyCapacity-count>=troop.Housing;
            armyRemove.interactable=State.ArmyCountOf(preparedTroop)>0;armyClear.interactable=count>0;
            armyRaiders.GetComponentInChildren<Text>().text=preparedTroop=="Raider" ? "RAIDERS - SELECTED" : "RAIDERS";
            armyArchers.GetComponentInChildren<Text>().text=!State.TroopUnlocked("Archer") ? "ARCHERS - REQUIRES BARRACKS" : preparedTroop=="Archer" ? "ARCHERS - SELECTED" : "ARCHERS";
            armyArchers.interactable=State.TroopUnlocked("Archer");
            armyTanks.interactable=State.TroopUnlocked("Tank");
            armyTanks.GetComponentInChildren<Text>().text=!armyTanks.interactable ? "TANKS - BARRACKS LV 2" : preparedTroop=="Tank" ? "TANKS - SELECTED" : "TANKS";
            armyRules.text = troop.Name+": "+troop.Housing+" space(s), "+troop.HitPoints+" HP, "+troop.Damage+" damage/sec, "+(troop.Range/100f).ToString("0.##")+"-cell range."+
                (preparedTroop=="Tank" ? "\nPrefers defenses. Slower movement: 1.8 cells/sec." : "")+
                "\nFree, instant preparation. Fill adds the selected type; Clear removes all types."+
                "\nCampaign uses this roster. Practice supplies eight free Raiders."+
                "\n"+(armyLast.interactable ? "Last Army replaces this roster with your latest campaign army." : "Last Army: "+repeatReason);
        }

        public void PrepareArmyFromHistory(int index)
        {
            if(State==null || PracticeOpen || IsPlacing || !ProfileOpen)return;
            bool fromHistory=profilePages[historyPageIndex].gameObject.activeSelf;
            if(!fromHistory && !profilePages[armyPageIndex].gameObject.activeSelf)return;
            var candidate=State.Copy();
            if(!candidate.TryPrepareHistoryArmy(index,out var reason) || !VillageSave.TryWrite(candidate,out reason))
            {if(fromHistory)historyHelp.text=reason;else armySummary.text=reason;return;}
            State=candidate;
            preparedTroop=State.ArmyCountOf("Raider")>0 ? "Raider" : State.ArmyCountOf("Archer")>0 ? "Archer" : "Tank";
            OpenArmyPreparation();
        }
    }
}
