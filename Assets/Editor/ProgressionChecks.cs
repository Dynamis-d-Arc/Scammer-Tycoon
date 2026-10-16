using System;
using ScammerTown;
using UnityEngine;
public static class ProgressionChecks
{
    public static int Run()
    {
        int count=0;Action<bool,string> check=(ok,msg)=>{if(!ok)throw new Exception("PROGRESSION: "+msg);count++;};
        var s=new TycoonState();Management.Ensure(s);var w=s.Current.workers[0];
        float cash=s.cash,normal=Management.Duration(s.Current,w,0,0),chance=Management.Chance(s.Current,w,0);
        check(OfficeProgression.Train(s,w,1) && s.cash==cash-300,"training charges exactly once");
        check(!OfficeProgression.Train(s,w,1) && s.cash==cash-300,"same specialty cannot charge twice");
        check(Mathf.Abs(Management.Duration(s.Current,w,0,0)-normal/1.15f)<.001f,"matching specialty is 15 percent faster");
        check(Mathf.Abs(Management.Chance(s.Current,w,0)-chance-5)<.001f,"specialty adds five success points");
        check(Management.Duration(s.Current,w,0,1)==24,"unmatched specialty has no bonus");
        Management.Start(s,0,w,0);check(!OfficeProgression.Train(s,w,2),"busy employee cannot retrain");
        check(Management.Chance(s.Current,w,3)==100,"repairs remain guaranteed");
        s.cash=100000;s.Current.level=4;
        foreach(string kind in new[]{"lamp","shelf","plant","sofa","rug"})for(int i=0;i<4;i++)
        {var item=FurnitureCatalog.BuyDecoration(s,kind);check(item!=null && RoomLayout.AutoPlace(s.Current,item),"decoration purchase and placement available");}
        check(Mathf.Abs(OfficeProgression.DecorSpeed(s.Current)-.08f)<.0001f,"speed decor caps at eight percent");
        check(Mathf.Abs(OfficeProgression.DecorMorale(s.Current)-.14f)<.0001f,"morale decor caps at 0.14 per second");
        foreach(var item in s.Current.layout)if(FurnitureCatalog.Find(item.kind)!=null)item.placed=false;
        check(OfficeProgression.DecorSpeed(s.Current)==0 && OfficeProgression.DecorMorale(s.Current)==0,"storage removes all bonuses");
        s=new TycoonState();Management.Ensure(s);w=s.Current.workers[0];
        check(!OfficeProgression.Accept(s,2),"advanced contract requires five jobs");
        check(OfficeProgression.Accept(s,0) && !OfficeProgression.Accept(s,1),"only one contract accepted");
        var saved=JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(s));
        check(Rules.Valid(saved) && saved.contract.target==5,"active contract survives save roundtrip");
        OfficeProgression.CompleteJob(s,1,0,10,(m,a)=>{});OfficeProgression.CompleteJob(s,0,1,10,(m,a)=>{});
        check(s.contract.progress==0,"wrong office and job do not count");
        Management.SetAssignment(s,0,w,0);cash=s.cash;
        Management.Tick(s,61,()=>0,(m,a)=>{});
        check(s.contract==null && s.contractsCompleted==1 && s.cash==cash+5*220+400,"jobs earn normal pay plus single contract reward");
        OfficeProgression.CompleteJob(s,0,0,61,(m,a)=>{});
        check(s.cash==cash+5*220+400 && !OfficeProgression.Accept(s,0),"reward cannot repeat and cooldown applies");
        s=new TycoonState();Management.Ensure(s);OfficeProgression.Accept(s,0);s.paused=true;
        Management.Tick(s,200,()=>0,(m,a)=>{});check(s.elapsed==0 && s.contract!=null,"pause freezes contract timer");
        s.paused=false;Management.Tick(s,121,()=>0,(m,a)=>{});
        check(s.contract==null && s.contractsCompleted==0,"unfinished contract expires without reward");
        s=new TycoonState();Management.Ensure(s);OfficeProgression.Accept(s,0);
        s.contract.progress=4;cash=s.cash;OfficeProgression.CompleteJob(s,0,0,120.01f,(m,a)=>{});
        check(s.contract.progress==4 && s.cash==cash,"late completion cannot receive reward");
        OfficeProgression.CompleteJob(s,0,0,120,(m,a)=>{});
        check(s.contract==null && s.cash==cash+400,"completion at deadline is accepted");
        saved.contract.reward=999999;check(!Rules.Valid(saved),"invalid contract reward rejected");
        return count;
    }
}
