using UnityEngine;
using UnityEngine.UI;

namespace Kingdoms.UI
{
    public sealed partial class VillageGameplay
    {
        Text armySummary, armyRules;
        Button armyAdd, armyRemove, armyFill, armyClear;
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
            armySummary = Label("Army Summary", page, "", 30, new Color(.23f,.25f,.3f), new Vector2(.06f,.50f), new Vector2(.94f,.83f));
            armyRules = Label("Army Rules", page, "", 23, new Color(.23f,.25f,.3f), new Vector2(.06f,.24f), new Vector2(.94f,.49f));
            armyRemove = ArmyButton(page, "Remove Raider", "REMOVE 1", .04f, () => ChangePreparedArmy(State.ArmyHousing - 1));
            armyAdd = ArmyButton(page, "Add Raider", "ADD RAIDER", .28f, () => ChangePreparedArmy(State.ArmyHousing + 1));
            armyFill = ArmyButton(page, "Fill Army", "FILL ARMY", .52f, () => ChangePreparedArmy(State.ArmyCapacity));
            armyClear = ArmyButton(page, "Clear Army", "CLEAR ARMY", .76f, () => ChangePreparedArmy(0));
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

        void ChangePreparedArmy(int count)
        {
            if (!ProfileOpen || !profilePages[armyPageIndex].gameObject.activeSelf || PracticeOpen) return;
            var candidate = State.Copy();
            if (!candidate.TrySetArmyCount(TroopCatalog.Raider.Id, count, out string reason))
            { armySummary.text = reason; return; }
            if (!VillageSave.TryWrite(candidate, out string error))
            { armySummary.text = error; return; }
            State = candidate;
            RefreshArmyPreparation();
        }

        void RefreshArmyPreparation()
        {
            int count = State.ArmyHousing;
            armySummary.text = "Raiders: " + count + "\nArmy space: " + count + " / " + State.ArmyCapacity +
                "\n" + (State.ArmyReady ? "READY - Your roster is prepared." : "EMPTY - Add a Raider to prepare your army.");
            armyAdd.interactable = armyFill.interactable = count < State.ArmyCapacity;
            armyRemove.interactable = armyClear.interactable = count > 0;
            armyRules.text = "Free, instant preparation. Each Raider uses 1 space.\n" +
                (State.Count("Barracks") == 0 ? "Shop > Army: build Barracks to unlock Army Camps." : "Army Camps add 8 spaces per level. Upgrade them to expand.") +
                "\nPractice supplies its own army; this roster is for future campaign battles.";
        }
    }
}
