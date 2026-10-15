using System;
using System.Reflection;
using UnityEngine;
using ScammerTown;

public static class IdleChecks
{
    public static int Run()
    {
        int count=0;Action<bool,string> check=(ok,msg)=>{if(!ok)throw new Exception("IDLE CHECK FAILED: "+msg);count++;};
        var s=new TycoonState{cash=0,energy=0,raidSeconds=20};s.Current.heat=100;
        Management.Ensure(s);var w=s.Current.workers[0];w.energy=0;w.resting=true;
        check(s.raidSeconds==-1 && s.Current.heat==0,"legacy heat and active raids cleared without penalty");
        check(Management.SetAssignment(s,0,w,0) && s.cash==0,"assignment can start at zero cash and energy");
        Management.Tick(s,13,()=>0,(m,a)=>{});
        check(s.cash==220 && s.jobsCompleted==1,"assigned job pays normally without energy or heat");
        Management.Tick(s,.1f,()=>0,(m,a)=>{});check(w.job==0,"assignment repeats automatically");
        check(s.cash==220,"repeat has no subscription or job fee");
        check(Management.SetAssignment(s,0,w,1) && w.job==0 && w.assignment==1,"changing assignment queues new job after current one");
        Management.Tick(s,13,()=>0,(m,a)=>{});Management.Tick(s,.1f,()=>0,(m,a)=>{});
        check(w.job==1,"queued assignment starts next");
        w.assignment=-1;Management.Tick(s,25,()=>0,(m,a)=>{});Management.Tick(s,1,()=>0,(m,a)=>{});
        check(w.job==-1,"stop after current job prevents restart");
        Management.SetAssignment(s,0,w,0);var desk=s.Current.layout.Find(i=>i.id==w.station);desk.placed=false;Management.Ensure(s);
        float remaining=w.remaining;Management.Tick(s,5,()=>0,(m,a)=>{});check(w.remaining==remaining,"stored desk pauses job");
        desk.placed=true;Management.Ensure(s);Management.Tick(s,1,()=>0,(m,a)=>{});check(w.remaining<remaining,"replacing desk resumes job");
        var loaded=JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(s));Management.Ensure(loaded);
        check(loaded.Current.workers[0].assignment==0 && Rules.Valid(loaded),"assignment and progress persist");
        loaded.Current.workers[0].assignment=99;check(!Rules.Valid(loaded),"invalid assignment rejected");
        var old=new TycoonState{cash=900};old.Current.ai=true;Management.Ensure(old);
        check(old.Current.workers[0].assignment==0 && old.cash==900,"old automation migrates without charging or losing cash");
        old.Current.workers[0].assignment=-1;Management.Ensure(old);check(old.Current.workers[0].assignment==-1,"migration never re-enables stopped assignments");
        var go=new GameObject("Employee UI verification");ScammerTownGame.SuppressDiskSaves=true;
        try
        {
            var game=go.AddComponent<ScammerTownGame>();game.state=s;
            var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            typeof(ScammerTownGame).GetMethod("OpenWorkerProfile",flags).Invoke(game,new object[]{0});
            check((bool)typeof(ScammerTownGame).GetField("workerModal",flags).GetValue(game),"employee click opens focused profile");
            check((int)typeof(ScammerTownGame).GetField("workerPage",flags).GetValue(game)==0,"profile opens before assignment choices");
            typeof(ScammerTownGame).GetMethod("OpenCity",flags).Invoke(game,null);
            check(!(bool)typeof(ScammerTownGame).GetField("workerModal",flags).GetValue(game),"city navigation closes employee profile");
            typeof(ScammerTownGame).GetMethod("OpenShop",flags).Invoke(game,new object[]{3});
            check((bool)typeof(ScammerTownGame).GetField("shopModal",flags).GetValue(game),"recruitment opens shop catalog");
            check((int)typeof(ScammerTownGame).GetField("shopPage",flags).GetValue(game)==3,"recruitment goes directly to hiring category");
            typeof(ScammerTownGame).GetMethod("OpenWorkerProfile",flags).Invoke(game,new object[]{0});
            check(!(bool)typeof(ScammerTownGame).GetField("shopModal",flags).GetValue(game),"employee profile closes shop to avoid stacked dialogs");
            typeof(ScammerTownGame).GetMethod("Navigate",flags).Invoke(game,new object[]{"Employees"});
            check(!(bool)typeof(ScammerTownGame).GetField("workerModal",flags).GetValue(game) && !(bool)typeof(ScammerTownGame).GetField("cityView",flags).GetValue(game),"team navigation exits city and dialogs");
            check((string)typeof(ScammerTownGame).GetField("tab",flags).GetValue(game)=="Employees","team navigation selects team panel");

        }
        finally {UnityEngine.Object.DestroyImmediate(go);ScammerTownGame.SuppressDiskSaves=false;}
        Debug.Log("SCAMMER_TOWN_IDLE_CHECKS_PASSED: "+count);return count;
    }
}
