using System;
using ScammerTown;
using UnityEngine;

public static class EmpireChecks
{
    public static int Run()
    {
        int count=0;Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception("EMPIRE: "+message);count++;};
        Func<TycoonState> fresh=()=>{var s=new TycoonState{cash=100000};Management.Ensure(s);return s;};
        var state=fresh();var c=state.Current;var worker=c.workers[0];
        check(Management.Jobs.Length==6,"six schemes available");
        for(int job=3;job<6;job++)
        {
            state=fresh();c=state.Current;worker=c.workers[0];float cash=state.cash;
            check(!Management.Start(state,0,worker,job) && !Management.SetAssignment(state,0,worker,job) && Management.AssignScamCrew(state,job)==0,"all assignment entry points respect advanced locks");
            check(!OfficeProgression.Accept(state,job) && !Management.Hire(state,0,job+1) && !OfficeProgression.Train(state,worker,job+1) && state.cash==cash,"locked contracts and specialists cannot charge");
            state.scamsCompleted=EmpireProgression.RequiredRuns(job);
            check(!EmpireProgression.Unlocked(state,0,job),"runs alone cannot bypass computer requirement");
            c.computerUpgrade=EmpireProgression.RequiredComputer(job);state.scamsCompleted--;
            check(!EmpireProgression.Unlocked(state,0,job),"equipment alone cannot bypass run requirement");
            state.scamsCompleted++;
            check(OfficeProgression.Train(state,worker,job+1) && OfficeProgression.Accept(state,job),"matching specialist and contract unlock together");
            check(Management.AssignScamCrew(state,job)==1 && worker.job==job,"advanced scheme starts from board");
            worker.assignment=-1;cash=state.cash;int completed=state.scamsCompleted;
            float expected=worker.quotedPay;
            Management.Tick(state,worker.remaining,()=>0,(m,a)=>{});
            check(state.scamsCompleted==completed+1 && state.cash==cash+expected && state.scamIncome==expected,"new scheme pays and counts exactly once");
            check(c.suspicion>0 && state.contract.progress==1,"new scheme affects suspicion and its contract");
            var saved=JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(state));Management.Ensure(saved);
            check(Rules.Valid(saved) && saved.Current.workers[0].specialty==job+1 && saved.contract.job==job,"new specialists and contracts roundtrip");
            cash=saved.cash;
            for(int j=1;j<saved.contract.target;j++)OfficeProgression.CompleteJob(state,0,job,state.elapsed,(m,a)=>{});
            // Complete the remaining target on the live state without letting its timer run out.
            check(!state.contractActive && state.cash==cash+EmpireProgression.ContractReward(job),"advanced contract pays its reward once");
            check(Management.Hire(saved,0,job+1),"advanced specialist recruitment works after unlock");
        }
        state=fresh();c=state.Current;worker=c.workers[0];
        float baseChance=Management.Chance(c,worker,0),baseDuration=Management.Duration(c,worker,0,0);
        for(int upgrade=0;upgrade<4;upgrade++)
        {
            state.paused=true;float cash=state.cash;
            check(!EmpireProgression.Buy(state,upgrade) && state.cash==cash,"pause blocks new upgrades");state.paused=false;
            state.cash=0;check(!EmpireProgression.Buy(state,upgrade) && EmpireProgression.Level(c,upgrade)==0,"cash required for new upgrades");state.cash=cash;
            for(int level=1;level<=3;level++)
            {
                cash=state.cash;int cost=EmpireProgression.Cost(c,upgrade);
                check(EmpireProgression.Buy(state,upgrade) && EmpireProgression.Level(c,upgrade)==level && state.cash==cash-cost,"upgrade has exact cost and one level per purchase");
            }
            cash=state.cash;check(!EmpireProgression.Buy(state,upgrade) && state.cash==cash,"max level cannot charge again");
        }
        check(!EmpireProgression.Buy(state,-1) && !EmpireProgression.Buy(state,4),"invalid upgrade rejected");
        check(Management.Chance(c,worker,0)==baseChance+6 && Management.Duration(c,worker,0,0)<baseDuration,"headsets and network affect real job calculations");
        check(EmpireProgression.Pay(c,0,0,true)>Management.Jobs[0].pay && OfficeProgression.ActualSuspicion(c,0,true)>4,"deal room trades larger payout for suspicion");
        worker.mood=50;Management.Tick(state,10,()=>0,(m,a)=>{});
        check(worker.mood>51,"break room restores worker mood");
        c.suspicion=99;OfficeProgression.CampaignResult(state,0,0,true,(m,a)=>{});
        check(c.lockdown==21,"break room shortens automatic breaks");c.lockdown=0;c.suspicion=0;
        float balancedTime=Management.Duration(c,worker,0,0),balancedHeat=OfficeProgression.ActualSuspicion(c,0,true);
        check(EmpireProgression.SetPace(state,1) && Management.Duration(c,worker,0,0)>balancedTime && OfficeProgression.ActualSuspicion(c,0,true)<balancedHeat,"cautious style slows work and suspicion");
        check(EmpireProgression.SetPace(state,2) && Management.Duration(c,worker,0,0)<balancedTime && OfficeProgression.ActualSuspicion(c,0,true)>balancedHeat,"pressure style speeds work and suspicion");
        state.paused=true;check(!EmpireProgression.SetPace(state,0) && c.operationPace==2,"pause prevents style change");state.paused=false;
        check(!EmpireProgression.SetPace(state,3) && !EmpireProgression.SetPace(state,-1),"unknown styles rejected");
        state.scamsCompleted=50;c.computerUpgrade=3;
        check(Management.Start(state,0,worker,5),"late scheme starts with upgrades");
        float actualPay=worker.quotedPay,before=state.cash;
        Management.Tick(state,worker.remaining,()=>0,(m,a)=>{});
        check(state.cash==before+actualPay,"deal room and operating style apply to actual payouts");
        var roundtrip=JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(state));Management.Ensure(roundtrip);
        check(Rules.Valid(roundtrip) && roundtrip.Current.operationPace==2 && roundtrip.Current.dealUpgrade==3 && roundtrip.Current.networkUpgrade==3 && roundtrip.Current.breakUpgrade==3 && roundtrip.Current.headsetUpgrade==3,"all new upgrades persist");
        roundtrip.Current.networkUpgrade=4;check(!Rules.Valid(roundtrip),"out of range upgrade rejected");roundtrip.Current.networkUpgrade=3;
        roundtrip.Current.operationPace=3;check(!Rules.Valid(roundtrip),"out of range style rejected");
        state=fresh();c=state.Current;state.scamsCompleted=50;c.computerUpgrade=3;worker=c.workers[0];
        check(Management.SetAssignment(state,0,worker,3),"new index three begins Velvet");
        Management.Tick(state,1,()=>0,(m,a)=>{});float remaining=worker.remaining;
        Management.Ensure(state);check(worker.job==3 && worker.assignment==3 && worker.remaining==remaining,"version three migration never retires Velvet as old repairs");
        var current=JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(state));Management.Ensure(current);
        check(current.Current.workers[0].job==3 && Rules.Valid(current),"active Velvet persists on load");
        state=fresh();c=state.Current;
        check(OfficeProgression.UnlockSkill(state,0),"advanced skill root purchase");
        for(int job=3;job<6;job++)
        {
            int focus=OfficeProgression.FocusFor(job),expert=OfficeProgression.ExpertiseFor(job);
            check(!OfficeProgression.UnlockSkill(state,expert),"advanced expertise needs its focus");
            check(OfficeProgression.UnlockSkill(state,focus) && OfficeProgression.UnlockSkill(state,expert) && OfficeProgression.SkillBonus(c,job)==12,"new campaign branches affect matching scheme");
        }
        check(OfficeProgression.SkillBonus(c,0)==3,"advanced skills do not leak into original branches");
        check(Rules.Valid(JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(state))),"extended skill bits survive save validation");
        c.campaignSkills=1<<14;check(!Rules.Valid(state),"unknown extended skill rejected");
        return count;
    }
}
