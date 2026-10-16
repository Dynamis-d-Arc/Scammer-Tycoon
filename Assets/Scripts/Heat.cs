using System;
using UnityEngine;

namespace ScammerTown
{
    // Detective Plodd's case file. Complaints fill it; at 75% a raid is scheduled
    // with a warning so the player can respond. Raids hurt but never take property.
    public static class Heat
    {
        public const string Detective="Detective Dolores Plodd";
        public const float RaidAt=75, WarningSeconds=90, CustodySeconds=240;
        public static readonly string[] Stages={"No file","Complaints piling up","Detective assigned","Closing in","Raid incoming"};
        public static readonly string[] StageNotes={
            "Nobody important is asking questions.",
            "Complaints are landing at the station. The Gazette is sniffing around.",
            Detective+" has opened a file. She is extremely proud of it.",
            "Plodd has a corkboard now. It has red string. You recognize some faces.",
            "Police are on the way. Clean house, or make it worth someone's while."
        };

        public static bool RaidPending(TycoonState s) => s.raidSeconds>=0 && s.raidOffice>=0 && s.raidOffice<s.companies.Length;
        public static int Stage(TycoonState s) => RaidPending(s)?4:s.caseFile>=RaidAt?3:s.caseFile>=50?2:s.caseFile>=25?1:0;
        public static void Add(TycoonState s,float amount) => s.caseFile=Mathf.Clamp(s.caseFile+amount,0,100);
        // Every run adds a little; burner phones make calls harder to trace.
        public static void OnRun(TycoonState s,int office,float heat,bool won) => Add(s,heat*.06f*(s.companies[office].burners>0?.8f:1));
        public static void OnCooldown(TycoonState s) => Add(s,10);

