using UnityEngine;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Kingdoms.UI
{
    public sealed partial class VillageGameplay
    {
        GameObject deploymentZone;
        Material deploymentMaterial;
        Vector2 deploymentPress;
        bool deploymentHeld, deploymentTouchBlocked;
        int deploymentFinger, deploymentInputAfterFrame;
        string deploymentFeedback="";
        float deploymentFeedbackUntil;

        void CancelGroundGesture() { deploymentHeld=false; }
        void BeginGroundGesture(Vector2 point)
        {
            deploymentPress=point;
            deploymentHeld=GroundInputAllowed && !OverUI(point);
        }
        void MoveGroundGesture(Vector2 point)
        {
            if((point-deploymentPress).sqrMagnitude>144 || OverUI(point))CancelGroundGesture();
        }
        void EndGroundGesture(Vector2 point)
        {
            MoveGroundGesture(point);
            bool accepted=deploymentHeld;CancelGroundGesture();
            if(accepted)TryDeployPracticeAtScreen(point);
        }
        bool GroundInputAllowed=>PracticeOpen && !ScoutingPractice && !WatchingPracticeReplay
            && !practicePaused && !checkpointSaveBlocked && practiceBattle.Outcome==PracticeOutcome.Running;

        void HandleGroundDeployment()
        {
            if(!GroundInputAllowed || Time.frameCount<=deploymentInputAfterFrame){CancelGroundGesture();return;}
            var touches=Touch.activeTouches;
            if(touches.Count>1){deploymentTouchBlocked=true;CancelGroundGesture();return;}
            if(deploymentTouchBlocked)
            {if(touches.Count==0)deploymentTouchBlocked=false;return;}
            if(touches.Count==1)
            {
                var touch=touches[0];
                if(touch.phase==UnityEngine.InputSystem.TouchPhase.Began)
                {deploymentFinger=touch.touchId;BeginGroundGesture(touch.screenPosition);}
                else if(touch.touchId!=deploymentFinger || touch.phase==UnityEngine.InputSystem.TouchPhase.Canceled)CancelGroundGesture();
                else if(touch.phase==UnityEngine.InputSystem.TouchPhase.Ended)EndGroundGesture(touch.screenPosition);
                else MoveGroundGesture(touch.screenPosition);
                return;
            }
            var mouse=Mouse.current;if(mouse==null)return;
            Vector2 point=mouse.position.ReadValue();
            if(mouse.leftButton.wasPressedThisFrame)BeginGroundGesture(point);
            if(mouse.leftButton.isPressed)MoveGroundGesture(point);
            if(mouse.leftButton.wasReleasedThisFrame)EndGroundGesture(point);
        }

        public bool TryDeployPracticeAtScreen(Vector2 point)
        {
            if(!GroundInputAllowed || float.IsNaN(point.x) || float.IsNaN(point.y)
                || float.IsInfinity(point.x) || float.IsInfinity(point.y)
                || !viewCamera.pixelRect.Contains(point) || !Screen.safeArea.Contains(point) || OverUI(point))return false;
            var plane=new Plane(Vector3.up,Vector3.zero);var ray=viewCamera.ScreenPointToRay(point);
            if(!plane.Raycast(ray,out float distance))return false;
            Vector3 position=ray.GetPoint(distance);
            if(position.x*100<practiceBattle.Layout.MinX || position.x*100>practiceBattle.Layout.MaxX
                || position.z*100<practiceBattle.Layout.MinZ || position.z*100>practiceBattle.Layout.MaxZ)
            {GroundFeedback("Tap inside the green deployment outline.");return false;}
            int x=Mathf.RoundToInt(position.x*2)*50,z=Mathf.RoundToInt(position.z*2)*50;
            if(!practiceBattle.CanDeployAt(x,z,deployedTroop,out string reason)){GroundFeedback(reason);return false;}
            if(!practiceBattle.DeployAt(x,z,deployedTroop))return false;
            GroundFeedback(deployedTroop+" deployed.");
            CreatePracticeRaider(practiceBattle.Raiders[practiceBattle.Raiders.Count-1]);SaveCampaignCheckpoint();RefreshPracticeBattle();return true;
        }
        void GroundFeedback(string message){deploymentFeedback=message;deploymentFeedbackUntil=Time.unscaledTime+2;}
        void CreateDeploymentZone()
        {
            CancelGroundGesture();deploymentFeedback="";deploymentFeedbackUntil=0;deploymentTouchBlocked=false;deploymentInputAfterFrame=Time.frameCount;
            if(deploymentMaterial==null)deploymentMaterial=PracticeMaterial("Deployment outline",new Color(.4f,1,.25f),true);
            deploymentZone=new GameObject("Deployment Zone",typeof(LineRenderer));deploymentZone.transform.SetParent(practiceWorld.transform,false);
            var line=deploymentZone.GetComponent<LineRenderer>();line.sharedMaterial=deploymentMaterial;
            line.useWorldSpace=false;line.loop=true;line.widthMultiplier=.12f;line.positionCount=4;
            float left=practiceBattle.Layout.MinX*.01f,right=practiceBattle.Layout.MaxX*.01f;
            float bottom=practiceBattle.Layout.MinZ*.01f,top=practiceBattle.Layout.MaxZ*.01f;
            line.SetPositions(new[]{new Vector3(left,.08f,bottom),new Vector3(right,.08f,bottom),new Vector3(right,.08f,top),new Vector3(left,.08f,top)});
        }
        void RefreshGroundDeployment()
        {
            deploymentZone.SetActive(!WatchingPracticeReplay && practiceBattle.Outcome==PracticeOutcome.Running);
            if(GroundInputAllowed)practiceInstructions.text=Time.unscaledTime<deploymentFeedbackUntil ? deploymentFeedback : "Select a troop, then tap inside the green outline. Lane buttons also work.";
        }
        void OnApplicationFocus(bool focused){if(!focused){CancelGroundGesture();if(CampaignBattleOpen)SaveProgress();}}
    }
}
