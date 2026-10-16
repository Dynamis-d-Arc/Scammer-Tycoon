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
        public static readonly string[] VoucherSymbols={"MOON","STAR","LEAF","BOLT","GEM"};
        public static bool HasLaptop(Company c,Worker w) => c.layout!=null && c.layout.Exists(it=>it.id==w.station && it.kind=="laptop" && it.placed);
        public static bool BeginVoucher(TycoonState s,Worker w,int seed)
        {
            if(s.paused || !s.Current.workers.Contains(w) || w.voucher!=null || s.elapsed<w.voucherReadyAt || !HasLaptop(s.Current,w))return false;
            var random=new System.Random(seed);var symbols=new int[4];for(int i=0;i<4;i++)symbols[i]=random.Next(5);
            w.voucher=new VoucherRound{symbols=symbols,allowance=(w.skill-1)/2,reward=180+w.skill*25+(int)SkillBonus(s.Current,0)*5,deadline=s.elapsed+25+w.skill*3+SkillBonus(s.Current,0)};
            return true;
        }
        public static bool ChooseVoucher(TycoonState s,Worker w,int symbol,Action<string,float> report)
        {
            if(s.paused || !s.Current.workers.Contains(w) || w.voucher==null || symbol<0 || symbol>=5)return false;
            var r=w.voucher;
            if(!HasLaptop(s.Current,w)){FinishVoucher(s,s.active,w,false,report);return false;}
            if(r.symbols[r.progress]==symbol)r.progress++;else r.mistakes++;
            if(r.progress==4 || r.mistakes>r.allowance)FinishVoucher(s,s.active,w,r.progress==4,report);
            return true;
        }
        public static void FinishVoucher(TycoonState s,int office,Worker w,bool won,Action<string,float> report)
        {
            if(w.voucher==null)return;
            int reward=w.voucher.reward;w.voucher=null;w.voucherReadyAt=s.elapsed+60;
            if(won)
            {
                s.cash+=reward;s.companies[office].earned+=reward;s.totalJobIncome+=reward;
                w.completed++;w.skill=Mathf.Max(w.skill,Mathf.Min(5,1+w.completed/5));
            }
            w.status=won?"Voucher cleared +$"+reward:"Voucher rejected";w.reaction=4;
            report(w.status+(won?"":" · no payout"),won?reward:0);
            if(won)CampaignResult(s,office,0,true,report);
        }
        public static bool ValidVoucher(Worker w)
        {
            if(float.IsNaN(w.voucherReadyAt) || float.IsInfinity(w.voucherReadyAt) || w.voucherReadyAt<0)return false;
            var r=w.voucher;if(r==null)return !w.voucherActive;
            if(r.symbols==null || r.symbols.Length!=4 || r.progress<0 || r.progress>=4 || r.allowance<0 || r.allowance>2 || r.mistakes<0 || r.mistakes>r.allowance || r.reward<205 || r.reward>380 || float.IsNaN(r.deadline) || float.IsInfinity(r.deadline) || r.deadline<0)return false;
            foreach(int symbol in r.symbols)if(symbol<0 || symbol>=5)return false;
            return true;
        }
        public static readonly string[] SkillNames={"Campaign basics","Phantom focus","Mirage focus","Moonshot focus","Phantom expertise","Mirage expertise","Moonshot expertise","Campaign mastery"};
        public static int SkillCost(int node) => node==0?400:node<=3?600:node<=6?1200:2500;
        public static int SkillPrerequisites(int node) => node==0?0:node<=3?1:node<=6?1<<(node-3):(1<<4)|(1<<5)|(1<<6);
        public static bool HasSkill(Company c,int node) => node>=0 && node<8 && (c.campaignSkills&(1<<node))!=0;
        public static bool ValidSkills(int skills)
        {
            if(skills<0 || skills>255)return false;
            for(int node=0;node<8;node++)if((skills&(1<<node))!=0 && (skills&SkillPrerequisites(node))!=SkillPrerequisites(node))return false;
            return true;
        }
        public static string SkillEffect(int node) => node==0?"+3 success points · all campaigns":node==7?"+3 success points · all campaigns":node<=3?"+4 success points · this campaign":"+5 success points · this campaign";
        public static float SkillBonus(Company c,int job) => job<0 || job>2?0:(HasSkill(c,0)?3:0)+(HasSkill(c,job+1)?4:0)+(HasSkill(c,job+4)?5:0)+(HasSkill(c,7)?3:0);
        public static bool UnlockSkill(TycoonState s,int node)
        {
            if(node<0 || node>=8 || s.paused || HasSkill(s.Current,node))return false;
            int required=SkillPrerequisites(node);
            if((s.Current.campaignSkills&required)!=required || !Rules.Pay(s,SkillCost(node)))return false;
            s.Current.campaignSkills|=1<<node;return true;
        }
        public static int SuspicionGain(int job) => job==0?4:job==1?7:job==2?12:0;
        public static string SuspicionLevel(Company c) => c.suspicion>=75?"Auto break soon":c.suspicion>=40?"Building":"Low";
        public static bool LayLow(TycoonState s)
        {
            if(s.paused || s.Current.lockdown>0)return false;
            s.Current.lockdown=30;return true;
        }
        public static bool CampaignResult(TycoonState s,int office,int job,bool won,Action<string,float> report)
        {
            if(job==3)return false;
            var c=s.companies[office];
            c.suspicion=Mathf.Min(100,c.suspicion+(SuspicionGain(job)+(won?0:3))*(1-.08f*c.securityTier));
            if(c.suspicion<100)
            {
                return false;
            }
            c.suspicion=45;c.lockdown=Mathf.Max(c.lockdown,30);
            report(Management.Name(office)+": team taking a 30s cooldown. Work resumes automatically; no money lost.",0);
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
            if(s.paused || specialty<1 || specialty>4 || w.specialty==specialty || !s.Current.workers.Contains(w) || w.job>=0 || w.voucher!=null || !Rules.Pay(s,300))return false;
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
