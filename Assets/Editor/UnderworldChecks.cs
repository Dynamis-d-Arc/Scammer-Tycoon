using System;
using System.Collections;
using System.Reflection;
using ScammerTown;
using UnityEngine;

// Phases 2–5: The Call, the detective and raids, dirty money and fronts, crew
// loyalty and the Velvet Syndicate.
public static class UnderworldChecks
{
    static int count;
    public static int Run()
    {
        count=0;Action<bool,string> check=(ok,msg)=>{if(!ok)throw new Exception("UNDERWORLD CHECK FAILED: "+msg);count++;};
        Action<string,float> report=(m,a)=>{};
        Func<TycoonState> fresh=()=>{var s=new TycoonState();Management.Ensure(s);return s;};
        CallChecks(check,report,fresh);
        HeatChecks(check,report,fresh);
        MoneyChecks(check,report,fresh);
        CrewChecks(check,report,fresh);
        UiChecks(check,fresh);
        Debug.Log("SCAMMER_TOWN_UNDERWORLD_CHECKS_PASSED: "+count);return count;
    }

    // Picks the best option this turn (avoiding repeats) or the worst one.
    static int Pick(CallSession call,bool best)
    {
        int choice=0;float chosen=best?float.MinValue:float.MaxValue;
        for(int i=0;i<call.options.Length;i++)
        {
            int a=call.options[i];float score=TheCall.Effect(call.mark,a)*10-(a==call.lastApproach?25:0);
            if(best?score>chosen:score<chosen){chosen=score;choice=i;}
        }
        return choice;
    }

    static void CallChecks(Action<bool,string> check,Action<string,float> report,Func<TycoonState> fresh)
    {
        var s=fresh();var w=s.Current.workers[0];
        check(TheCall.Begin(s,0,w,1)==null,"no call without a running scam");
        s.Current.townResistance[0]=30;
        check(Management.SetAssignment(s,0,w,0),"run starts");
        var call=TheCall.Begin(s,0,w,7);
        check(call!=null && w.onCall && call.trust>0 && call.doubt>10,"taking the call pauses the crew member");
        float remaining=w.remaining;Management.Tick(s,5,()=>0,report);
        check(w.remaining==remaining,"the run waits while you're on the call");
        check(TheCall.Begin(s,0,w,8)==null,"one call at a time");
        check(!JsonUtility.ToJson(s).Contains("onCall"),"live call state is never saved");
        int scams=s.scamsCompleted,quoted=w.quotedPay;float dirty=s.dirty,suspicion=s.Current.suspicion,quotedHeat=w.quotedSuspicion,resistance=s.Current.townResistance[0];
        for(int guard=0;guard<10 && !call.Over;guard++)TheCall.Choose(s,call,Pick(call,true),report);
        check(call.outcome==TheCall.Closed,"good choices close the deal");
        check(s.scamsCompleted==scams+1 && call.payout==Mathf.RoundToInt(quoted*TheCall.CloseMultiplier(call)) && call.payout>=Mathf.RoundToInt(quoted*1.5f),"a close pays 1.5–2× and counts as a scam run");
        check(Mathf.Abs(s.dirty-dirty-call.payout)<.01f,"call proceeds are dirty money");
        check(Mathf.Abs(s.Current.suspicion-suspicion-quotedHeat*.5f)<.01f,"a smooth close draws half the suspicion");
        check(s.Current.townResistance[0]<resistance,"a close calms the town");
        Management.Tick(s,.1f,()=>0,report);
        check(!w.onCall && w.job==0 && TheCall.Begin(s,0,w,9)==null && TheCall.Blocker(s,0,w).Contains("breath"),"a cooldown follows each call");
        check(!TheCall.Choose(s,call,0,report),"a finished call takes no more choices");

        s=fresh();w=s.Current.workers[0];Management.SetAssignment(s,0,w,0);
        call=TheCall.Begin(s,0,w,11);
        suspicion=s.Current.suspicion;quotedHeat=w.quotedSuspicion;int partial=w.quotedPartial;
        for(int guard=0;guard<10 && !call.Over;guard++)TheCall.Choose(s,call,Pick(call,false),report);
        check(call.outcome==TheCall.HungUp && call.payout==partial,"bad choices make the mark hang up for the partial take");
        check(Mathf.Abs(s.Current.suspicion-suspicion-quotedHeat*1.5f*(1+3f/4))<.01f,"a blown call draws extra suspicion");
        check(s.Current.townResistance[0]>=13,"a blown call makes the town warier");

        s=fresh();w=s.Current.workers[0];Management.SetAssignment(s,0,w,0);
        call=TheCall.Begin(s,0,w,12);call.turn=TheCall.MaxTurns-1;call.trust=10;call.doubt=0;
        TheCall.Choose(s,call,Pick(call,false),report);
        check(call.outcome==TheCall.Stalled && !w.onCall && w.job==0,"after six turns the mark stalls and the run carries on");
        Management.Tick(s,13,()=>0,report);check(s.scamsCompleted==1,"a stalled run still finishes normally");

        s=fresh();w=s.Current.workers[0];Management.SetAssignment(s,0,w,0);
        call=TheCall.Begin(s,0,w,13);TheCall.Abandon(s,call);
        check(call.outcome==TheCall.Abandoned && !w.onCall && w.callReadyAt==s.elapsed+20,"handing a call back resumes the run");
        s.elapsed=w.callReadyAt;s.Current.lockdown=10;check(TheCall.Begin(s,0,w,14)==null,"no calls while laying low");
        s.Current.lockdown=0;Heat.Arrest(s,w);check(TheCall.Begin(s,0,w,15)==null,"no calls from custody");

        var repeat=fresh();var rw=repeat.Current.workers[0];Management.SetAssignment(repeat,0,rw,0);
        var a=TheCall.Begin(repeat,0,rw,21);a.options[0]=0;a.lastApproach=0;float doubt=a.doubt;
        TheCall.Choose(repeat,a,0,report);check(a.doubt-doubt>=14,"repeating yourself raises doubt");
    }