        public static void Advance(TycoonState s,float dt,Func<float> random,Action<string,float> report)
        {
            for(int i=0;i<s.companies.Length;i++)
            {
                if(!Management.Owned(s,i) || s.companies[i].workers==null)continue;
                foreach(var w in s.companies[i].workers)
                    if(w.heldUntil>0 && s.elapsed>=w.heldUntil)
                    {
                        w.heldUntil=0;w.status="Ready";w.loyalty=Mathf.Max(0,w.loyalty-15);
                        report(w.name+" is out of custody. Nobody posted bail. They noticed.",0);
                    }
            }
            if(s.raidSeconds>=0 && !RaidPending(s))s.raidSeconds=-1;
            if(RaidPending(s))
            {
                s.raidSeconds-=dt;
                if(s.raidSeconds<=0)ExecuteRaid(s,random,report);
                return;
            }
            bool calm=true;
            for(int i=0;i<s.companies.Length;i++)if(Management.Owned(s,i) && s.companies[i].suspicion>=40)calm=false;
            if(calm)s.caseFile=Mathf.Max(0,s.caseFile-dt*.03f);
            // A mountain of unlaundered cash is its own kind of evidence.
            if(s.dirty>15000)Add(s,(s.dirty-15000)/10000f*.02f*dt);
            if(s.caseFile>=RaidAt)Schedule(s,report);
        }
        static void Schedule(TycoonState s,Action<string,float> report)
        {
            int target=Management.Owned(s,s.active)?s.active:0;
            for(int i=0;i<s.companies.Length;i++)
                if(Management.Owned(s,i) && s.companies[i].suspicion>s.companies[target].suspicion)target=i;
            s.raidOffice=target;
            s.raidSeconds=WarningSeconds+(OfficeProgression.Placed(s.companies[target],"scanner")?30:0);
            s.evidenceShredded=s.burnersDitched=s.wentDark=false;
            string text="RAID WARNING: "+Detective+" has a warrant for the "+Management.Name(target)+". "+Mathf.CeilToInt(s.raidSeconds)+"s to clean house.";
            report(text,0);Underworld.Announce(s,text);
        }
        public static int ShredCost(TycoonState s) => RaidPending(s) && OfficeProgression.Placed(s.companies[s.raidOffice],"shredder")?0:400;
        public static bool Shred(TycoonState s)
        {
            if(s.paused || !RaidPending(s) || s.evidenceShredded || !Rules.Pay(s,ShredCost(s)))return false;
            s.evidenceShredded=true;Add(s,-10);return true;
        }
        public static bool DitchBurners(TycoonState s)
        {
            if(s.paused || !RaidPending(s) || s.burnersDitched || s.companies[s.raidOffice].burners<=0)return false;
            s.companies[s.raidOffice].burners=0;s.burnersDitched=true;return true;
        }
        public static int BribeCost(TycoonState s) => 2500+1500*s.bribes;
        public static bool Bribe(TycoonState s)
        {
            if(s.paused || !RaidPending(s) || !Rules.Pay(s,BribeCost(s)))return false;
            s.bribes++;s.raidSeconds=-1;s.raidOffice=-1;s.caseFile=40;return true;
        }
        public static bool GoDark(TycoonState s)
        {
            if(s.paused || !RaidPending(s) || s.wentDark)return false;
            var c=s.companies[s.raidOffice];c.lockdown=Mathf.Max(c.lockdown,s.raidSeconds+5);c.pushUntil=0;s.wentDark=true;return true;
        }
        static void ExecuteRaid(TycoonState s,Func<float> random,Action<string,float> report)
        {
            int office=s.raidOffice;var c=s.companies[office];
            float share=(s.evidenceShredded?.25f:.5f)*(s.wentDark?.5f:1);
            int seized=Mathf.FloorToInt(s.dirty*share);Laundering.Seize(s,seized);
            int phones=c.burners;c.burners=0;
            Worker arrested=null;
            // Ditched burners leave nothing to trace; going dark leaves nobody at the desks.
            if(!s.burnersDitched && !s.wentDark)
            {
                var suspects=c.workers.FindAll(w=>!w.Held(s) && Management.HasPlacedDesk(c,w));
                if(suspects.Count>0){arrested=suspects[Mathf.Clamp((int)(random()*suspects.Count),0,suspects.Count-1)];Arrest(s,arrested);}
            }
            bool pallet=false;
            var exhibit=c.layout.Find(item=>item.kind=="cashpallet");
            if(exhibit!=null){c.layout.Remove(exhibit);c.decorations?.RemoveAll(d=>d.id==exhibit.id);pallet=true;}
            LivingTown.OnShutdown(c,report);
            c.lockdown=Mathf.Max(c.lockdown,s.wentDark?20:60);c.suspicion=Mathf.Min(c.suspicion,30);c.pushUntil=0;
            s.caseFile=s.wentDark?25:35;s.raidCount++;s.lastRaidAt=s.elapsed;s.lastRaidOffice=office;
            s.raidSeconds=-1;s.raidOffice=-1;s.evidenceShredded=s.burnersDitched=s.wentDark=false;
            string text="RAID on the "+Management.Name(office)+": police seized $"+seized.ToString("N0")+" in dirty cash"+
                (phones>0?", "+phones+" burner phones":"")+(pallet?", a pallet of cash ('Exhibit A')":"")+
                (arrested!=null?", and took "+arrested.name+" in for questioning":"")+".";
            report(text,0);Underworld.Announce(s,text);
        }
        public static void Arrest(TycoonState s,Worker w)
        {
            w.heldUntil=s.elapsed+CustodySeconds;w.job=-1;w.remaining=0;w.quoteVersion=0;w.onCall=false;w.breakQueued=false;
            w.mood=Mathf.Max(20,w.mood-20);w.loyalty=Mathf.Max(0,w.loyalty-10);w.status="In custody";
        }
        public static int BailCost(Worker w) => 500+w.salary*10;
        public static bool Bail(TycoonState s,int office,Worker w)
        {
            if(s.paused || !Management.Owned(s,office) || !s.companies[office].workers.Contains(w) || !w.Held(s) || !Rules.Pay(s,BailCost(w)))return false;
            w.heldUntil=0;w.status="Ready";w.loyalty=Mathf.Min(100,w.loyalty+10);return true;
        }
        public static int BurnerPackCost => 300;
        public static bool BuyBurners(TycoonState s)
        {
            var c=s.Current;
            if(s.paused || c.burners>=999 || !Rules.Pay(s,BurnerPackCost))return false;
            c.burners=Mathf.Min(999,c.burners+3);return true;
        }
    }
}
