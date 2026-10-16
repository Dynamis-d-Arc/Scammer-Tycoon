using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ScammerTown;
using UnityEngine;

public static class MarksChecks
{
    public static int Run()
    {
        int count=0;Action<bool,string> check=(ok,msg)=>{if(!ok)throw new Exception("MARKS CHECK FAILED: "+msg);count++;};
        var alex=new Worker{name="Alex"};
        var names=new HashSet<string>();
        for(int job=0;job<Management.Jobs.Length;job++)for(int seed=1;seed<=20;seed++)
        {
            var a=Marks.For(seed,job);var b=Marks.For(seed,job);
            check(a.Name==b.Name && a.belief==b.belief && a.type==b.type,"mark is deterministic");
            check(!string.IsNullOrEmpty(a.first) && !string.IsNullOrEmpty(a.last) && !string.IsNullOrEmpty(a.belief),"mark has a name and a belief");
            for(int stage=0;stage<Marks.StageCount;stage++)check(!string.IsNullOrEmpty(Marks.StageLine(alex,a,stage)) && !Marks.StageLine(alex,a,stage).Contains("{"),"hook stage line is filled in");
            check(Marks.Outcome(alex,a,true,220).Contains("$220") && Marks.Outcome(alex,a,false,33).Contains("$33"),"outcome lines include the take");
            check(Marks.Portrait(a).Length==256,"portrait is 16x16");
        }
        for(int seed=1;seed<=60;seed++)names.Add(Marks.For(seed,0).Name);
        check(names.Count>=40,"marks vary across runs");

        // Each run gets a saved mark seed; reloading keeps the same mark.
        var s=new TycoonState();Management.Ensure(s);var w=s.Current.workers[0];
        check(Management.SetAssignment(s,0,w,0) && w.markSeed==1 && s.marksMet==1,"starting a run picks a mark");
        string first=Marks.Current(w).Name;
        var loaded=JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(s));Management.Ensure(loaded);
        check(Rules.Valid(loaded) && Marks.Current(loaded.Current.workers[0]).Name==first,"mark survives a save roundtrip");
        var finished=new List<string>();var messages=new List<string>();
        Action<TycoonState,int,Worker,int,bool,int> listener=(st,office,worker,job,won,pay)=>finished.Add(job+":"+won+":"+pay+":"+worker.markSeed);
        Management.RunFinished+=listener;
        try
        {
            Management.Tick(s,13,()=>0,(m,a)=>messages.Add(m));
            check(finished.Count==1 && finished[0]=="0:True:220:1","run finished event reports the resolved run and its mark");
            check(messages.Exists(m=>m.Contains(first)),"activity log names the mark");
            check(w.job==0 && w.markSeed==2 && s.marksMet==2,"the repeat run moves on to a new mark");
            check(s.cash==4820+220,"marks are cosmetic: payout unchanged");
        }
        finally{Management.RunFinished-=listener;}

        // Chatter: ambient lines come and go; results always get a reaction.
        var c=s.Current;int shown=0;w.reaction=0;
        for(float t=0;t<18;t+=.5f)if(!string.IsNullOrEmpty(Marks.Bark(c,w,0,t)))shown++;
        check(shown>0 && shown<36,"crew chatter appears periodically, not constantly");
        w.reaction=2;w.status="Scam landed! +$220";check(!string.IsNullOrEmpty(Marks.Bark(c,w,0,5)),"successful run gets a reaction");
        w.status="Partial take +$33";check(!string.IsNullOrEmpty(Marks.Bark(c,w,0,5)),"partial run gets a reaction");
        w.reaction=0;c.lockdown=10;
        bool lockdownLine=false;for(float t=0;t<18;t+=.5f)if(!string.IsNullOrEmpty(Marks.Bark(c,w,0,t)))lockdownLine=true;
        check(lockdownLine,"crew still talk during a cooldown");
        c.lockdown=0;

        // The Gazette reacts to the town.
        var town=new TycoonState();Management.Ensure(town);
        check(!string.IsNullOrEmpty(Marks.Headline(town,out string kicker)) && !string.IsNullOrEmpty(kicker),"quiet day still prints a paper");
        town.Current.townResistance[0]=60;check(Marks.Headline(town,out kicker).Contains("JET SKI"),"wary town warns about the overused scheme");
        town.Current.suspicion=50;check(Marks.Headline(town,out kicker).Contains("JET SKI"),"scheme warning outranks moderate suspicion");
        town.Current.suspicion=80;check(Marks.Headline(town,out kicker).Contains("POLICE"),"high suspicion makes the front page");

