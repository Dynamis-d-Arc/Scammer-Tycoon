using System;
using UnityEngine;

namespace ScammerTown
{
    [Serializable] public class VoucherRound
    {
        public int[] symbols;
        public int progress, mistakes, allowance, reward;
        public float deadline;
    }
    [Serializable] public class ShortContract
    {
        public int job, company, target, progress, reward;
        public float deadline;
    }
    public static class OfficeProgression
    {
        // Voucher sessions were replaced by The Call; old saves still validate their data.
        public static bool ValidVoucher(Worker w)
        {
            if(float.IsNaN(w.voucherReadyAt) || float.IsInfinity(w.voucherReadyAt) || w.voucherReadyAt<0)return false;
            var r=w.voucher;if(r==null)return !w.voucherActive;
            if(r.symbols==null || r.symbols.Length!=4 || r.progress<0 || r.progress>=4 || r.allowance<0 || r.allowance>2 || r.mistakes<0 || r.mistakes>r.allowance || r.reward<205 || r.reward>380 || float.IsNaN(r.deadline) || float.IsInfinity(r.deadline) || r.deadline<0)return false;
            foreach(int symbol in r.symbols)if(symbol<0 || symbol>=5)return false;
            return true;
        }
        public static readonly string[] SkillNames={"Campaign basics","Phantom focus","Mirage focus","Moonshot focus","Phantom expertise","Mirage expertise","Moonshot expertise","Campaign mastery","Velvet focus","Echo focus","Crown focus","Velvet expertise","Echo expertise","Crown expertise"};
        public static bool FocusNode(int node) => (node>=1 && node<=3) || (node>=8 && node<=10);
        public static int FocusFor(int job) => job<3?job+1:job+5;
        public static int ExpertiseFor(int job) => job<3?job+4:job+8;
        public static int SkillCost(int node) => node==0?400:node==7?2500:FocusNode(node)?600:1200;
        public static int SkillPrerequisites(int node) => node==0?0:node==7?(1<<4)|(1<<5)|(1<<6):FocusNode(node)?1:1<<(node-3);
        public static bool HasSkill(Company c,int node) => node>=0 && node<SkillNames.Length && (c.campaignSkills&(1<<node))!=0;
        public static bool ValidSkills(int skills)
        {
            if(skills<0 || skills>=(1<<SkillNames.Length))return false;
            for(int node=0;node<SkillNames.Length;node++)if((skills&(1<<node))!=0 && (skills&SkillPrerequisites(node))!=SkillPrerequisites(node))return false;
            return true;
        }
        public static string SkillEffect(int node) => node==0?"+3 success points · all campaigns":node==7?"+3 success points · all campaigns":FocusNode(node)?"+4 success points · this campaign":"+5 success points · this campaign";
        public static float SkillBonus(Company c,int job) => job<0 || job>=Management.Jobs.Length?0:(HasSkill(c,0)?3:0)+(HasSkill(c,FocusFor(job))?4:0)+(HasSkill(c,ExpertiseFor(job))?5:0)+(HasSkill(c,7)?3:0);
        public static bool UnlockSkill(TycoonState s,int node)
        {
            if(node<0 || node>=SkillNames.Length || s.paused || HasSkill(s.Current,node))return false;
            int required=SkillPrerequisites(node);
            if((s.Current.campaignSkills&required)!=required || !Rules.Pay(s,SkillCost(node)))return false;
            s.Current.campaignSkills|=1<<node;return true;
        }
        public static int SuspicionGain(int job) => job==0?4:job==1?7:job==2?12:job==3?8:job==4?15:job==5?22:0;
        public static float ActualSuspicion(Company c,int job,bool won) => (SuspicionGain(job)+(won?0:3))*EmpireProgression.Suspicion(c)*LivingTown.HeatFactor(c,job);
        public static string SuspicionLevel(Company c) => c.suspicion>=75?"Auto break soon":c.suspicion>=40?"Building":"Low";
        public static bool LayLow(TycoonState s)
        {
            if(s.paused || s.Current.lockdown>0)return false;
            s.Current.lockdown=30;s.Current.suspicion=Mathf.Max(0,s.Current.suspicion-20);s.Current.pushUntil=0;return true;
        }
        public static bool CampaignResult(TycoonState s,int office,int job,bool won,Action<string,float> report,float heat=-1)
        {
            if(job<0 || job>=Management.Jobs.Length)return false;
            var c=s.companies[office];
            c.suspicion=Mathf.Min(100,c.suspicion+(heat<0?ActualSuspicion(c,job,won):heat));
            if(c.suspicion<100)
            {
                return false;
            }
            LivingTown.OnShutdown(c,report);
            Heat.OnCooldown(s);
            c.suspicion=45;c.lockdown=Mathf.Max(c.lockdown,EmpireProgression.BreakSeconds(c));
            report(Management.Name(office)+": team taking a "+EmpireProgression.BreakSeconds(c)+"s cooldown. Work resumes automatically; banked cash is safe.",0);
            return true;
        }
        public static string CampaignActivity(Worker w)
        {
            if(w.job<0)return "Awaiting campaign";
            float progress=1-w.remaining/Mathf.Max(.01f,w.duration);
            return LivingTown.Activity(w,progress);
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
        public static bool Placed(Company c,string kind) => c.layout!=null && c.layout.Exists(item=>item.placed && item.kind==kind);
        public static string DecorBenefit(string kind) => kind=="shredder" ? "Den gear: 10% less suspicion from every scam here. One counts." :
            kind=="cashcounter" ? "Den gear: +5% successful take here. One counts." :
            kind=="scanner" ? "Den gear: you hear them coming. Automatic cooldowns 5s shorter. One counts." :
            kind=="cashpallet" ? "Cosmetic. Nobody counts it. Everybody stares at it." : kind=="lamp" || kind=="shelf" ? "+2% job speed each (max 2 of each; +8% total)." : kind=="plant" ? "+0.03 morale/sec each (max 2)." : kind=="sofa" || kind=="rug" ? "+0.04 morale/sec (one per type)." : "Cosmetic decoration.";
        public static bool Train(TycoonState s,Worker w,int specialty)
        {
            if(s.paused || specialty<1 || specialty>Management.Jobs.Length || !EmpireProgression.Unlocked(s,s.active,specialty-1) || w.specialty==specialty || !s.Current.workers.Contains(w) || w.job>=0 || w.onCall || !Rules.Pay(s,300))return false;
            w.specialty=specialty;return true;
        }
        public static bool Accept(TycoonState s,int job)
        {
            if(s.paused || s.contractActive || s.elapsed<s.contractCooldown || !EmpireProgression.Unlocked(s,s.active,job))return false;
            s.contract=new ShortContract{job=job,company=s.active,target=EmpireProgression.ContractTarget(job),reward=EmpireProgression.ContractReward(job),deadline=s.elapsed+120};
            s.contractActive=true;
            return true;
        }
        public static void CompleteJob(TycoonState s,int company,int job,float time,Action<string,float> report)
        {
            var c=s.contract;
            if(!s.contractActive || c==null || c.company!=company || c.job!=job || time>c.deadline)return;
            if(++c.progress<c.target)return;
            Laundering.Income(s,c.reward);s.contractsCompleted++;s.contractCooldown=s.elapsed+30;s.contract=null;s.contractActive=false;
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
