using System;
using ScammerTown;
using UnityEngine;

public static class LivingTownChecks
{
    public static int Run()
    {
        int count=0;Action<bool,string> check=(ok,msg)=>{if(!ok)throw new Exception("LIVING TOWN: "+msg);count++;};
        Action<string,float> report=(m,a)=>{};
        Func<TycoonState> fresh=()=>{var s=new TycoonState{cash=100000,scamsCompleted=40,scamVersion=3};Management.Ensure(s);s.Current.computerUpgrade=3;return s;};
        Action<TycoonState,int,bool> run=(s,job,win)=>{var w=s.Current.workers[0];s.Current.lockdown=0;w.assignment=-1;check(Management.Start(s,s.active,w,job),"scheme starts");Management.Tick(s,w.remaining,()=>win?0:.999f,report);};
        var state=fresh();var c=state.Current;var w=c.workers[0];
        check(Rules.Valid(state),"migrated state valid");
        state.Current.level=3;state.Current.desktops=6;state.Current.employeeCount=6;Management.Ensure(state);
        for(int i=0;i<6;i++)check(state.Current.workers[i].trait==i,"six persistent employee traits");
        var copy=JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(state));copy.Current.workers.Reverse();Management.Ensure(copy);
        check(copy.Current.workers[0].trait==5 && Rules.Valid(copy),"traits survive reordered save");
        state=fresh();c=state.Current;w=c.workers[0];
        float baseChance=Management.Chance(c,w,0),baseDuration=Management.Duration(c,w,0,0);
        w.trait=1;check(Management.Chance(c,w,0)==baseChance+5 && Management.Duration(c,w,0,0)>baseDuration,"smooth talker tradeoff affects work");
        w.trait=2;check(Management.Duration(c,w,0,0)<baseDuration && LivingTown.Heat(state,c,w,0)>4,"hustler tradeoff affects work");
        w.trait=3;check(Management.Duration(c,w,0,0)>baseDuration && LivingTown.Heat(state,c,w,0)<4,"perfectionist tradeoff affects work");
        w.trait=4;check(LivingTown.PreviewPay(state,0,w,0,true)==242,"ambitious take bonus");run(state,0,true);check(w.mood==78,"ambitious mood cost is applied");
        state=fresh();c=state.Current;w=c.workers[0];
        run(state,0,true);run(state,0,true);run(state,0,true);
        check(state.scamIncome==220+242+264 && w.phantomStreak==3,"giveaway streak grows actual payouts");
        run(state,0,false);check(w.phantomStreak==0 && w.status.Contains("33"),"partial take resets streak");
        state=fresh();w=state.Current.workers[0];
        run(state,1,true);run(state,1,true);run(state,1,true);
        check(state.scamIncome==2160 && w.schemeSteps[1]==0,"storefront third-run batch bonus");
        state=fresh();w=state.Current.workers[0];
        run(state,3,true);run(state,3,false);check(w.schemeSteps[3]==1,"partial auction retains round");
        run(state,3,true);run(state,3,true);
        check(w.schemeSteps[3]==0 && w.status.Contains("2100"),"auction finale doubles its payout");
        state=fresh();w=state.Current.workers[0];
        run(state,5,true);run(state,5,true);run(state,5,true);
        check(state.scamIncome==12600 && w.schemeSteps[5]==0,"two half-pay preparations followed by 2.5x finale");
        state=fresh();c=state.Current;w=c.workers[0];
        check(LivingTown.PreviewPay(state,0,w,4,true)==1232,"off-season tickets reduced");
        state.elapsed=120;state.day=2;state.lastBillDay=2;Management.Ensure(state);
        check(LivingTown.PreviewPay(state,0,w,4,true)==2175,"festival changes ticket opportunity");
        check(Management.Start(state,0,w,4),"festival job starts");int quoted=w.quotedPay;float heat=w.quotedSuspicion;
        state.elapsed=360;c.dealUpgrade=3;c.operationPace=2;LivingTown.Advance(state,0,report);
        float income=state.scamIncome;Management.Tick(state,w.remaining,()=>0,report);
        check(state.scamIncome-income==quoted && Math.Abs(c.suspicion-heat)<.001f,"calendar and upgrade changes do not rewrite active quotes");
        state=fresh();c=state.Current;w=c.workers[0];c.townResistance[0]=80;
        check(Management.Chance(c,w,0)==baseChance-12 && OfficeProgression.ActualSuspicion(c,0,true)>4,"town resistance penalizes repeated schemes");
        check(LivingTown.Shelve(state,0) && c.townResistance[0]==60,"shelving trades downtime for resistance relief");
        check(!LivingTown.Shelve(state,0) && !Management.Start(state,0,w,0),"shelving cannot be spammed or bypassed");
        Management.SetAssignment(state,0,w,0);Management.Tick(state,30,()=>0,report);
        check(w.job==-1 && c.townResistance[0]<60,"shelved crew waits and resistance recovers");
        Management.Tick(state,30,()=>0,report);check(w.job>=0 || state.scamsCompleted>40,"shelved assignment resumes automatically");
        state=fresh();c=state.Current;w=c.workers[0];
        check(LivingTown.Push(state) && !LivingTown.Push(state),"push has a cooldown");
        check(LivingTown.PreviewPay(state,0,w,0,true)==275 && LivingTown.Heat(state,c,w,0)==7,"push trades pay for suspicion");
        state.paused=true;string paused=JsonUtility.ToJson(state);Management.Tick(state,90,()=>0,report);
        check(JsonUtility.ToJson(state)==paused && !LivingTown.Shelve(state,0) && !LivingTown.RequestBreak(state,w),"pause freezes new mechanics and actions");
        state=fresh();c=state.Current;w=c.workers[0];run(state,2,true);
        check(c.moonshotPot==250 && LivingTown.CashOut(state)==250 && LivingTown.CashOut(state)==0,"pot banks once");
        run(state,2,true);c.suspicion=99;float cash=state.cash;OfficeProgression.CampaignResult(state,0,0,true,report);
        check(c.moonshotPot==0 && state.cash==cash,"shutdown loses unbanked pot only");
        c.moonshotPot=500;state.empireRank=4;Management.Ensure(state);c.suspicion=99;OfficeProgression.CampaignResult(state,0,0,true,report);
        check(c.moonshotPot==250,"rank reward protects half the pot");
        state=fresh();c=state.Current;w=c.workers[0];w.mood=40;
        check(Management.SetAssignment(state,0,w,0) && LivingTown.RequestBreak(state,w),"busy employee queues a break");
        Management.Tick(state,w.remaining,()=>0,report);check(w.breakRemaining==12 && w.job==-1 && w.assignment==0,"break begins after job without losing assignment");
        int runs=state.scamsCompleted;float mood=w.mood;Management.Tick(state,6,()=>0,report);
        check(state.scamsCompleted==runs && w.mood>=mood+9 && w.breakRemaining==6,"break pauses work and restores mood");
        copy=JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(state));Management.Ensure(copy);check(copy.Current.workers[0].breakRemaining==6 && Rules.Valid(copy),"active break persists");
        Management.Tick(state,7,()=>0,report);check(w.job==0 && w.breakRemaining==0,"employee returns to queued work");
        state=fresh();check(LivingTown.ClaimRank(state) && state.empireRank==1 && !LivingTown.ClaimRank(state),"rank claim requires next milestone");
        check(LivingTown.StartRival(state) && !LivingTown.StartRival(state),"one rival challenge at a time");
        state.scamsCompleted+=state.rivalTarget;cash=state.cash;LivingTown.ResolveRival(state,report);
        check(state.rivalWins==1 && state.cash==cash+1500 && !state.rivalActive,"rival reward paid once");LivingTown.ResolveRival(state,report);
        check(state.cash==cash+1500 && !LivingTown.StartRival(state),"rival cannot repay or bypass cooldown");
        state.elapsed=state.rivalCooldown;LivingTown.StartRival(state);state.elapsed=state.rivalDeadline;LivingTown.ResolveRival(state,report);
        check(state.rivalResult==2 && state.rivalWins==1,"missed rival deadline loses without reward");
        state.scamsCompleted=150;check(LivingTown.ClaimRank(state) && LivingTown.ClaimRank(state),"runs plus rival win unlock district rank");
        check(!LivingTown.Renovate(state),"headquarters needs Harbor ownership");state.branchOwned=true;state.harborOwned=true;
        cash=state.cash;check(LivingTown.Renovate(state) && state.cash==cash-15000 && !LivingTown.Renovate(state),"headquarters renovation pays exactly once");
        check(LivingTown.WorkSpeed(state.companies[2],new Worker())==1.1f,"headquarters has gameplay effect");
        copy=JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(state));Management.Ensure(copy);check(Rules.Valid(copy) && copy.companies[2].headquarters,"long-term progress roundtrips");
        copy.Current.townResistance[0]=float.NaN;check(!Rules.Valid(copy),"nonfinite resistance rejected");copy.Current.townResistance[0]=0;
        copy.Current.workers[0].trait=99;check(!Rules.Valid(copy),"unknown personality rejected");
        state=fresh();c=state.Current;w=c.workers[0];var route=OfficeLife.Route(c,w);check(route.Count>0,"starter employee can leave desk");
        for(int i=0;i<route.Count;i++){check(OfficeLife.Open(c,route[i]),"walking stays off furniture");if(i>0)check((route[i]-route[i-1]).sqrMagnitude==1,"walking route has no teleports");}
        check(OfficeLife.Position(route,0)==route[0] && OfficeLife.Position(route,1)==route[0],"walking returns to desk-side tile");
        var texture=Resources.Load<Texture2D>("PixelOffice/crew_walking");check(texture!=null && texture.isReadable,"walking sprites imported");
        for(int look=1;look<=4;look++)for(int frame=0;frame<2;frame++)
        {var uv=OfficeLife.SpriteUV(texture,look,frame);check(uv.width>0 && uv.height>0 && uv.width<.25f && uv.height<.5f,"all walking poses fit transparent cells");}
        return count;
    }
}
