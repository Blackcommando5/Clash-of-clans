using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Kingdoms.UI
{
    public sealed partial class VillageGameplay
    {
        int scoutBuildingId=-1,scoutFinger,scoutInputAfterFrame;
        bool scoutHeld,scoutTouchBlocked;
        Vector2 scoutPress;
        Button scoutNext,scoutClear;
        LineRenderer scoutRange,scoutFootprint;
        Material scoutMaterial;
        public int InspectedScoutBuildingId=>scoutBuildingId;
        bool ScoutInputAllowed=>ScoutingPractice && !WatchingPracticeReplay && !practicePaused;

        void BuildScoutInspection()
        {
            scoutNext=Button("Next Scout Defense",practiceSafe,"NEXT DEFENSE",new Vector2(.02f,.34f),new Vector2(.21f,.43f),new Color(.28f,.55f,.76f));
            scoutClear=Button("Clear Scout Inspection",practiceSafe,"CLEAR INSPECTION",new Vector2(.02f,.24f),new Vector2(.21f,.32f),new Color(.48f,.35f,.19f));
            ReferenceText(scoutNext.GetComponentInChildren<Text>(),22,Color.white);ReferenceText(scoutClear.GetComponentInChildren<Text>(),20,Color.white);
            scoutNext.onClick.AddListener(InspectNextDefense);scoutClear.onClick.AddListener(ClearScoutInspection);
        }

        void ResetScoutInspection(bool release=false)
        {
            scoutBuildingId=-1;CancelScoutGesture();scoutTouchBlocked=false;scoutInputAfterFrame=Time.frameCount;
            if(scoutRange!=null)scoutRange.gameObject.SetActive(false);
            if(scoutFootprint!=null)scoutFootprint.gameObject.SetActive(false);
            if(release){scoutRange=null;scoutFootprint=null;}
        }
        public void ClearScoutInspection(){ResetScoutInspection();if(PracticeOpen)RefreshPracticeBattle();}

        public bool InspectScoutBuilding(int id)
        {
            if(!ScoutInputAllowed)return false;
            foreach(var building in practiceBattle.Buildings)
                if(building.Id==id && building.Alive)
                {scoutBuildingId=id;RefreshPracticeBattle();return true;}
            return false;
        }
        public void InspectNextDefense()
        {
            if(!ScoutInputAllowed)return;
            int first=-1;bool after=scoutBuildingId<0;
            foreach(var building in practiceBattle.Buildings)
            {
                if(building.Alive && building.Damage>0)
                {
                    if(first<0)first=building.Id;
                    if(after){InspectScoutBuilding(building.Id);return;}
                }
                if(building.Id==scoutBuildingId)after=true;
            }
            if(first>=0)InspectScoutBuilding(first);
        }

        public bool TryInspectScoutAtScreen(Vector2 point)
        {
            if(!ScoutInputAllowed || float.IsNaN(point.x) || float.IsNaN(point.y) || float.IsInfinity(point.x) || float.IsInfinity(point.y)
                || !viewCamera.pixelRect.Contains(point) || !Screen.safeArea.Contains(point) || OverUI(point))return false;
            var ray=viewCamera.ScreenPointToRay(point);float nearest=float.MaxValue;int selected=-1;
            // Only enemy-model colliders are eligible; scenery and home-village objects cannot be selected.
            foreach(var building in practiceBattle.Buildings)
            {
                if(!building.Alive || !practiceModels.TryGetValue(building.Id,out var model))continue;
                foreach(var collider in model.GetComponentsInChildren<Collider>())
                    if(collider.enabled && collider.Raycast(ray,out var hit,500) && hit.distance<nearest)
                    {nearest=hit.distance;selected=building.Id;}
            }
            if(selected>=0)return InspectScoutBuilding(selected);
            ClearScoutInspection();return false;
        }

        void CancelScoutGesture(){scoutHeld=false;}
        void BeginScoutGesture(Vector2 point){scoutPress=point;scoutHeld=ScoutInputAllowed && !OverUI(point);}
        void MoveScoutGesture(Vector2 point){if((point-scoutPress).sqrMagnitude>144 || OverUI(point))CancelScoutGesture();}
        void EndScoutGesture(Vector2 point)
        {
            MoveScoutGesture(point);bool accepted=scoutHeld;CancelScoutGesture();if(accepted)TryInspectScoutAtScreen(point);
        }
        void HandleScoutInspectionInput()
        {
            if(!ScoutInputAllowed || Time.frameCount<=scoutInputAfterFrame){CancelScoutGesture();return;}
            var touches=Touch.activeTouches;
            if(touches.Count>1){scoutTouchBlocked=true;CancelScoutGesture();return;}
            if(scoutTouchBlocked){if(touches.Count==0)scoutTouchBlocked=false;return;}
            if(touches.Count==1)
            {
                var touch=touches[0];
                if(touch.phase==UnityEngine.InputSystem.TouchPhase.Began){scoutFinger=touch.touchId;BeginScoutGesture(touch.screenPosition);}
                else if(touch.touchId!=scoutFinger || touch.phase==UnityEngine.InputSystem.TouchPhase.Canceled)CancelScoutGesture();
                else if(touch.phase==UnityEngine.InputSystem.TouchPhase.Ended)EndScoutGesture(touch.screenPosition);
                else MoveScoutGesture(touch.screenPosition);
                return;
            }
            var mouse=Mouse.current;if(mouse==null)return;Vector2 point=mouse.position.ReadValue();
            if(mouse.leftButton.wasPressedThisFrame)BeginScoutGesture(point);
            if(mouse.leftButton.isPressed)MoveScoutGesture(point);
            if(mouse.leftButton.wasReleasedThisFrame)EndScoutGesture(point);
        }

        LineRenderer ScoutOutline(string name,int points)
        {
            if(scoutMaterial==null)scoutMaterial=PracticeMaterial("Scout highlight",new Color(1,.75f,.12f),true);
            var outline=new GameObject(name,typeof(LineRenderer));outline.transform.SetParent(practiceWorld.transform,false);
            var line=outline.GetComponent<LineRenderer>();line.sharedMaterial=scoutMaterial;line.useWorldSpace=true;line.loop=true;
            line.widthMultiplier=.085f;line.positionCount=points;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;
            return line;
        }
        void RefreshScoutInspection()
        {
            scoutNext.gameObject.SetActive(ScoutingPractice);scoutClear.gameObject.SetActive(ScoutingPractice && scoutBuildingId>=0);
            scoutNext.interactable=ScoutInputAllowed;scoutClear.interactable=ScoutInputAllowed;
            if(!ScoutingPractice)return;
            if(battleMission<0 || string.IsNullOrEmpty(campaignMessage))practiceInstructions.text="Tap an enemy building to inspect it, or use Next Defense. Scouting costs nothing.";
            PracticeBattle.Entity selected=null;
            foreach(var building in practiceBattle.Buildings)if(building.Id==scoutBuildingId && building.Alive){selected=building;break;}
            if(selected==null)return;
            var definition=BuildingCatalog.Find(selected.Kind);
            practiceScoutText.text=definition.Name+" | Health: "+selected.HitPoints+" / "+selected.MaxHitPoints
                + (selected.Damage>0 ? "\nDamage: "+selected.Damage+" / second | Range: "+(selected.Range/100f).ToString("0.#")+" cells from center\nAttacks ground troops inside the gold circle."
                    : selected.Kind=="Wall" ? "\nBlocks ground movement; troops can breach it.\nWalls do not count toward destruction percentage."
                    : "\nNo attack. Counts toward destruction and victory."+(selected.Kind=="TownHall" ? "\nDestroying the Town Hall earns one star." : ""));
            if(battleMission>=0)
            {
                var mission=CampaignCatalog.Find(battleMission);
                practiceScoutText.text+=State.CampaignCleared(battleMission) ? "\nFirst-clear reward already claimed." : "\nFirst clear: "+mission.Gold+" gold + "+mission.Elixir+" elixir";
            }
            float x=selected.X*.01f,z=selected.Z*.01f,half=selected.HalfSize*.01f+.05f;
            if(scoutFootprint==null)scoutFootprint=ScoutOutline("Scout Building Outline",4);
            scoutFootprint.gameObject.SetActive(true);
            scoutFootprint.SetPositions(new[]{new Vector3(x-half,.11f,z-half),new Vector3(x+half,.11f,z-half),new Vector3(x+half,.11f,z+half),new Vector3(x-half,.11f,z+half)});
            if(selected.Damage>0)
            {
                if(scoutRange==null)scoutRange=ScoutOutline("Scout Defense Range",96);
                scoutRange.gameObject.SetActive(true);
                for(int i=0;i<96;i++){float angle=i*Mathf.PI*2/96;scoutRange.SetPosition(i,new Vector3(x+Mathf.Cos(angle)*selected.Range*.01f,.1f,z+Mathf.Sin(angle)*selected.Range*.01f));}
            }
            else if(scoutRange!=null)scoutRange.gameObject.SetActive(false);
        }
    }
}
