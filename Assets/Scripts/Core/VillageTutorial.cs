namespace Kingdoms
{
    public sealed partial class VillageState
    {
        public const int TutorialStepCount=8, TutorialAll=(1<<TutorialStepCount)-1;
        public int tutorialMilestones;
        public bool tutorialHintsPaused;
        // Evidence is read-only; milestones are captured only on successful saves or load migration.
        public int TutorialProgress
        {
            get
            {
                int progress=tutorialMilestones;
                if(buildings!=null)
                {
                    if(Count("GoldMine")>0)progress|=1;
                    if(Count("ElixirCollector")>0)progress|=4;
                    if(Count("Barracks")>0)progress|=8;
                    if(Count("ArmyCamp")>0)progress|=16;
                    if(buildings.Exists(b=>b!=null && b.level>1))progress|=128;
                }
                if(collectedFirstGold)progress|=2;
                if(ArmyReady || HasCampaignRun || campaignCleared!=0)progress|=32;
                if(CampaignCleared(0))progress|=64;
                return progress;
            }
        }
        public int TutorialStep
        {
            get {int progress=TutorialProgress;for(int i=0;i<TutorialStepCount;i++)if((progress&(1<<i))==0)return i;return TutorialStepCount;}
        }
        public bool TutorialComplete=>TutorialStep==TutorialStepCount;
    }
    public static class VillageTutorial
    {
        static readonly string[] Titles={"Build a Gold Mine","Collect some gold","Build an Elixir Collector","Build Barracks","Build an Army Camp","Prepare your army","Win and claim Gate Outpost","Complete a building upgrade"};
        public static string Title(int step)=>step>=0 && step<Titles.Length ? Titles[step] : "Your village is ready";
        public static string Hint(VillageState state)
        {
            switch(state.TutorialStep)
            {
                case 0:return "Open Shop > Resources. A Gold Mine costs 150 elixir. Place it on a free patch of ground.";
                case 1:return state.CollectableGold>0 ? "Gold is ready. Collect it from the mine or use Collect All. Make room in storage if it is full." : "Your mine produces gold over time. Wait for its counter to rise, then collect.";
                case 2:return "Open Shop > Resources. An Elixir Collector costs 150 gold and produces the elixir needed for army buildings.";
                case 3:return "Open Shop > Army. Barracks cost 200 elixir and unlock Archers and Army Camps. Collect elixir if you need more.";
                case 4:return "Open Shop > Army. An Army Camp costs 250 elixir and adds eight spaces. Let your collector produce, then collect elixir if needed.";
                case 5:return "Prepare Army is free and instant. Add troops or use Fill. A full camp helps in your first attack.";
                case 6:return state.HasCampaignRun ? (state.campaignOutcome==PracticeOutcome.Running ? "Open Campaign and Resume Attack to continue your saved battle. If no compatible checkpoint exists, abandon the attack and prepare again for free." : "Open Campaign to claim a victory or dismiss a loss. Only claiming the Gate Outpost victory completes this step. Make storage room if required.") : !state.ArmyReady ? "Prepare another army for free, then try Gate Outpost. Losses do not block your progress." : "Open Campaign and scout Gate Outpost. Start spends the whole roster. Deploy inside the green zone or use lane buttons, win, then claim the reward.";
                case 7:return "Select your Gold Mine and open Upgrade. It costs 300 elixir and takes 30 seconds with a free builder. Completion counts; canceling does not. Any completed building upgrade qualifies.";
                default:return "You have completed the build, prepare, attack and upgrade loop. Explore the remaining missions and expand your village.";
            }
        }
    }
}
