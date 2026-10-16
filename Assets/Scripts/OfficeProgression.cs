using System;
using UnityEngine;

namespace ScammerTown
{
    [Serializable] public class ShortContract
    {
        public int job, company, target, progress, reward;
        public float deadline;
    }
    public static class OfficeProgression
    {
        public static int SuspicionGain(int job) => job==0?4:job==1?7:job==2?12:0;
        public static string SuspicionLevel(Company c) => c.suspicion>=75?"Investigated":c.suspicion>=40?"Watched":"Quiet";
        public static bool LayLow(TycoonState s)
        {
            if(s.paused || s.Current.lockdown>0)return false;
            s.Current.lockdown=30;return true;
        }
        public static bool CampaignResult(TycoonState s,int office,int job,bool won,Action<string,float> report)
        {
            if(job==3)return false;
            var c=s.companies[office];float before=c.suspicion;
            c.suspicion=Mathf.Min(100,c.suspicion+SuspicionGain(job)+(won?0:3));
            if(c.suspicion<100)
            {
                if(before<75 && c.suspicion>=75)report(Management.Name(office)+": investigation warning! Lay low before suspicion reaches 100.",0);
                return false;
            }
            float fine=Mathf.Min(500,s.cash*.05f);s.cash-=fine;
            c.suspicion=45;c.lockdown=Mathf.Max(c.lockdown,30);
            report(Management.Name(office)+": campaign accounts frozen for 30s; $"+Mathf.FloorToInt(fine)+" penalty. Suspicion reset to 45.",0);
            return true;
        }
        public static string CampaignActivity(Worker w)
        {
            if(w.job<0)return "Awaiting campaign";
            if(w.job==3)return "Repair shift";
            float progress=1-w.remaining/Mathf.Max(.01f,w.duration);
            return progress<.33f?"Campaign live":progress<.67f?"Fictional chats active":"Awaiting result";
        }
        public static string Specialty(int value) => value==0 ? "Generalist" : Management.Jobs[value-1].name+" specialist";
        public static bool Matches(Worker w,int job) => w.specialty==job+1;
        public static float DecorSpeed(Company c)
        {
            int lamps=0,shelves=0;
            if(c.layout!=null)foreach(var item in c.layout)if(item.placed){if(item.kind=="lamp")lamps++;if(item.kind=="shelf")shelves++;}
            return .02f*(Mathf.Min(2,lamps)+Mathf.Min(2,shelves));
        }
        public static float DecorMorale(Company c)
        {
            int plants=0;bool sofa=false,rug=false;
            if(c.layout!=null)foreach(var item in c.layout)if(item.placed){if(item.kind=="plant")plants++;if(item.kind=="sofa")sofa=true;if(item.kind=="rug")rug=true;}
            return Mathf.Min(plants,2)*.03f+(sofa?.04f:0)+(rug?.04f:0);
        }
        public static string DecorBenefit(string kind) => kind=="lamp" || kind=="shelf" ? "+2% job speed each (max 2 of each; +8% total)." : kind=="plant" ? "+0.03 morale/sec each (max 2)." : kind=="sofa" || kind=="rug" ? "+0.04 morale/sec (one per type)." : "Cosmetic decoration.";
        public static bool Train(TycoonState s,Worker w,int specialty)
        {
            if(s.paused || specialty<1 || specialty>4 || w.specialty==specialty || !s.Current.workers.Contains(w) || w.job>=0 || !Rules.Pay(s,300))return false;
            w.specialty=specialty;return true;
        }
        public static bool Accept(TycoonState s,int job)
        {
            if(s.paused || s.contractActive || s.elapsed<s.contractCooldown || job<0 || job>2 || (job==2 && s.jobsCompleted<5))return false;
            s.contract=new ShortContract{job=job,company=s.active,target=job==0?5:job==1?4:3,reward=job==0?400:job==1?700:1000,deadline=s.elapsed+120};
            s.contractActive=true;
            return true;
        }
        public static void CompleteJob(TycoonState s,int company,int job,float time,Action<string,float> report)
        {
            var c=s.contract;
            if(!s.contractActive || c==null || c.company!=company || c.job!=job || time>c.deadline)return;
            if(++c.progress<c.target)return;
            s.cash+=c.reward;s.contractsCompleted++;s.contractCooldown=s.elapsed+30;s.contract=null;s.contractActive=false;
            report("Contract complete · +$"+c.reward,c.reward);
        }
        public static void Expire(TycoonState s,Action<string,float> report)
        {
            if(!s.contractActive || s.contract==null || s.elapsed<s.contract.deadline)return;
            s.contract=null;s.contractActive=false;s.contractCooldown=s.elapsed+30;
            report("Contract expired. No penalty; try again in 30 seconds.",0);
        }
    }
}