        // Den gear: placed, capped, small bonuses.
        var gear=new TycoonState{cash=100000};Management.Ensure(gear);var gc=gear.Current;gc.level=3;RoomLayout.Ensure(gc);
        float heat=OfficeProgression.ActualSuspicion(gc,0,true),cooldown=EmpireProgression.BreakSeconds(gc);
        int take=EmpireProgression.Pay(gc,0,1,true),partial=EmpireProgression.Pay(gc,0,1,false);
        var shredder=FurnitureCatalog.BuyDecoration(gear,"shredder");
        check(shredder!=null && gear.cash==100000-350 && !shredder.placed,"den gear is bought into storage");
        check(Mathf.Approximately(OfficeProgression.ActualSuspicion(gc,0,true),heat),"stored shredder gives no bonus");
        check(RoomLayout.AutoPlace(gc,shredder) && Mathf.Abs(OfficeProgression.ActualSuspicion(gc,0,true)-heat*.9f)<.001f,"placed shredder cuts suspicion by 10%");
        RoomLayout.AutoPlace(gc,FurnitureCatalog.BuyDecoration(gear,"shredder"));
        check(Mathf.Abs(OfficeProgression.ActualSuspicion(gc,0,true)-heat*.9f)<.001f,"a second shredder does not stack");
        RoomLayout.AutoPlace(gc,FurnitureCatalog.BuyDecoration(gear,"cashcounter"));
        check(EmpireProgression.Pay(gc,0,1,true)==Mathf.RoundToInt(600*1.05f) && take==600,"cash counter adds 5% to successful takes");
        check(EmpireProgression.Pay(gc,0,1,false)==partial,"cash counter leaves partial takes alone");
        RoomLayout.AutoPlace(gc,FurnitureCatalog.BuyDecoration(gear,"scanner"));
        check(EmpireProgression.BreakSeconds(gc)==cooldown-5,"police scanner shortens automatic cooldowns");
        RoomLayout.AutoPlace(gc,FurnitureCatalog.BuyDecoration(gear,"cashpallet"));
        check(EmpireProgression.Pay(gc,0,1,true)==Mathf.RoundToInt(600*1.05f) && EmpireProgression.BreakSeconds(gc)==cooldown-5 && Mathf.Abs(OfficeProgression.ActualSuspicion(gc,0,true)-heat*.9f)<.001f,"cash pallet is cosmetic");
        var gearSave=JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(gear));Management.Ensure(gearSave);
        check(Rules.Valid(gearSave) && gearSave.Current.layout.Exists(i=>i.kind=="scanner" && i.placed),"den gear survives a save roundtrip");
        var atlas=Resources.Load<Texture2D>("PixelOffice/denprops");
        check(atlas!=null && atlas.isReadable,"den gear atlas imported");
        foreach(var def in FurnitureCatalog.All)if(def.Den)
            check(def.pixels.xMax<=atlas.width && def.pixels.yMax<=atlas.height && atlas.GetPixel((int)def.pixels.center.x,atlas.height-1-(int)def.pixels.center.y).a>.35f,def.kind+" has artwork in the den atlas");

        // Feed, marks board and patrols in the game view.
        var go=new GameObject("Marks UI verification");bool guard=ScammerTownGame.SuppressDiskSaves;ScammerTownGame.SuppressDiskSaves=true;
        Delegate handler=null;
        try
        {
            var game=go.AddComponent<ScammerTownGame>();
            var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            Action<string,object[]> call=(name,args)=>typeof(ScammerTownGame).GetMethod(name,flags).Invoke(game,args);
            Func<string,object> field=name=>typeof(ScammerTownGame).GetField(name,flags).GetValue(game);
            handler=Delegate.CreateDelegate(typeof(Action<TycoonState,int,Worker,int,bool,int>),game,typeof(ScammerTownGame).GetMethod("OnRunFinished",flags));
            Management.RunFinished+=(Action<TycoonState,int,Worker,int,bool,int>)handler;
            var fs=new TycoonState();Management.Ensure(fs);game.state=fs;var fw=fs.Current.workers[0];
            Management.SetAssignment(fs,0,fw,0);Management.Tick(fs,4,()=>0,(m,a)=>{});
            call("UpdateHookFeed",null);
            var feed=(IList)field("hookFeed");
            check(feed.Count==0,"joining mid-run does not flood the feed");
            Management.Tick(fs,9,()=>0,(m,a)=>{});
            check(feed.Count==1,"completed run is announced in the feed");
            call("UpdateHookFeed",null);
            check(feed.Count==2,"the next mark's opening line follows");
            var other=new TycoonState();Management.Ensure(other);Management.SetAssignment(other,0,other.Current.workers[0],0);
            Management.Tick(other,13,()=>0,(m,a)=>{});
            check(feed.Count==2,"runs from another state never reach this feed");
            fs.Current.suspicion=80;call("UpdateHookFeed",null);
            check(feed.Count==3,"rising suspicion shows up in the feed");

            call("SynchronizeRoom",null);
            var board=fs.Current.layout.Find(i=>i.kind=="board");
            check(board!=null && board.Name=="Marks board","the room has a marks board");
            var e=new Event{type=EventType.MouseDown,button=0,mousePosition=new Vector2(500,350)};
            call("HandleRoomPointer",new object[]{e,EventType.MouseDown,true,board,new Vector2(board.x,board.y)});
            e=new Event{type=EventType.MouseUp,button=0,mousePosition=new Vector2(500,350)};
            call("HandleRoomPointer",new object[]{e,EventType.MouseUp,true,null,new Vector2(board.x,board.y)});
            check((bool)field("managementPanel") && (string)field("tab")=="Scams","clicking the marks board opens the Scam Board");

            var patrols=typeof(ScammerTownGame).GetMethod("PatrolCars",flags);
            fs.Current.suspicion=0;check((int)patrols.Invoke(game,null)==0,"no patrols when the town is calm");
            fs.Current.suspicion=50;check((int)patrols.Invoke(game,null)==1,"a patrol car appears as suspicion builds");
            fs.Current.suspicion=90;check((int)patrols.Invoke(game,null)==2,"more patrols when suspicion is high");
        }
        finally
        {
            if(handler!=null)Management.RunFinished-=(Action<TycoonState,int,Worker,int,bool,int>)handler;
            UnityEngine.Object.DestroyImmediate(go);ScammerTownGame.SuppressDiskSaves=guard;
        }
        Debug.Log("SCAMMER_TOWN_MARKS_CHECKS_PASSED: "+count);return count;
    }
}
