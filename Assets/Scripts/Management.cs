using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScammerTown
{
    [Serializable] public class Worker
    {
        public string name, station;
        public int skill=1, salary=35, completed, job=-1, assignment=-1;
        public float energy=100, mood=80, remaining, duration, reaction;
        public bool resting;
        public string status="Ready";
    }
    public sealed class JobCard
    {
        public string name, description; public int pay; public float seconds, chance;
        public JobCard(string n,string d,int p,float t,float c) {name=n;description=d;pay=p;seconds=t;chance=c;}
    }
    public static class Management
    {
        public static readonly JobCard[] Jobs={
            new JobCard("Inbox shuffle","A low-risk fictional puzzle shift.",220,12,80),
            new JobCard("Parcel puzzle","Bigger rewards per completed job. Product training helps.",600,24,60),
            new JobCard("Neon pitch","A high-stakes fictional contract. Unlock with 5 jobs.",1250,40,45),
            new JobCard("Community repairs","Guaranteed payout, even without a desk.",120,10,100)
        };
        public static string Name(int i) => i==0 ? "Basement Apartment" : i==1 ? "Neon Apartments" : "Harbor Loft";
        public static bool Owned(TycoonState s,int i) => i==0 || (i==1 ? s.branchOwned : i==2 && s.harborOwned);
        public static int Rent(int i) => i==0 ? 25 : i==1 ? 100 : 180;
        public static float Speed(int i) => i==0 ? 1 : i==1 ? 1.2f : 1.1f;
        public static string Benefit(int i) => i==0 ? "Low rent · $25/day" : i==1 ? "20% faster jobs · $100/day" : "15% higher payouts · 10% faster · $180/day";
        public static void Ensure(TycoonState s)
        {
            if(s.companies.Length==2) Array.Resize(ref s.companies,3);
            if(s.companies[2]==null) s.companies[2]=new Company{level=2,neonTheme=true};
            if(s.managementVersion==0) {s.managementVersion=1;s.incidentCompany=-1;s.lastBillDay=s.day;s.nextIncident=s.elapsed+180;}
            bool migrateIdle=s.idleVersion<1;
            s.raidSeconds=-1;
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
                foreach(var worker in c.workers)
                {
                    if(migrateIdle)worker.assignment=c.ai ? 0 : worker.job;
                    worker.resting=false;
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
        }
        public static Worker At(Company c,RoomItem item) => c.workers==null ? null : c.workers.Find(w=>w.station==item.id);
        public static bool Assign(Company c,Worker worker,string station)
        {
            if(worker.job>=0 || !c.workers.Contains(worker) || !c.layout.Exists(it=>it.id==station && it.Station && it.placed)) return false;
            var other=c.workers.Find(w=>w.station==station);
            if(other!=null && other!=worker) {if(other.job>=0)return false;other.station=worker.station;}
            worker.station=station;return true;
        }
        public static float Chance(Company c,Worker w,int job) => job==3 ? 100 : Mathf.Clamp(Jobs[job].chance+(w.skill-1)*3+(job==1 ? c.productTier-1 : c.employeeTier-1)*3+(w.mood-70)*.12f,20,98);
        public static bool Start(TycoonState s,int companyIndex,Worker w,int job)
        {
            if(job<0 || job>=Jobs.Length || !Owned(s,companyIndex) || s.paused) return false;
            var c=s.companies[companyIndex];var j=Jobs[job];
            if(!c.workers.Contains(w) || w.job>=0 || c.lockdown>0 || (job==2 && s.jobsCompleted<5)) return false;
            if(job!=3 && !c.layout.Exists(it=>it.id==w.station && it.placed && it.Station)) return false;
            w.job=job;w.duration=j.seconds/(Speed(companyIndex)*(1+.12f*c.computerUpgrade+.05f*c.lightingUpgrade+.04f*c.comfortUpgrade));w.remaining=w.duration;w.status=j.name;
            return true;
        }
        public static bool SetAssignment(TycoonState s,int index,Worker worker,int job)
        {
            if(index<0 || index>=s.companies.Length || !Owned(s,index) || s.paused || job<0 || job>=Jobs.Length || !s.companies[index].workers.Contains(worker) || (job==2 && s.jobsCompleted<5))return false;
            var c=s.companies[index];
            if(job!=3 && !c.layout.Exists(it=>it.id==worker.station && it.Station && it.placed))return false;
            worker.assignment=job;
            if(worker.job<0)Start(s,index,worker,job);
            return true;
        }
        public static bool Hire(TycoonState s,int i,int specialist=0)
        {
            var c=s.companies[i];int cost=specialist==0 ? 750 : 1200;
            if(!Owned(s,i) || s.paused || c.employeeCount>=c.Capacity || !Rules.Pay(s,cost))return false;
            c.employeeCount++;Ensure(s);var w=c.workers[c.workers.Count-1];
            if(specialist>0){w.skill=3;w.salary=60;w.name="Robin "+c.employeeCount;}
            return true;
        }
        public static bool BuyHarbor(TycoonState s)
        {
            if(s.harborOwned || !s.branchOwned || s.jobsCompleted<10 || s.paused || !Rules.Pay(s,7500))return false;
            s.harborOwned=true;Ensure(s);return true;
        }
        public static bool ClaimGoal(TycoonState s)
        {
            bool done=s.goal==0 ? s.jobsCompleted>=1 : s.goal==1 ? s.totalJobIncome>=1000 : s.goal==2 ? s.companies[0].Stations>=2 : s.goal==3 ? s.companies[0].employeeCount>=2 : s.goal==4 && s.branchOwned;
            if(s.goal>=5 || !done || s.paused)return false;
            s.cash+=GoalReward(s.goal);s.goal++;return true;
        }
        public static int GoalReward(int goal) => new[]{250,300,350,500,750}[Mathf.Clamp(goal,0,4)];
        public static string Goal(TycoonState s)
        {
            switch(s.goal)
            {
                case 0:return "First shift: finish a job ("+Mathf.Min(1,s.jobsCompleted)+"/1)";
                case 1:return "Earn $1,000 from jobs ($"+Mathf.Min(1000,(int)s.totalJobIncome)+"/1,000)";
                case 2:return "Basement: own 2 workstations ("+Mathf.Min(2,s.companies[0].Stations)+"/2)";
                case 3:return "Basement: hire a second employee ("+Mathf.Min(2,s.companies[0].employeeCount)+"/2)";
                case 4:return "Expand: buy Neon Apartments";
                default:return "First-day goals complete! Unlock Harbor with 10 jobs and $7,500.";
            }
        }
        // All owned properties advance together. Randomness is injected for repeatable tests.
        public static void Tick(TycoonState s,float dt,Func<float> random,Action<string,float> report)
        {
            if(s.paused || dt<=0)return;
            s.elapsed+=dt;s.day=1+(int)(s.elapsed/Rules.DaySeconds);
            s.recoveryCooldown=Mathf.Max(0,s.recoveryCooldown-dt);
            if(s.recoveryRemaining>0)
            {
                s.recoveryRemaining=Mathf.Max(0,s.recoveryRemaining-dt);
                if(s.recoveryRemaining==0){s.cash+=100;s.recoveryCooldown=10;report("Odd job finished · +$100.",100);}
            }
            for(int i=0;i<s.companies.Length;i++)
            {
                if(!Owned(s,i))continue;
                var c=s.companies[i];float workTime=Mathf.Max(0,dt-c.lockdown);c.lockdown=Mathf.Max(0,c.lockdown-dt);
                foreach(var w in c.workers)
                {
                    w.reaction=Mathf.Max(0,w.reaction-dt);
                    w.mood=Mathf.Min(100,w.mood+dt*.03f*c.decorUpgrade);
                    float available=workTime;
                    while(available>0)
                    {
                        if(w.job<0 && (w.assignment<0 || !Start(s,i,w,w.assignment)))break;
                        if(w.job!=3 && !c.layout.Exists(it=>it.id==w.station && it.placed && it.Station)) break;
                        float spent=Mathf.Min(available,w.remaining);
                        w.remaining-=spent;available-=spent;if(w.remaining>0)break;
                        int job=w.job;var j=Jobs[job];bool won=random()*100<Chance(c,w,job);
                        int pay=won ? Mathf.RoundToInt(j.pay*(i==2 ? 1.15f : 1)) : Mathf.RoundToInt(j.pay*.15f);
                        s.cash+=pay;c.earned+=pay;s.totalJobIncome+=pay;s.jobsCompleted++;w.completed++;s.attempts++;if(won)s.wins++;
                        w.skill=Mathf.Max(w.skill,Mathf.Min(5,1+w.completed/5));w.mood=Mathf.Clamp(w.mood+(won ? 4 : -3),20,100);
                        w.job=-1;w.remaining=0;w.status=won ? "Success! +$"+pay : "Partial result +$"+pay;w.reaction=4;
                        report(Name(i)+": "+w.name+" · "+w.status,pay);
                    }
                }
            }
            if(s.day>s.lastBillDay)
            {
                float bill=0;for(int i=0;i<s.companies.Length;i++)if(Owned(s,i)) {bill+=Rent(i);foreach(var w in s.companies[i].workers)bill+=w.salary;}
                float paid=Mathf.Min(bill,s.cash);s.cash-=paid;s.lastBillDay=s.day;
                if(paid<bill) foreach(var c in s.companies)foreach(var w in c.workers)w.mood=Mathf.Max(30,w.mood-10);
                report("Daily rent + wages: $"+paid+" / $"+bill+(paid<bill ? ". Shortfall waived; morale -10. Free recovery work available." : "."),0);
            }
            if(s.elapsed>=s.nextIncident && s.incidentCompany<0)
            {
                s.incidentCompany=s.active;s.incidentCountdown=20;s.nextIncident=s.elapsed+180;
                report(Name(s.active)+": suspicious traffic detected. 20 seconds to secure or isolate the connection.",0);
            }
            if(s.incidentCompany>=0)
            {
                s.incidentCountdown-=dt;
                if(s.incidentCountdown<=0)
                {
                    var c=s.companies[s.incidentCompany];bool blocked=c.lockdown>0 || random()*100<Rules.Chance(c.securityTier);
                    float lost=blocked ? 0 : Mathf.Min(200,s.cash*.1f);s.cash-=lost;
                    report(blocked ? "Security blocked the incident." : "Unresolved incident: lost $"+Mathf.FloorToInt(lost)+" (10% of cash, capped at $200).",0);s.incidentCompany=-1;
                }
            }
        }
    }
}