    static void HeatChecks(Action<bool,string> check,Action<string,float> report,Func<TycoonState> fresh)
    {
        var s=fresh();var w=s.Current.workers[0];Management.SetAssignment(s,0,w,0);
        Management.Tick(s,12.5f,()=>0,report);check(s.caseFile>0,"every run adds to the case file");
        var plain=fresh();plain.Current.burners=0;Heat.OnRun(plain,0,10,true);
        var burner=fresh();burner.Current.burners=3;Heat.OnRun(burner,0,10,true);
        check(Mathf.Abs(plain.caseFile-.6f)<.001f && Mathf.Abs(burner.caseFile-.48f)<.001f,"burner phones make runs 20% harder to trace");
        var cooldown=fresh();cooldown.Current.suspicion=99;OfficeProgression.CampaignResult(cooldown,0,0,true,report);
        check(Mathf.Abs(cooldown.caseFile-10)<.001f,"each automatic cooldown adds 10 to the case file");
        var calm=fresh();calm.caseFile=50;Management.Tick(calm,10,()=>0,report);
        check(Mathf.Abs(calm.caseFile-49.7f)<.01f,"the file fades while every office is calm");
        var pile=fresh();pile.cash=40000;pile.dirty=35000;pile.caseFile=10;Management.Tick(pile,10,()=>0,report);
        check(pile.caseFile>10,"a mountain of dirty cash draws the detective");

        var r=fresh();r.branchOwned=true;Management.Ensure(r);r.companies[1].suspicion=60;r.companies[0].suspicion=20;r.caseFile=75;
        Management.Tick(r,.1f,()=>0,report);
        check(Heat.RaidPending(r) && r.raidOffice==1 && Mathf.Abs(r.raidSeconds-90)<.01f && Heat.Stage(r)==4,"at 75% a raid is scheduled against the hottest office");
        check(Rules.Valid(JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(r))),"a pending raid saves and loads");
        var bad=JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(r));bad.raidOffice=2;check(!Rules.Valid(bad),"a raid on an unowned office is rejected");
        var scanned=fresh();scanned.Current.level=3;RoomLayout.Ensure(scanned.Current);scanned.cash=100000;
        RoomLayout.AutoPlace(scanned.Current,FurnitureCatalog.BuyDecoration(scanned,"scanner"));scanned.caseFile=80;Management.Tick(scanned,.1f,()=>0,report);
        check(Mathf.Abs(scanned.raidSeconds-120)<.01f,"the police scanner buys 30 more seconds");

        var raid=fresh();raid.Current.level=3;RoomLayout.Ensure(raid.Current);var rw=raid.Current.workers[0];
        raid.cash=10000;raid.dirty=6000;
        var pallet=FurnitureCatalog.BuyDecoration(raid,"cashpallet");RoomLayout.AutoPlace(raid.Current,pallet);
        raid.caseFile=80;Management.Tick(raid,.1f,()=>0,report);
        float clean=Laundering.Clean(raid),dirty=raid.dirty;int desks=raid.Current.desktops;
        Management.Tick(raid,89.8f,()=>0,report);check(Heat.RaidPending(raid),"the warning counts down");
        Management.Tick(raid,.2f,()=>0,report);
        check(!Heat.RaidPending(raid) && raid.raidCount==1 && raid.lastRaidAt>0 && raid.caseFile==35,"the raid happens and the file resets to 35%");
        check(Mathf.Abs(raid.dirty-(dirty-Mathf.Floor(dirty*.5f)))<.01f && Mathf.Abs(Laundering.Clean(raid)-clean)<.01f,"raids seize half the dirty cash and no clean money");
        check(raid.Current.burners==0 && !raid.Current.layout.Exists(i=>i.kind=="cashpallet") && !raid.Current.decorations.Exists(d=>d.kind=="cashpallet"),"raids seize burner phones and the pallet of cash");
        check(rw.Held(raid) && raid.Current.lockdown>59 && raid.Current.desktops==desks,"one crew member is arrested; the office shuts for 60s; desks stay");
        raid.Current.lockdown=0;check(!Management.Start(raid,0,rw,0) && rw.job<0,"crew in custody can't work");
        check(Marks.Headline(raid,out string kicker).Contains("RAID"),"the Gazette covers the raid");
        int bail=Heat.BailCost(rw);float cash=raid.cash,loyalty=rw.loyalty;
        check(Heat.Bail(raid,0,rw) && raid.cash==cash-bail && !rw.Held(raid) && Mathf.Approximately(rw.loyalty,Mathf.Min(100,loyalty+10)),"bail releases crew and earns loyalty");

        var cp=fresh();cp.cash=10000;cp.dirty=8000;cp.caseFile=80;Management.Tick(cp,.1f,()=>0,report);
        cash=cp.cash;check(Heat.ShredCost(cp)==400 && Heat.Shred(cp) && cp.cash==cash-400 && cp.evidenceShredded && !Heat.Shred(cp),"shredding without a shredder costs $400, once");
        check(Heat.DitchBurners(cp) && cp.Current.burners==0 && !Heat.DitchBurners(cp),"ditching burners empties the phone table");
        dirty=cp.dirty;Management.Tick(cp,89.8f,()=>0,report);Management.Tick(cp,.2f,()=>0,report);
        check(Mathf.Abs(cp.dirty-(dirty-Mathf.Floor(dirty*.25f)))<.01f,"shredded evidence halves the seizure");
        check(!cp.Current.workers[0].Held(cp),"ditched burners mean nobody is arrested");

        var dark=fresh();dark.caseFile=80;Management.Tick(dark,.1f,()=>0,report);
        check(Heat.GoDark(dark) && dark.Current.lockdown>=dark.raidSeconds && !Heat.GoDark(dark),"going dark stops the office until the raid");
        Management.Tick(dark,89.8f,()=>0,report);Management.Tick(dark,.2f,()=>0,report);
        check(!dark.Current.workers[0].Held(dark) && Mathf.Abs(dark.Current.lockdown-19.8f)<.05f && dark.caseFile==25,"a dark office gets a short shutdown and a lighter file");

        var bribe=fresh();bribe.cash=20000;bribe.caseFile=80;Management.Tick(bribe,.1f,()=>0,report);cash=bribe.cash;
        check(Heat.BribeCost(bribe)==2500 && Heat.Bribe(bribe) && !Heat.RaidPending(bribe) && bribe.caseFile==40 && bribe.cash==cash-2500 && Heat.BribeCost(bribe)==4000,"a bribe cancels the raid and gets pricier");
        check(!Heat.Bribe(bribe) && !Heat.Shred(bribe) && !Heat.GoDark(bribe),"counterplay needs a pending raid");

        var custody=fresh();var cw=custody.Current.workers[0];Heat.Arrest(custody,cw);loyalty=cw.loyalty;
        Management.Tick(custody,239,()=>0,report);check(cw.Held(custody),"custody lasts four minutes");
        Management.Tick(custody,2,()=>0,report);check(!cw.Held(custody) && cw.loyalty<loyalty,"unbailed crew come back resentful");

        var legacy=new TycoonState{raidSeconds=30};check(Rules.Valid(legacy),"legacy raid timers still load");
        Management.Ensure(legacy);check(!Heat.RaidPending(legacy) && legacy.raidSeconds==-1,"legacy raid timers never resume");
    }

    static void MoneyChecks(Action<bool,string> check,Action<string,float> report,Func<TycoonState> fresh)
    {
        var s=fresh();var w=s.Current.workers[0];Management.SetAssignment(s,0,w,0);Management.Tick(s,12.5f,()=>0,report);
        check(s.cash==4820+220 && s.dirty==220 && Laundering.Clean(s)==4820,"scam takes arrive as dirty money");
        check(w.job==0 && w.reaction>0 && w.status.StartsWith("Scam landed") && !string.IsNullOrEmpty(Marks.Bark(s.Current,w,0,5)),"the result stays on screen while the next run starts");
        check(Rules.Pay(s,100) && s.dirty==120 && s.cash==4940,"everyday spending uses dirty money first");
        s.dirty=s.cash;
        check(!Laundering.PayClean(s,1) && !Rules.BuyApartment(s) && !s.branchOwned,"properties need clean money");
        s.dirty=0;check(Rules.BuyApartment(s) && s.branchOwned,"clean money buys property");
        var l=fresh();l.cash=20000;l.dirty=5000;
        check(Laundering.BuyFront(l,0) && l.cash==17500 && l.dirty==5000 && !Laundering.BuyFront(l,0),"fronts are bought once, with clean money");
        Laundering.Advance(l,Rules.DaySeconds);
        check(Mathf.Abs(l.dirty-3500)<.01f && Mathf.Abs(l.cash-17350)<.01f && Mathf.Abs(l.laundered-1350)<.01f,"a front washes its daily amount for a 10% fee");
        Laundering.Seize(l,99999);check(l.dirty==0 && Mathf.Abs(l.cash-13850)<.01f,"seizures only ever take dirty money");
        l.dirty=l.cash+10;check(!Rules.Valid(l),"dirty money above total cash is rejected");
        var k=fresh();OfficeProgression.Accept(k,0);for(int i=0;i<5;i++)OfficeProgression.CompleteJob(k,0,0,0,report);
        check(k.dirty==400,"contract payouts are dirty");
        var g=fresh();g.scamsCompleted=1;float dirty=g.dirty;check(Management.ClaimGoal(g) && g.dirty==dirty,"milestone rewards are clean");
        var pot=fresh();pot.Current.moonshotPot=500;LivingTown.CashOut(pot);check(pot.dirty==500,"Moonshot cash-outs are dirty");
        var h=fresh();h.empireRank=3;h.branchOwned=h.harborOwned=true;Management.Ensure(h);h.cash=20000;h.dirty=10000;
        check(!LivingTown.Renovate(h),"the Harbor renovation needs clean money");
        h.dirty=0;check(LivingTown.Renovate(h),"clean money renovates Harbor");
        var neon=fresh();neon.branchOwned=true;neon.cash=9000;neon.dirty=8000;neon.scamsCompleted=10;
        check(!Management.BuyHarbor(neon),"Harbor needs clean money");
    }

    static void CrewChecks(Action<bool,string> check,Action<string,float> report,Func<TycoonState> fresh)
    {
        var probe=new Worker{mood=80,salary=35,skill=1};
        check(Crew.DailyChange(probe,false,false)==4 && Crew.DailyChange(probe,true,false)==-10 && Crew.DailyChange(probe,false,true)==-4,"pay, shortfalls and custody move loyalty");
        var s=fresh();var w=s.Current.workers[0];float loyalty=w.loyalty;int salary=w.salary;
        check(Crew.Raise(s,w) && w.salary==salary+10 && w.loyalty==loyalty+12,"a raise buys loyalty");
        var talk=fresh();var tw=talk.Current.workers[0];tw.loyalty=10;
        Crew.Daily(talk,false,()=>0,report);check(Mathf.Abs(talk.caseFile-12)<.001f && tw.loyalty==35,"disloyal crew talk to the detective");
        var quiet=fresh();quiet.Current.workers[0].loyalty=10;Crew.Daily(quiet,false,()=>.99f,report);
        check(quiet.caseFile==0,"talking is a chance, not a certainty");

        var poach=fresh();poach.scamsCompleted=30;poach.cash=10000;check(Management.Hire(poach,0),"second crew member hired");
        var target=poach.Current.workers[1];target.loyalty=5;target.mood=30;
        Crew.Daily(poach,false,()=>.99f,report);check(target.courted && poach.Current.workers.Count==2,"the Syndicate announces who it's courting");
        Crew.Daily(poach,false,()=>.99f,report);check(poach.Current.workers.Count==1 && poach.Current.employeeCount==1 && !poach.Current.workers.Contains(target),"courted crew leave at the next payday");
        check(Rules.Valid(JsonUtility.FromJson<TycoonState>(JsonUtility.ToJson(poach))),"a smaller crew still saves cleanly");
        var keep=fresh();keep.scamsCompleted=30;keep.cash=10000;Management.Hire(keep,0);var kept=keep.Current.workers[1];kept.loyalty=5;kept.mood=30;
        Crew.Daily(keep,false,()=>.99f,report);check(kept.courted && Crew.Raise(keep,kept) && Crew.Raise(keep,kept) && !kept.courted,"raises win courted crew back");
        Crew.Daily(keep,false,()=>.99f,report);check(keep.Current.workers.Contains(kept),"won-back crew stay");
        var last=fresh();last.scamsCompleted=30;var only=last.Current.workers[0];only.loyalty=5;only.mood=30;only.courted=true;
        Crew.Daily(last,false,()=>.99f,report);check(last.Current.workers.Count==1,"the Syndicate never takes your last crew member");

        var sy=fresh();Syndicate.Advance(sy,()=>0,report);check(sy.rivalMoveAt==0,"the Syndicate ignores small crews");
        sy.scamsCompleted=30;Syndicate.Advance(sy,()=>0,report);check(sy.rivalMoveAt==sy.elapsed+Syndicate.MoveEvery,"the Syndicate notices you at 25 runs");
        var sw=sy.Current.workers[0];int previewBefore=LivingTown.PreviewPay(sy,0,sw,0,true);
        sy.elapsed=sy.rivalMoveAt;Syndicate.Advance(sy,()=>0,report);
        check(sy.undercutJob==0 && Syndicate.UndercutFactor(sy,0)==.8f && LivingTown.PreviewPay(sy,0,sw,0,true)<previewBefore,"an undercut cuts that scheme's take by 20%");
        check(Marks.Headline(sy,out string kicker).Contains("KNOCK-OFF"),"the Gazette covers the undercut");
        sy.elapsed=sy.rivalMoveAt;float file=sy.caseFile;Syndicate.Advance(sy,()=>.3f,report);
        check(Mathf.Abs(sy.caseFile-file-8)<.001f,"a tip-off adds 8 to the case file");
        sy.cash=10000;sy.dirty=3000;sy.elapsed=sy.rivalMoveAt;Syndicate.Advance(sy,()=>.6f,report);
        check(Syndicate.OfferPending(sy) && sy.offerAmount==3000 && Syndicate.OfferReturn(sy)==1950,"she offers to launder dirty cash at a 35% cut");
        check(Syndicate.AcceptOffer(sy) && sy.dirty==0 && Mathf.Abs(sy.cash-8950)<.01f && !Syndicate.OfferPending(sy),"accepting washes the cash, minus her cut");
        sy.dirty=3000;sy.elapsed=sy.rivalMoveAt;Syndicate.Advance(sy,()=>.6f,report);sy.elapsed+=Syndicate.OfferSeconds;Syndicate.Advance(sy,()=>0,report);
        check(!Syndicate.OfferPending(sy) && sy.offerAmount==0,"offers expire");
        sy.elapsed=sy.rivalMoveAt;loyalty=sw.loyalty;Syndicate.Advance(sy,()=>.9f,report);
        check(sw.loyalty==loyalty-15,"she courts your least loyal crew");
        float before2=sy.cash;
        check(Syndicate.Truce(sy) && sy.cash==before2-Syndicate.TruceCost && Syndicate.TruceActive(sy) && !Syndicate.Truce(sy),"a truce costs $3,000 and can't stack");
        sy.elapsed=sy.rivalMoveAt;file=sy.caseFile;Syndicate.Advance(sy,()=>.3f,report);check(sy.caseFile==file,"no moves during a truce");
        var race=fresh();race.elapsed=10;Syndicate.OnRaceWon(race);check(race.truceUntil==10+2*Rules.DaySeconds,"beating her in a race buys two quiet days");
        for(int rank=1;rank<=5;rank++)check(!string.IsNullOrEmpty(Underworld.Story[rank]) && !string.IsNullOrEmpty(Underworld.StoryTitles[rank]),"every rank has a story beat");
    }

    static void UiChecks(Action<bool,string> check,Func<TycoonState> fresh)
    {
        var go=new GameObject("Underworld UI verification");bool guard=ScammerTownGame.SuppressDiskSaves;ScammerTownGame.SuppressDiskSaves=true;
        try
        {
            var game=go.AddComponent<ScammerTownGame>();
            var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            Action<string,object[]> call=(name,args)=>typeof(ScammerTownGame).GetMethod(name,flags).Invoke(game,args);
            Func<string,object> field=name=>typeof(ScammerTownGame).GetField(name,flags).GetValue(game);
            var s=fresh();game.state=s;var w=s.Current.workers[0];Management.SetAssignment(s,0,w,0);
            call("OpenCall",new object[]{w});
            check((bool)field("workerModal") && (int)field("workerPage")==3 && field("activeCall")!=null && w.onCall,"Take the call opens the call screen with the call live");
            call("SyncCall",null);check(w.onCall,"an open call stays live");
            typeof(ScammerTownGame).GetField("workerModal",flags).SetValue(game,false);
            call("SyncCall",null);check(field("activeCall")==null && !w.onCall && w.callReadyAt>s.elapsed,"closing the call screen hands the call back");
            call("Navigate",new object[]{"Money"});check((bool)field("managementPanel") && (string)field("tab")=="Money","the money panel opens");
            call("UpdateHookFeed",null);
            var feed=(IList)field("hookFeed");int lines=feed.Count;
            call("OnUnderworldNews",new object[]{s,"RAID WARNING: test"});check(feed.Count==lines+1,"underworld news reaches the live feed");
            call("OnUnderworldNews",new object[]{fresh(),"elsewhere"});check(feed.Count==lines+1,"news from another save is ignored");
        }
        finally{UnityEngine.Object.DestroyImmediate(go);ScammerTownGame.SuppressDiskSaves=guard;}
    }
}
