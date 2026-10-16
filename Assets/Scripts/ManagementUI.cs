using UnityEngine;

namespace ScammerTown
{
    public partial class ScammerTownGame
    {
        int selectedWorker;
        AudioSource feedbackAudio;
        AudioClip rewardSound,buySound;
        float popupUntil,popupAmount;
        Worker SelectedWorker => C.workers==null || C.workers.Count==0 ? null : C.workers[Mathf.Clamp(selectedWorker,0,C.workers.Count-1)];
        void SetupFeedback()
        {
            feedbackAudio=gameObject.AddComponent<AudioSource>();feedbackAudio.playOnAwake=false;feedbackAudio.volume=.16f;
            rewardSound=Tone("Job success",660,990);buySound=Tone("Upgrade complete",440,660);
        }
        AudioClip Tone(string name,float a,float b)
        {
            int count=11025;var data=new float[count];
            for(int i=0;i<count;i++) {float t=i/44100f;float f=t<.12f ? a : b;data[i]=Mathf.Sin(t*f*Mathf.PI*2)*Mathf.Sin(Mathf.PI*i/count)*.35f;}
            var clip=AudioClip.Create(name,count,1,44100,false);clip.SetData(data,0);return clip;
        }
        void PlayFeedback(bool success) {if(!state.muted && feedbackAudio!=null)feedbackAudio.PlayOneShot(success ? rewardSound : buySound);}
        void RewardFeedback(float amount) {popupAmount=amount;popupUntil=Time.unscaledTime+2;PlayFeedback(true);}
        void DrawRewardPopup()
        {
            if(Time.unscaledTime>=popupUntil)return;
            float age=2-(popupUntil-Time.unscaledTime);
            Label(390,95-age*22,230,36,"+"+Money(popupAmount),stat);
        }
        void DrawProgressAndAlerts()
        {
            Label(300,714,580,28,Management.Goal(state),heading);
            if(state.goal<5 && Button(886,713,196,"Claim $"+Management.GoalReward(state.goal),CanAct && GoalReady,30))
            {
                int reward=Management.GoalReward(state.goal);
                if(Management.ClaimGoal(state)){RewardFeedback(reward);Log("Milestone complete · +"+Money(reward));Save();}
                else Notify("Complete the goal shown before claiming its reward.");
            }
            if(state.incidentCompany>=0)
            {
                Label(300,752,430,52,Management.Name(state.incidentCompany)+": threat in "+Mathf.CeilToInt(state.incidentCountdown)+"s. Isolate free, or secure for $100.");
                if(Button(746,754,160,"Isolate · free",CanAct,40))ResolveThreat(false);
                if(Button(918,754,164,"Secure · $100",CanAct && state.cash>=100,40))ResolveThreat(true);
            }
            else
            {
                Label(300,752,780,50,GoalReady ? "Milestone complete! Claim your reward to reveal the next goal." : state.goal==0 ? "Select an employee, choose Assignment, then pick a job. They keep working automatically." : "Grow at your own pace. Rent and salaries are paid every game day (120 seconds).",small);
            }
        }
        void ResolveThreat(bool pay)
        {
            if(state.incidentCompany<0 || !CanAct || (pay && !Rules.Pay(state,100)))return;
            var c=state.companies[state.incidentCompany];if(!pay)c.lockdown=25;
            state.incidentCompany=-1;Log(pay ? "Connection secured for $100; work continues." : "Connection isolated for 25 seconds. Threat blocked for free.");PlayFeedback(false);Save();
        }
        void Recruit(int specialist)
        {
            if(Management.Hire(state,state.active,specialist)){selectedWorker=C.workers.Count-1;OpenWorkerProfile(selectedWorker);Notify(C.workers[selectedWorker].name+" joined the crew!");PlayFeedback(false);Save();}
            else Notify("Not enough funds or employee capacity. Expand in Properties.");
        }
        void DrawRoomUpgrades()
        {
            Space();Text("ROOM IMPROVEMENTS · 3 LEVELS EACH");
            UpgradeButton("Computers",C.computerUpgrade,450,"12% faster jobs per level; brighter desk trim",()=>C.computerUpgrade++);
            UpgradeButton("Chairs",C.comfortUpgrade,300,"4% faster jobs per level; upholstered seats",()=>C.comfortUpgrade++);
            UpgradeButton("Lighting",C.lightingUpgrade,250,"5% faster jobs per level; lit floor borders",()=>C.lightingUpgrade++);
            UpgradeButton("Décor",C.decorUpgrade,200,"Morale recovery; decorative furniture sold separately",()=>C.decorUpgrade++);
        }
        void UpgradeButton(string name,int level,int unitCost,string benefit,System.Action upgrade)
        {
            Text(name+" · "+level+"/3\n"+benefit,true);
            Action(level==3 ? "Fully upgraded" : "Upgrade "+name+" · "+Money(unitCost*(level+1)),()=>Buy("Upgrade "+name,unitCost*(level+1),upgrade),CanAct && level<3 && state.cash>=unitCost*(level+1));
            if(level<3 && state.cash<unitCost*(level+1))Text("Need "+Money(unitCost*(level+1)-state.cash)+" more",true);
        }
        void DrawWorkerBubble(RoomItem item,Vector2 p,float tile)
        {
            var w=Management.At(C,item);if(w==null)return;
            string text=w.name+" · "+(w.reaction>0 ? w.status : w.job>=0 ? Mathf.CeilToInt(w.remaining)+"s" : "Ready");
            float labelY=p.y-tile*2.25f*WorkstationVisualScale;
            Rect r=labelY<10 ? new Rect(p.x+tile*1.4f,10,140,33) : new Rect(p.x-70,labelY,140,33);
            Panel(r,w.reaction>0 ? new Color(.09f,.24f,.23f,.97f) : new Color(.065f,.095f,.16f,.97f));
            if(heldItem!=null) GUI.Label(r,text,small);
            else if(GUI.Button(r,text,small))OpenWorkerProfile(C.workers.IndexOf(w));
            if(w.job>=0)Bar(r.x,r.y+34,r.width,(1-w.remaining/w.duration)*100,lime);
        }
    }
}
