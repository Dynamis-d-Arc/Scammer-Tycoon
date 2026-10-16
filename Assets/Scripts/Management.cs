using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScammerTown
{
    [Serializable] public class Worker
    {
        public string name, station;
        public int specialty;
        public int appearance;
        public int skill=1, salary=35, completed, job=-1, assignment=-1;
        public float energy=100, mood=80, remaining, duration, reaction;
        public bool resting;
        public bool voucherActive;
        public VoucherRound voucherData;
        public VoucherRound voucher { get => voucherActive?voucherData:null; set {voucherData=value;voucherActive=value!=null;} }
        public float voucherReadyAt;
        public string status="Ready";
        public int trait, traitVersion, phantomStreak;
        public int[] schemeSteps;
        public bool breakQueued;
        public float breakRemaining;
        public int quoteVersion, quotedPay, quotedPartial;
        public float quotedChance, quotedSuspicion;
        // Cosmetic: picks the fictional mark for the current run.
        public int markSeed;
        // The Call cooldown; custody after a raid; loyalty to the crew (0-100).
        public float callReadyAt, heldUntil, loyalty=70;
        public bool courted;
        // Runtime only: the player has taken over this worker's call, so the run pauses.
        [NonSerialized] public bool onCall;
        public bool Held(TycoonState s) => heldUntil>s.elapsed;
    }
    public sealed class JobCard
    {
        public string name, description; public int pay; public float seconds, chance;
        public JobCard(string n,string d,int p,float t,float c) {name=n;description=d;pay=p;seconds=t;chance=c;}
    }
    public static class Management
    {
        public static readonly JobCard[] Jobs={
            new JobCard("Phantom Giveaway","Tell the town's prize hunters they've won. They haven't. Small takes, low suspicion.",220,12,80),
            new JobCard("Mirage Storefront","A gorgeous online shop that ships nothing, ever. Bigger takes, more complaints.",600,24,60),
            new JobCard("Moonshot Club","An 'exclusive' investment club where everyone is a founding member. Big takes, high suspicion.",1250,40,45),
            new JobCard("Velvet Auction","Luxury lots that don't exist, for collectors who can't tell. Steady takes, moderate suspicion.",1050,30,68),
            new JobCard("Echo Tickets","Front-row seats to shows that aren't happening. Fast, risky bursts of income.",1450,22,48),
            new JobCard("Crown Collectibles","'Royal' heirlooms with very official wax seals. Slow, lucrative and conspicuous.",3600,55,40)
        };
        // Fired after a scam run resolves: state, office, worker, job, success, payout.
        public static event Action<TycoonState,int,Worker,int,bool,int> RunFinished;
        public static string Name(int i) => i==0 ? "Basement Apartment" : i==1 ? "Neon Apartments" : "Harbor Loft";
        public static bool Owned(TycoonState s,int i) => i==0 || (i==1 ? s.branchOwned : i==2 && s.harborOwned);
        public static int Rent(int i) => i==0 ? 25 : i==1 ? 100 : 180;
        public static float Speed(int i) => i==0 ? 1 : i==1 ? 1.2f : 1.1f;
        public static string Benefit(int i) => i==0 ? "Low rent · $25/day" : i==1 ? "20% faster jobs · $100/day" : "15% higher payouts · 10% faster · $180/day";
        public static void Ensure(TycoonState s)
        {
            bool migrateRemovedWork=s.scamVersion<2;
            if(s.scamVersion<1)
            {
                // Old saves did not separate scams from repairs. Keep milestone/unlock
                // credit without misreporting those old jobs as newly recorded scams.
                s.legacyScamCredit=Mathf.Clamp(s.jobsCompleted,0,10);
                s.legacyScamIncomeCredit=Mathf.Clamp(s.totalJobIncome,0,1000);
                s.scamVersion=1;
            }
            if(s.companies.Length==2) Array.Resize(ref s.companies,3);
            if(s.companies[2]==null) s.companies[2]=new Company{level=2,neonTheme=true};
            if(s.managementVersion==0) {s.managementVersion=1;s.incidentCompany=-1;s.lastBillDay=s.day;s.nextIncident=s.elapsed+180;}
            bool migrateIdle=s.idleVersion<1;
            if(s.underworldVersion<1)
            {
                // Legacy raid timers predate the detective; never resume them.
                s.raidSeconds=-1;s.raidOffice=-1;s.lastRaidAt=-1;s.lastRaidOffice=-1;s.undercutJob=-1;
                s.underworldVersion=1;
            }
            for(int i=0;i<s.companies.Length;i++)
            {
                var c=s.companies[i];RoomLayout.Ensure(c);
                if(c.workers==null) c.workers=new List<Worker>();
                while(c.workers.Count<c.employeeCount)
                {
                    int n=c.workers.Count;
                    c.workers.Add(new Worker{name=n==0 ? (i==0 ? "Alex" : i==1 ? "Riley" : "Morgan") : new[]{"Sam","Casey","Jordan","Taylor","Avery"}[(n-1)%5]+" "+n,salary=n==0 ? 25 : 35});
                }
                c.employeeCount=c.workers.Count;
                c.heat=0;
                int crewSlot=0;
                foreach(var worker in c.workers)
                {
                    if(worker.appearance<1 || worker.appearance>CrewAppearance.Count)
                        worker.appearance=1+(i+crewSlot)%CrewAppearance.Count;
                    crewSlot++;
                    if(migrateIdle)worker.assignment=c.ai ? 0 : worker.job;
                    if(migrateRemovedWork)
                    {
                        // Preserve crew investment; retired repair specialists become
                        // Phantom specialists, and repeating repairs become Phantom runs.
                        if(worker.specialty==4)worker.specialty=1;
                        if(worker.assignment==3)worker.assignment=0;
                        if(worker.job==3)
                        {
                            worker.job=-1;worker.remaining=worker.duration=worker.reaction=0;
                            worker.status="Ready";
                        }
                    }
                    worker.resting=false;
                    // The voucher minigame was replaced by The Call; open sessions end unpaid.
                    if(worker.voucherActive)worker.voucher=null;
                    worker.loyalty=Mathf.Clamp(worker.loyalty,0,100);
                }
                c.ai=false;
                var occupied=new HashSet<string>();
                foreach(var w in c.workers)
                {
                    // A stored desk still belongs to its worker; replacing it resumes the job.
                    if(w.station!=null && (!c.layout.Exists(it=>it.id==w.station && it.Station) || !occupied.Add(w.station))) w.station=null;
                }
                foreach(var w in c.workers) if(w.station==null)
                {
                    var station=c.layout.Find(it=>it.Station && it.placed && !occupied.Contains(it.id));
                    if(station!=null) {w.station=station.id;occupied.Add(station.id);}
                }
            }
            s.idleVersion=1;
            s.scamVersion=3;
            LivingTown.Ensure(s);
        }
        public static Worker At(Company c,RoomItem item) => c.workers==null ? null : c.workers.Find(w=>w.station==item.id);
        public static bool Assign(Company c,Worker worker,string station)
        {
            if(worker.job>=0 || worker.onCall || !c.workers.Contains(worker) || !c.layout.Exists(it=>it.id==station && it.Station && it.placed)) return false;
            var other=c.workers.Find(w=>w.station==station);
            if(other!=null && other!=worker) {if(other.job>=0 || other.onCall)return false;other.station=worker.station;}
            worker.station=station;return true;
        }
        public static float Chance(Company c,Worker w,int job) => Mathf.Clamp(Jobs[job].chance+(w.skill-1)*3+(job==1 || job==3 || job==5 ? c.productTier-1 : c.employeeTier-1)*3+(w.mood-70)*.12f+2*c.headsetUpgrade+(OfficeProgression.Matches(w,job)?5:0)+OfficeProgression.SkillBonus(c,job)+LivingTown.ChanceBonus(c,w,job),20,98);
        public static float Duration(Company c,Worker w,int company,int job) => Jobs[job].seconds/(Speed(company)*EmpireProgression.Speed(c)*LivingTown.WorkSpeed(c,w)*(1+.12f*c.computerUpgrade+.05f*c.lightingUpgrade+.04f*c.comfortUpgrade+.08f*c.networkUpgrade+OfficeProgression.DecorSpeed(c))*(OfficeProgression.Matches(w,job)?1.15f:1));
        public static bool Start(TycoonState s,int companyIndex,Worker w,int job)
        {
            if(job<0 || job>=Jobs.Length || !Owned(s,companyIndex) || s.paused) return false;
            var c=s.companies[companyIndex];var j=Jobs[job];
            if(!c.workers.Contains(w) || w.job>=0 || w.Held(s) || c.lockdown>0 || !EmpireProgression.Unlocked(s,companyIndex,job)) return false;
            if(!HasPlacedDesk(c,w)) return false;
            if(!LivingTown.CanStart(s,c,w,job))return false;
            // Keep the last result on screen during its reaction; the next run starts quietly.
            w.job=job;w.duration=Duration(c,w,companyIndex,job);w.remaining=w.duration;if(w.reaction<=0)w.status=j.name;
            w.markSeed=++s.marksMet;
            LivingTown.Quote(s,companyIndex,w,job);
            return true;
        }
        public static bool SetAssignment(TycoonState s,int index,Worker worker,int job)
        {
            if(index<0 || index>=s.companies.Length || !Owned(s,index) || s.paused || !EmpireProgression.Unlocked(s,index,job) || !s.companies[index].workers.Contains(worker))return false;
            var c=s.companies[index];
            if(!HasPlacedDesk(c,worker))return false;
            worker.assignment=job;
            if(worker.job<0)Start(s,index,worker,job);
            return true;
        }
        public static int StartIdleTeam(TycoonState s)
        {
            if(s.paused)return 0;
            int started=0;
            foreach(var worker in s.Current.workers)
                if(worker.assignment<0 && worker.job<0 && SetAssignment(s,s.active,worker,0))started++;
            return started;
        }
        public static int ScamProgress(TycoonState s) => s.scamsCompleted+s.legacyScamCredit;
        public static float ScamIncomeProgress(TycoonState s) => s.scamIncome+s.legacyScamIncomeCredit;
        public static bool HasPlacedDesk(Company c,Worker w) => c.layout.Exists(it=>it.id==w.station && it.Station && it.placed);
        public static int AssignScamCrew(TycoonState s,int job)
        {
            if(s.paused || !EmpireProgression.Unlocked(s,s.active,job))return 0;
            int assigned=0;
            foreach(var worker in s.Current.workers)
                if(worker.assignment!=job && HasPlacedDesk(s.Current,worker) && SetAssignment(s,s.active,worker,job))assigned++;
            return assigned;
        }
        public static bool Hire(TycoonState s,int i,int specialist=0)
        {
            if(specialist<0 || specialist>Jobs.Length || (specialist>0 && !EmpireProgression.Unlocked(s,i,specialist-1)))return false;
            var c=s.companies[i];int cost=specialist==0 ? 750 : 1200;
            if(!Owned(s,i) || s.paused || c.employeeCount>=c.Capacity || !Rules.Pay(s,cost))return false;
            c.employeeCount++;Ensure(s);var w=c.workers[c.workers.Count-1];
            if(specialist>0){w.skill=3;w.salary=60;w.name="Robin "+c.employeeCount;w.specialty=specialist;}
            // Queue a scam even without a desk; placing one resumes the new hire.
            w.assignment=0;Start(s,i,w,0);
            return true;
        }
        public static bool BuyHarbor(TycoonState s)
        {
            if(s.harborOwned || !s.branchOwned || ScamProgress(s)<10 || s.paused || !Laundering.PayClean(s,7500))return false;
            s.harborOwned=true;Ensure(s);return true;
        }
        public static bool ClaimGoal(TycoonState s)
        {
            bool done=GoalReady(s);
            if(s.goal>=5 || !done || s.paused)return false;
            s.cash+=GoalReward(s.goal);s.goal++;return true;
        }
        public static int GoalReward(int goal) => new[]{250,300,350,500,750}[Mathf.Clamp(goal,0,4)];
        public static bool GoalReady(TycoonState s) => s.goal==0 ? ScamProgress(s)>=1 : s.goal==1 ? ScamIncomeProgress(s)>=1000 : s.goal==2 ? s.companies[0].Stations>=2 : s.goal==3 ? s.companies[0].employeeCount>=2 : s.goal==4 && s.branchOwned;
        public static string Goal(TycoonState s)
        {
            switch(s.goal)
            {
                case 0:return "First scam: finish a scam run ("+Mathf.Min(1,ScamProgress(s))+"/1)";
                case 1:return "Take $1,000 from scams ($"+Mathf.Min(1000,(int)ScamIncomeProgress(s))+"/1,000)";
                case 2:return "Basement: own 2 workstations ("+Mathf.Min(2,s.companies[0].Stations)+"/2)";
                case 3:return "Basement: hire a second employee ("+Mathf.Min(2,s.companies[0].employeeCount)+"/2)";
                case 4:return "Expand: buy Neon Apartments";
                default:return "Scam empire established! Unlock Harbor with 10 scam runs and $7,500.";
            }
        }
        // All owned properties advance together. Randomness is injected for repeatable tests.
        public static void Tick(TycoonState s,float dt,Func<float> random,Action<string,float> report)
        {
            if(s.paused || dt<=0)return;
            s.elapsed+=dt;s.day=1+(int)(s.elapsed/Rules.DaySeconds);
            LivingTown.Advance(s,dt,report);
            Heat.Advance(s,dt,random,report);
            Laundering.Advance(s,dt);
            for(int i=0;i<s.companies.Length;i++)
            {
                if(!Owned(s,i))continue;
                var c=s.companies[i];float workTime=Mathf.Max(0,dt-c.lockdown);c.lockdown=Mathf.Max(0,c.lockdown-dt);
                bool campaigning=c.workers.Exists(w=>(w.job>=0 && w.job<Jobs.Length) || (w.assignment>=0 && w.assignment<Jobs.Length));
                if(!campaigning || workTime==0)c.suspicion=Mathf.Max(0,c.suspicion-dt*.6f);
                float decorMorale=OfficeProgression.DecorMorale(c);
                foreach(var w in c.workers)
                {
                    w.reaction=Mathf.Max(0,w.reaction-dt);
                    w.mood=Mathf.Min(100,w.mood+dt*(.03f*c.decorUpgrade+.04f*c.breakUpgrade+decorMorale));
                    float breakTime=Mathf.Min(dt,w.breakRemaining);
                    w.breakRemaining=Mathf.Max(0,w.breakRemaining-dt);
                    if(breakTime>0)w.mood=Mathf.Min(100,w.mood+breakTime*(1.5f+.25f*c.breakUpgrade));
                    // A run the player has taken over waits for the call; custody stops work.
                    float available=c.lockdown>0 || w.onCall || w.Held(s)?0:Mathf.Max(0,workTime-breakTime);
                    while(available>0)
                    {
                        if(w.job<0 && (w.assignment<0 || !Start(s,i,w,w.assignment)))break;
                        if(!HasPlacedDesk(c,w)) break;
                        float spent=Mathf.Min(available,w.remaining);
                        w.remaining-=spent;available-=spent;if(w.remaining>0)break;
                        bool won=random()*100<(w.quoteVersion>0?w.quotedChance:Chance(c,w,w.job));
                        if(ResolveRun(s,i,w,won,1,1,s.elapsed-available,report))break;
                        if(w.breakRemaining>0)break;
                    }
                }
            }
            OfficeProgression.Expire(s,report);
            LivingTown.ResolveRival(s,report);
            if(s.day>s.lastBillDay)
            {
                float bill=0;for(int i=0;i<s.companies.Length;i++)if(Owned(s,i)) {bill+=Rent(i);foreach(var w in s.companies[i].workers)bill+=w.salary;}
                float paid=Mathf.Min(bill,s.cash);Laundering.Spend(s,paid);s.lastBillDay=s.day;
                if(paid<bill) foreach(var c in s.companies)foreach(var w in c.workers)w.mood=Mathf.Max(30,w.mood-10);
                report("Daily rent + wages: $"+paid+" / $"+bill+(paid<bill ? ". Shortfall waived; morale -10." : "."),0);
                Crew.Daily(s,paid<bill,random,report);
            }
            Syndicate.Advance(s,random,report);
            if(s.elapsed>=s.nextIncident && s.incidentCompany<0)
            {
                s.nextIncident=s.elapsed+180;
                report("Connection check handled automatically. Your team keeps working.",0);
            }
            if(s.incidentCompany>=0)
            {
                s.incidentCompany=-1;s.incidentCountdown=0;
                report("Previous connection alert cleared automatically.",0);
            }
        }
        // Pays out and records one finished run; shared by the idle loop and The Call.
        // Returns true when the result tips the office into an automatic cooldown.
        public static bool ResolveRun(TycoonState s,int i,Worker w,bool won,float payMultiplier,float heatMultiplier,float finishedAt,Action<string,float> report)
        {
            var c=s.companies[i];int job=w.job;var j=Jobs[job];
            int pay=w.quoteVersion>0?(won?w.quotedPay:w.quotedPartial):EmpireProgression.Pay(c,i,job,won);
            pay=Mathf.RoundToInt(pay*payMultiplier);
            Laundering.Income(s,pay);c.earned+=pay;s.totalJobIncome+=pay;s.jobsCompleted++;w.completed++;s.attempts++;if(won)s.wins++;
            s.scamsCompleted++;s.scamIncome+=pay;
            w.skill=Mathf.Max(w.skill,Mathf.Min(5,1+w.completed/5));w.mood=Mathf.Clamp(w.mood+(won ? 4 : -3),20,100);
            w.job=-1;w.remaining=0;w.onCall=false;w.status=won ? "Scam landed! +$"+pay : "Partial take +$"+pay;w.reaction=4;
            report(Name(i)+": "+w.name+" → "+Marks.For(w.markSeed,job).Name+" · "+j.name+" · "+w.status,pay);
            OfficeProgression.CompleteJob(s,i,job,finishedAt,report);
            LivingTown.Complete(s,i,w,job,won,pay,report);
            RunFinished?.Invoke(s,i,w,job,won,pay);
            float heat=(w.quoteVersion>0?w.quotedSuspicion:OfficeProgression.ActualSuspicion(c,job,true))*heatMultiplier;
            w.quoteVersion=0;
            if(w.breakQueued)LivingTown.BeginBreak(w);
            heat*=won?1:1+3f/OfficeProgression.SuspicionGain(job);
            Heat.OnRun(s,i,heat,won);
            return OfficeProgression.CampaignResult(s,i,job,won,report,heat);
        }
        // Removes a worker from the crew (poached by the rival); their desk frees up.
        public static void RemoveWorker(Company c,Worker w)
        {
            if(c.workers==null || !c.workers.Remove(w))return;
            c.employeeCount=c.workers.Count;
        }
    }
}
