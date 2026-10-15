using System;
using ScammerTown;
using UnityEngine;

public static class CityChecks
{
    public static int Run()
    {
        int count=0;
        Action<bool,string> check=(ok,message)=>{if(!ok)throw new Exception("CITY CHECK FAILED: "+message);count++;};
        var s=new TycoonState();Rules.MigrateStaff(s);
        check(s.Current.employeeCount==1,"new game has a starter employee");
        check(s.cash==4820,"starter employee is free");
        s.Current.employeeCount=3;Rules.MigrateStaff(s);
        check(s.Current.employeeCount==3,"migration is idempotent");
        var old=new TycoonState {staffVersion=0,cash=2020};old.Current.employeeCount=0;old.Current.employeeTier=3;
        Rules.MigrateStaff(old);
        check(old.Current.employeeCount==1 && old.Current.employeeTier==3 && old.cash==2020,"old save gains starter without losing progress");
        check(!Rules.VisitApartment(s,1) && s.active==0,"unowned apartment cannot be visited");
        check(!Rules.VisitApartment(s,-1) && !Rules.VisitApartment(s,2),"invalid destination rejected");
        s.cash=2999;check(!Rules.BuyApartment(s) && s.cash==2999 && !s.branchOwned,"insufficient funds do not mutate ownership");
        s.cash=4820;check(Rules.BuyApartment(s) && s.cash==1820 && s.branchOwned,"apartment costs exactly 3000");
        check(s.companies[1].employeeCount==1 && s.companies[1].desktops==1,"new apartment can run operations immediately");
        check(s.active==0,"purchase does not unexpectedly switch active apartment");
        check(!Rules.BuyApartment(s) && s.cash==1820,"duplicate purchase is blocked");
        s.Current.heat=73;s.Current.employeeTier=4;s.Current.ai=true;s.cycle=8;
        RoomLayout.Ensure(s.Current);
        foreach(var item in s.Current.layout)if(FurnitureCatalog.IsFurnishing(item))item.placed=false;
        var fridge=s.Current.layout.Find(i=>i.id=="fridge");RoomLayout.Move(s.Current,fridge,3,4,1);
        check(Rules.VisitApartment(s,1) && s.active==1 && s.neon,"visit switches apartment and theme");
        check(s.companies[0].ai && s.cycle==0,"leaving preserves background automation and resets legacy cycle");
        check(s.Current.heat==0 && s.Current.employeeTier==1,"apartment progression is independent");
        check(Rules.VisitApartment(s,0) && s.Current.heat==73 && s.Current.employeeTier==4,"return restores old progression");
        check(fridge.x==3 && fridge.y==4 && fridge.rotation==1,"visits preserve customized layout");
        s.paused=true;check(Rules.VisitApartment(s,1),"visiting works while paused");
        var locked=new TycoonState {paused=true};check(!Rules.BuyApartment(locked),"purchase blocked while paused");
        s.raidSeconds=20;check(Rules.VisitApartment(s,0) && s.active==0,"legacy raid data no longer blocks city navigation");
        locked.paused=false;locked.raidSeconds=30;check(Rules.BuyApartment(locked),"legacy raid data no longer blocks purchase");
        var c=new Company {desktops=4,employeeCount=1};check(Rules.StaffedStations(c)==1,"AI requires staff per workstation");
        c.employeeCount=3;check(Rules.StaffedStations(c)==3,"hiring increases staffed station count");
        c.employeeCount=5;check(Rules.StaffedStations(c)==4,"AI capped by available equipment");
        c.desktops=40;c.employeeCount=40;check(Rules.StaffedStations(c)==20,"AI cycle limit retained");
        s.raidSeconds=-1;s.active=1;
        var saved=JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(s));
        check(Rules.Valid(saved) && saved.branchOwned && saved.active==1 && saved.companies[0].employeeCount==3,"ownership, staff and selected apartment survive save/load");
        var room=new Company();RoomLayout.Ensure(room);
        var first=room.layout.Find(i=>i.Station);
        check(RoomLayout.HasEmployee(room,first),"starter employee is visible at the first placed workstation");
        room.employeeCount=0;check(!RoomLayout.HasEmployee(room,first),"empty crew does not show a worker");
        room.employeeCount=1;first.placed=false;check(!RoomLayout.HasEmployee(room,first),"stored workstations do not show workers");
        room.desktops=2;RoomLayout.Ensure(room);
        var second=room.layout.Find(i=>i.Station && i!=first);
        check(RoomLayout.HasEmployee(room,second),"employee takes next placed desk when first is stored");
        Debug.Log("SCAMMER_TOWN_CITY_CHECKS_PASSED: "+count);
        return count;
    }
}
