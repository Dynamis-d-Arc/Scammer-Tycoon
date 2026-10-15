using System;
using UnityEngine;
using ScammerTown;

public static class ManagementChecks
{
    public static int Run()
    {
        int count=0;Action<bool,string> check=(ok,msg)=>{if(!ok)throw new Exception("MANAGEMENT CHECK FAILED: "+msg);count++;};
        Action<string,float> report=(msg,cash)=>{};
        Func<TycoonState> fresh=()=>{var s=new TycoonState();Rules.MigrateStaff(s);Management.Ensure(s);return s;};
        var state=fresh();var c=state.Current;var alex=c.workers[0];
        check(alex.name=="Alex" && alex.station!=null,"starter has profile and assigned desk");
        check(state.companies.Length==3 && !Management.Owned(state,2),"harbor exists but starts locked");
        check(Rules.Valid(state),"migrated save validates");
        var old=new TycoonState{cash=0,day=180,elapsed=179*120,branchOwned=true};old.companies[0].employeeCount=3;old.companies[0].employeeTier=4;
        Management.Ensure(old);Management.Ensure(old);
        check(old.cash==0 && old.Current.workers.Count==3 && old.Current.employeeTier==4 && old.lastBillDay==180,"migration preserves progress and does not backbill");
        check(!Management.Start(state,0,alex,2),"high-risk job locked until five completions");
        check(Management.Start(state,0,alex,0),"starter can begin first job");
        check(!Management.Start(state,0,alex,0),"busy employee cannot take a second job");
        float cash=state.cash,remaining=alex.remaining;state.paused=true;Management.Tick(state,20,()=>0,report);
        check(alex.remaining==remaining && state.cash==cash,"pause freezes jobs");state.paused=false;
        Management.Tick(state,13,()=>0,report);
        check(state.jobsCompleted==1 && state.cash==cash+220 && alex.job==-1,"job completes and pays once");
        Management.Tick(state,1,()=>0,report);check(state.cash==cash+220,"finished job cannot repay");
        check(c.heat==0,"jobs do not generate heat");
        check(Management.ClaimGoal(state) && state.goal==1,"first-job goal claim");
        check(!Management.ClaimGoal(state),"goal cannot be claimed before next condition");
        state.totalJobIncome=1000;check(Management.ClaimGoal(state),"income goal claim");
        RoomLayout.Expand(c); // Leave room for the second desk alongside the movable furnishings.
        c.desktops=2;Management.Ensure(state);check(Management.ClaimGoal(state),"second workstation goal");
        check(Management.Hire(state,0) && c.workers.Count==2,"hiring creates persistent employee");
        check(Management.ClaimGoal(state),"second employee goal");
        var other=c.workers[1];string desk=other.station;
        check(Management.Assign(c,alex,desk) && other.station!=alex.station,"desk reassignment swaps without duplicate occupancy");
        Management.Start(state,0,alex,0);check(!Management.Assign(c,other,alex.station),"busy workers cannot be displaced");
        var roundtrip=JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(state));
        check(Rules.Valid(roundtrip) && roundtrip.Current.workers[0].job==0 && roundtrip.goal==4,"profiles and in-progress jobs roundtrip");
        state=fresh();state.cash=9000;Rules.BuyApartment(state);Management.Ensure(state);
        var baseWorker=state.companies[0].workers[0];var neonWorker=state.companies[1].workers[0];
        Management.Start(state,0,baseWorker,0);Management.Start(state,1,neonWorker,0);
        check(neonWorker.duration<baseWorker.duration,"neon completes jobs faster");
        Rules.VisitApartment(state,1);Management.Tick(state,13,()=>0,report);
        check(state.companies[0].earned==220 && state.companies[1].earned==220,"both properties earn while player visits one");
        state.companies[0].ai=true;Rules.VisitApartment(state,0);Rules.VisitApartment(state,1);
        check(state.companies[0].ai,"switching preserves background automation");
        state.cash=0;Management.Tick(state,1,()=>0,report);check(baseWorker.job==-1 && state.cash==0,"automation cannot overspend");
        state.companies[0].ai=false;state.recoveryRemaining=10;Management.Tick(state,10,()=>0,report);check(state.cash==100,"zero-fund recovery works");
        var lost=new TycoonState{cash=0};Management.Ensure(lost);lost.Current.desktops=0;Management.Ensure(lost);
        check(Management.Start(lost,0,lost.Current.workers[0],3),"repair job works without equipment");
        Management.Tick(lost,11,()=>.99f,report);check(lost.cash==120,"recovery job guaranteed payout");
        var upgraded=fresh();upgraded.Current.computerUpgrade=3;upgraded.Current.lightingUpgrade=3;
        Management.Start(upgraded,0,upgraded.Current.workers[0],0);check(upgraded.Current.workers[0].duration<12,"equipment improves job speed");
        var bill=fresh();bill.cash=1;Management.Tick(bill,121,()=>0,report);
        check(bill.cash==0 && bill.Current.workers[0].mood>=30,"unpaid rent cannot create debt or deadlock");
        var threat=fresh();threat.nextIncident=0;Management.Tick(threat,1,()=>.99f,report);
        check(threat.incidentCompany==0 && threat.cash==4820,"incident warns before taking cash");
        threat.Current.lockdown=25;Management.Tick(threat,20,()=>.99f,report);
        check(threat.incidentCompany==-1 && threat.cash==4820,"free isolation blocks threat");
        state=fresh();state.branchOwned=true;state.cash=8000;state.jobsCompleted=10;
        check(Management.BuyHarbor(state) && state.cash==500,"harbor unlock and purchase");
        check(!Management.BuyHarbor(state),"no duplicate property charge");
        check(Rules.VisitApartment(state,2) && state.active==2,"third property navigation");
        check(Rules.Valid(JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(state))),"third property save loads");
        state.Current.workers[0].mood=float.NaN;check(!Rules.Valid(state),"invalid worker data rejected");
        Debug.Log("SCAMMER_TOWN_MANAGEMENT_CHECKS_PASSED: "+count);return count;
    }
}
