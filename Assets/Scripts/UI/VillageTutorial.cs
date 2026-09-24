using UnityEngine;
using UnityEngine.UI;

namespace Kingdoms.UI
{
    public sealed partial class VillageGameplay
    {
        int tutorialPageIndex;
        Button tutorialEntry,tutorialAction,tutorialPause;
        Text tutorialTitle,tutorialChecklist,tutorialHelp;
        void BuildTutorialInterface()
        {
            tutorialEntry=Button("Village Guide",safe,"VILLAGE GUIDE",new Vector2(.36f,.80f),new Vector2(.64f,.87f),new Color(.28f,.55f,.76f));
            ReferenceText(tutorialEntry.GetComponentInChildren<Text>(),22,Color.white);tutorialEntry.onClick.AddListener(OpenVillageGuide);
            tutorialPageIndex=profilePages.Count;var page=Box("Village Guide Page",profilePages[0].parent,Vector2.zero,Vector2.one);profilePages.Add(page);
            tutorialTitle=Label("Village Guide Title",page,"",36,new Color(.23f,.25f,.3f),new Vector2(.05f,.84f),new Vector2(.95f,.98f));
            tutorialChecklist=Label("Village Guide Checklist",page,"",24,new Color(.23f,.25f,.3f),new Vector2(.04f,.23f),new Vector2(.47f,.82f));tutorialChecklist.alignment=TextAnchor.MiddleLeft;
            tutorialHelp=Label("Village Guide Help",page,"",26,new Color(.23f,.25f,.3f),new Vector2(.50f,.25f),new Vector2(.95f,.8f));
            tutorialPause=Button("Toggle Guide Hints",page,"PAUSE HINTS",new Vector2(.04f,.06f),new Vector2(.46f,.20f),new Color(.55f,.44f,.20f));tutorialPause.onClick.AddListener(ToggleTutorialHints);
            tutorialAction=Button("Guide Next Action",page,"",new Vector2(.51f,.06f),new Vector2(.96f,.20f),new Color(.35f,.60f,.25f));tutorialAction.onClick.AddListener(FollowTutorialStep);
            page.gameObject.SetActive(false);
        }
        public void OpenVillageGuide()
        {if(State==null || PracticeOpen || IsPlacing)return;OpenProfile(tutorialPageIndex);RefreshTutorialInterface();}
        void ToggleTutorialHints()
        {
            var candidate=State.Copy();candidate.tutorialHintsPaused=!candidate.tutorialHintsPaused;
            if(!VillageSave.TryWrite(candidate,out var error)){tutorialHelp.text=error;return;}
            State=candidate;RefreshTutorialInterface();
        }
        void RefreshTutorialInterface()
        {
            if(tutorialEntry==null || State==null)return;
            int step=State.TutorialStep;
            tutorialEntry.gameObject.SetActive(!PracticeOpen && !IsPlacing && !ProfileOpen && !shop.activeSelf && !DetailsOpen && selectedIndex<0);
            tutorialEntry.GetComponentInChildren<Text>().text=State.TutorialComplete ? "GUIDE COMPLETE" : State.tutorialHintsPaused ? "GUIDE (PAUSED)" : "VILLAGE GUIDE "+(step+1)+" / 8";
            tutorialTitle.text=State.TutorialComplete ? "Village guide complete" : "Village guide - Step "+(step+1)+" of 8";
            string checklist="";for(int i=0;i<VillageState.TutorialStepCount;i++)checklist+=((State.TutorialProgress&(1<<i))!=0 ? "DONE  " : i==step ? ">  " : "-  ")+(i+1)+". "+VillageTutorial.Title(i)+"\n";
            tutorialChecklist.text=checklist.TrimEnd();tutorialHelp.text=VillageTutorial.Hint(State);
            tutorialPause.GetComponentInChildren<Text>().text=State.tutorialHintsPaused ? "RESUME HINTS" : "PAUSE HINTS";
            tutorialAction.GetComponentInChildren<Text>().text=step==0 || step==2 ? "OPEN RESOURCE SHOP" : step==3 || step==4 ? "OPEN ARMY SHOP" : step==1 ? "COLLECT RESOURCES" : step==5 || (step==6 && !State.HasCampaignRun && !State.ArmyReady) ? "PREPARE ARMY" : step==6 ? "OPEN CAMPAIGN" : step==7 ? "VIEW UPGRADE" : "EXPLORE CAMPAIGN";
        }
        void FollowTutorialStep()
        {
            if(State==null || PracticeOpen || IsPlacing)return;
            int step=State.TutorialStep;CloseProfile();
            if(step==0 || step==2 || step==3 || step==4){OpenShop();SetReferenceCategory(step<3 ? 1 : 0);}
            else if(step==1)Collect();
            else if(step==5 || (step==6 && !State.HasCampaignRun && !State.ArmyReady))OpenArmyPreparation();
            else if(step==6 || step==8)OpenCampaign();
            else
            {
                int index=State.buildings.FindIndex(b=>b.kind=="GoldMine");
                if(index>=0){SelectBuilding(index);OpenBuildingDetails();}
            }
            RefreshTutorialInterface();
        }
        bool TutorialHintsVisible=>State!=null && !State.tutorialHintsPaused && !State.TutorialComplete && !PracticeOpen && !IsPlacing && !ProfileOpen && !shop.activeSelf && !DetailsOpen && selectedIndex<0;
    }
}
