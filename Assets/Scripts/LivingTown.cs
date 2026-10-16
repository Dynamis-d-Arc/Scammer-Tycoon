using System;
using UnityEngine;

namespace ScammerTown
{
    // Save-backed progression. Quotes lock a run's rules when it begins.
    public static class LivingTown
    {
        public static readonly string[] Traits={"Steady","Smooth talker","Hustler","Perfectionist","Ambitious","Social butterfly"};
        public static readonly string[] TraitEffects={"Reliable: no modifiers.","+5 success points; 5% slower.","15% faster; 20% more suspicion.","10% slower; 20% less suspicion.","10% larger successful takes; -6 mood per run.","+2 success points per active teammate (maximum +6)."};
        public static readonly string[] Events={"Quiet streets","Neon festival","Market day","Town watch","Collectors' week"};
        public static readonly string[] EventEffects={"Normal conditions. A good time to rotate schemes.","Echo Tickets pays 50% more instead of its usual 15% off-season reduction.","Mirage Storefront pays 20% more.","All schemes generate 25% more suspicion.","Velvet and Crown pay 20% more."};
        public static readonly string[] RankNames={"Street operator","Crew boss","District contender","Harbor magnate","Town legend"};
        public static readonly int[] RankRuns={25,75,150,300,600};
        public static readonly string[] RankRewards={"$1,500 + town event forecast","$3,000 + team coordination (+3 success with 2 active teammates)","$6,000 + Harbor headquarters renovation unlocked","$10,000 + 50% of the Moonshot pot protected during shutdowns","$20,000 + gold office insignia"};
        public static int EventAt(float elapsed) => ((int)(elapsed/Rules.DaySeconds))%5;
        public static void Ensure(TycoonState s)
        {
            for(int office=0;office<s.companies.Length;office++)
            {
                var c=s.companies[office];
                if(c.townResistance==null)c.townResistance=new float[6];
                if(c.shelvedUntil==null)c.shelvedUntil=new float[6];
                c.townEvent=EventAt(s.elapsed);c.potProtected=s.empireRank>=4;
                for(int i=0;i<c.workers.Count;i++)
                {
                    var w=c.workers[i];
                    if(w.traitVersion==0){w.trait=(office+i)%Traits.Length;w.traitVersion=1;}
                    if(w.schemeSteps==null)w.schemeSteps=new int[6];
                }
            }
            s.townVersion=1;
        }
        static int Step(Worker w,int job) => w.schemeSteps==null?0:w.schemeSteps[job];
        public static float Resistance(Company c,int job) => c.townResistance==null?0:c.townResistance[job];
        public static int Teammates(Company c,Worker w) => c.workers==null?0:c.workers.FindAll(other=>other!=w && (other.job>=0 || other.assignment>=0) && other.breakRemaining<=0 && Management.HasPlacedDesk(c,other)).Count;
        public static float SuccessChance(TycoonState s,Company c,Worker w,int job) => Mathf.Clamp(Management.Chance(c,w,job)+(s.empireRank>=2 && Teammates(c,w)>=2?3:0),20,98);
        public static float ChanceBonus(Company c,Worker w,int job) => (w.trait==1?5:w.trait==5?Mathf.Min(6,Teammates(c,w)*2):0)-Resistance(c,job)*.15f;
        public static float WorkSpeed(Company c,Worker w) => (w.trait==1?.95f:w.trait==2?1.15f:w.trait==3?.9f:1)*(c.headquarters?1.1f:1);
        public static float HeatFactor(Company c,int job) => (1+Resistance(c,job)*.005f)*(c.townEvent==3?1.25f:1);
        public static float Heat(TycoonState s,Company c,Worker w,int job) => OfficeProgression.ActualSuspicion(c,job,true)*(w.trait==2?1.2f:w.trait==3?.8f:1)*(s.elapsed<c.pushUntil?1.75f:1);
        public static bool CanStart(TycoonState s,Company c,Worker w,int job) => w.breakRemaining<=0 && !w.breakQueued && (c.shelvedUntil==null || s.elapsed>=c.shelvedUntil[job]);
        public static float TakeMultiplier(Company c,Worker w,int job)
        {
            float mechanic=job==0?1+.1f*w.phantomStreak:job==1?(Step(w,1)==2?1.6f:1):job==3?(Step(w,3)==2?2:1):job==4?(c.townEvent==1?1.5f:.85f):job==5?(Step(w,5)==2?2.5f:.5f):1;
            float season=(job==1 && c.townEvent==2) || ((job==3 || job==5) && c.townEvent==4)?1.2f:1;
            return mechanic*season*(w.trait==4?1.1f:1);
        }
        public static int PreviewPay(TycoonState s,int office,Worker w,int job,bool won) => won?Mathf.RoundToInt(EmpireProgression.Pay(s.companies[office],office,job,true)*TakeMultiplier(s.companies[office],w,job)*(s.elapsed<s.companies[office].pushUntil?1.25f:1)*Syndicate.UndercutFactor(s,job)):EmpireProgression.Pay(s.companies[office],office,job,false);
        public static void Quote(TycoonState s,int office,Worker w,int job)
        {
            var c=s.companies[office];c.townEvent=EventAt(s.elapsed);
            w.quoteVersion=1;w.quotedPay=PreviewPay(s,office,w,job,true);w.quotedPartial=PreviewPay(s,office,w,job,false);
            w.quotedChance=SuccessChance(s,c,w,job);
            w.quotedSuspicion=Heat(s,c,w,job);
        }
        public static string Mechanic(int job) => new[]{
            "Streak: each success adds 10% to the next take (up to +30%). A partial result resets it.",
            "Batch: every third run pays 60% extra on success; then the batch resets.",
            "Pot: successful runs add an extra 20% to a shared pot. Cash out before a shutdown.",
            "Auction: two successful opening rounds build a final round worth double. Partial results keep the round.",
            "Festival window: +50% take during the Neon festival; -15% outside it. Shelve while waiting.",
            "Preparation: two half-pay setup runs lead to a 2.5x finale. Partial results still advance preparation."
        }[job];
        public static string WorkerMechanic(Worker w,int job) => job==0?"Streak "+w.phantomStreak+" / 3":job==1?"Batch "+(Step(w,1)+1)+" / 3":job==3?"Auction round "+(Step(w,3)+1)+" / 3":job==5?"Collection stage "+(Step(w,5)+1)+" / 3":job==2?"Contributes to the office pot":"Watch the festival calendar";
        public static string Activity(Worker w,float progress)
        {
            if(w.job==3)return Step(w,3)==2?"Final auction round":"Opening auction round";
            if(w.job==5)return Step(w,5)==2?"Collection finale":"Preparing collection";
            if(w.job==4)return "Ticket window active";
            if(w.job==2)return "Building the club pot";
            if(w.job==1)return "Storefront batch "+(Step(w,1)+1)+" / 3";
            return progress<.5f?"Giveaway live":"Awaiting giveaway result";
        }
        public static void Complete(TycoonState s,int office,Worker w,int job,bool won,int pay,Action<string,float> report)
        {
            var c=s.companies[office];
            c.townResistance[job]=Mathf.Min(100,c.townResistance[job]+(won?5:8));
            if(job==0)w.phantomStreak=won?Mathf.Min(3,w.phantomStreak+1):0;
            if(job==1 || job==5 || (job==3 && won))w.schemeSteps[job]=(w.schemeSteps[job]+1)%3;
            if(job==2 && won)c.moonshotPot+=Mathf.RoundToInt(pay*.2f);
            if(w.trait==4)w.mood=Mathf.Max(20,w.mood-6);
            if(c.breakUpgrade>0 && w.mood<40)w.breakQueued=true;
        }
        public static void Advance(TycoonState s,float dt,Action<string,float> report)
        {
            foreach(var c in s.companies)
            {
                if(c.townResistance==null)continue;
                c.townEvent=EventAt(s.elapsed);
                for(int job=0;job<6;job++)c.townResistance[job]=Mathf.Max(0,c.townResistance[job]-dt*.08f);
            }
        }
        public static bool Shelve(TycoonState s,int job)
        {
            if(s.paused || job<0 || job>=6 || !EmpireProgression.Unlocked(s,s.active,job) || s.elapsed<s.Current.shelvedUntil[job])return false;
            s.Current.shelvedUntil[job]=s.elapsed+60;s.Current.townResistance[job]=Mathf.Max(0,s.Current.townResistance[job]-20);return true;
        }
        public static bool Push(TycoonState s)
        {
            var c=s.Current;
            if(s.paused || c.lockdown>0 || s.elapsed<c.pushCooldown)return false;
            c.pushUntil=s.elapsed+30;c.pushCooldown=s.elapsed+90;return true;
        }
        public static int CashOut(TycoonState s)
        {
            var c=s.Current;if(s.paused || c.moonshotPot<=0)return 0;
            int payout=Mathf.RoundToInt(c.moonshotPot);c.moonshotPot=0;
            Laundering.Income(s,payout);c.earned+=payout;s.totalJobIncome+=payout;s.scamIncome+=payout;return payout;
        }
        public static void OnShutdown(Company c,Action<string,float> report)
        {
            if(c.moonshotPot<=0)return;
            float lost=c.moonshotPot*(c.potProtected?.5f:1);c.moonshotPot-=lost;
            report("Shutdown: unbanked Moonshot pot lost $"+Mathf.RoundToInt(lost)+". Bank it earlier next time.",0);
        }
        public static bool RequestBreak(TycoonState s,Worker w)
        {
            if(s.paused || !s.Current.workers.Contains(w) || w.breakRemaining>0 || w.breakQueued || w.onCall || w.Held(s))return false;
            w.breakQueued=true;if(w.job<0)BeginBreak(w);return true;
        }
        public static void BeginBreak(Worker w){w.breakQueued=false;w.breakRemaining=12;w.status="Coffee break";}
        public static bool RankReady(TycoonState s) => s.empireRank<5 && Management.ScamProgress(s)>=RankRuns[s.empireRank] && (s.empireRank<2 || s.rivalWins>=s.empireRank-1);
        public static bool ClaimRank(TycoonState s)
        {
            if(s.paused || !RankReady(s))return false;
            s.cash+=new[]{1500,3000,6000,10000,20000}[s.empireRank++];
            foreach(var c in s.companies)c.potProtected=s.empireRank>=4;
            return true;
        }
        public static bool StartRival(TycoonState s)
        {
            if(s.paused || s.rivalActive || s.elapsed<s.rivalCooldown || Management.ScamProgress(s)<25)return false;
            s.rivalActive=true;s.rivalResult=0;s.rivalStartRuns=s.scamsCompleted;
            s.rivalTarget=12+6*Mathf.Min(s.rivalWins,10);s.rivalDeadline=s.elapsed+180;return true;
        }
        public static int RivalProgress(TycoonState s) => Mathf.Clamp(Mathf.FloorToInt((180-Mathf.Max(0,s.rivalDeadline-s.elapsed))/180*s.rivalTarget),0,s.rivalTarget);
        public static int RivalReward(TycoonState s) => 1500+500*Mathf.Min(s.rivalWins,10);
        public static void ResolveRival(TycoonState s,Action<string,float> report)
        {
            if(!s.rivalActive)return;
            if(s.scamsCompleted-s.rivalStartRuns>=s.rivalTarget && s.elapsed<=s.rivalDeadline)
            {
                int reward=RivalReward(s);s.cash+=reward;s.rivalWins++;s.rivalResult=1;Syndicate.OnRaceWon(s);
                s.rivalActive=false;s.rivalCooldown=s.elapsed+60;report("Rival beaten: Velvet Syndicate · +$"+reward,reward);
            }
            else if(s.elapsed>=s.rivalDeadline)
            {s.rivalActive=false;s.rivalResult=2;s.rivalCooldown=s.elapsed+60;report("Velvet Syndicate won this round. No penalty. Retry in 60s.",0);}
        }
        public static bool Renovate(TycoonState s)
        {
            if(s.paused || s.empireRank<3 || !s.harborOwned || s.companies[2].headquarters || !Laundering.PayClean(s,15000))return false;
            s.companies[2].headquarters=true;s.companies[2].deliveryUntil=s.elapsed+10;return true;
        }
        static bool Number(float f) => !float.IsNaN(f) && !float.IsInfinity(f) && f>=0;
        public static bool Valid(TycoonState s)
        {
            if(s.townVersion<0 || s.townVersion>1)return false;
            if(s.townVersion==0)return true;
            if(s.empireRank<0 || s.empireRank>5 || s.rivalWins<0 || s.rivalResult<0 || s.rivalResult>2 || !Number(s.rivalDeadline) || !Number(s.rivalCooldown))return false;
            if(s.rivalActive && (s.rivalStartRuns<0 || s.rivalStartRuns>s.scamsCompleted || s.rivalTarget!=12+6*Mathf.Min(s.rivalWins,10)))return false;
            for(int office=0;office<s.companies.Length;office++)
            {
                var c=s.companies[office];
                if(c.townResistance==null || c.townResistance.Length!=6 || c.shelvedUntil==null || c.shelvedUntil.Length!=6)return false;
                if(!Number(c.moonshotPot) || !Number(c.pushUntil) || !Number(c.pushCooldown) || !Number(c.deliveryUntil) || c.townEvent<0 || c.townEvent>4)return false;
                if(c.headquarters && (office!=2 || !s.harborOwned || s.empireRank<3))return false;
                if(c.potProtected!=(s.empireRank>=4))return false;
                for(int j=0;j<6;j++)if(!Number(c.townResistance[j]) || c.townResistance[j]>100 || !Number(c.shelvedUntil[j]))return false;
                if(c.workers==null)return false;
                foreach(var w in c.workers)
                {
                    if(w==null)return false;
                    if(w.traitVersion!=1 || w.trait<0 || w.trait>=Traits.Length || w.phantomStreak<0 || w.phantomStreak>3 || w.schemeSteps==null || w.schemeSteps.Length!=6)return false;
                    foreach(int step in w.schemeSteps)if(step<0 || step>2)return false;
                    if(!Number(w.breakRemaining) || w.breakRemaining>12 || w.quoteVersion<0 || w.quoteVersion>1)return false;
                    if(w.quoteVersion==1 && (w.job<0 || w.quotedPay<0 || w.quotedPartial<0 || !Number(w.quotedChance) || w.quotedChance>98 || !Number(w.quotedSuspicion)))return false;
                }
            }
            return true;
        }
    }
}
