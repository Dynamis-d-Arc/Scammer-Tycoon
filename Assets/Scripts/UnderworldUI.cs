using UnityEngine;

namespace ScammerTown
{
    // Screens for The Call, the detective's case file, dirty money and fronts,
    // the Velvet Syndicate and rank story beats.
    public partial class ScammerTownGame
    {
        CallSession activeCall;
        bool storyModal;
        int citySelectedFront=-1;
        static readonly Color TrustGreen=new Color(.3f,.85f,.55f),DoubtRed=new Color(1,.38f,.32f),DirtyAmber=new Color(1,.74f,.36f);

        void Report(string message,float amount){Log(message);if(amount>0)RewardFeedback(amount);else Notify(message);}
        void OnUnderworldNews(TycoonState s,string text){if(s==state)Feed(text,FeedTown);}

        // Keeps runtime call state honest when the call screen is closed any other way.
        void SyncCall()
        {
            if(activeCall!=null && !activeCall.Over && (!workerModal || workerPage!=3 || SelectedWorker!=activeCall.worker))
            {TheCall.Abandon(state,activeCall);activeCall=null;}
            foreach(var c in state.companies)
                if(c.workers!=null)foreach(var w in c.workers)
                    if(w.onCall && (activeCall==null || activeCall.Over || activeCall.worker!=w))w.onCall=false;
        }
        void StartCall(Worker w)
        {
            if(activeCall!=null && !activeCall.Over)TheCall.Abandon(state,activeCall);
            activeCall=TheCall.Begin(state,state.active,w,Random.Range(0,int.MaxValue));
            if(activeCall==null)Notify(TheCall.Blocker(state,state.active,w)??"That call can't be taken right now.");
        }
        void OpenCall(Worker w)
        {
            int index=C.workers.IndexOf(w);if(index<0)return;
            OpenWorkerProfile(index);workerPage=3;StartCall(w);
        }

        // ---------- The Call ----------
        void DrawCallTab(Worker w)
        {
            GUI.BeginGroup(new Rect(352,302,736,434));
            Panel(new Rect(0,0,736,424),new Color(.045f,.07f,.12f));
            var call=activeCall!=null && activeCall.worker==w?activeCall:null;
            if(call==null)
            {
                Label(20,16,700,30,"THE CALL",heading);
                Label(20,48,696,64,"Take over "+w.name+"'s call. Each turn, pick an approach. Fill Trust before Doubt hits 100. Each type of mark has favorites, and repeating yourself costs you.",small);
                var mark=Marks.Current(w);
                if(mark!=null)
                {
                    DrawCallPortrait(new Rect(20,118,104,104),mark);
                    Label(156,118,560,26,mark.Name,heading);
                    Label(156,146,560,22,mark.type+" · "+Marks.TypeBlurbs[mark.typeIndex],small);
                    Label(156,170,560,40,"Currently "+mark.belief+".",small);
                }
                Label(20,252,696,64,"A close pays 1.5–2× the success take with half the suspicion and calms the town. A hang-up pays only the partial take and draws extra suspicion. After six turns the mark wants to 'think about it' and the run carries on as normal.",small);
                string blocker=TheCall.Blocker(state,state.active,w);
                if(blocker!=null)Label(20,322,696,24,blocker,body);
                if(Button(20,354,320,"Take the call",blocker==null,44,true))StartCall(w);
                GUI.EndGroup();return;
            }
            DrawCallPortrait(new Rect(20,18,104,104),call.mark);
            Label(146,16,420,26,call.mark.Name,heading);
            Label(146,42,420,20,call.mark.type+" · "+Management.Jobs[call.job].name,small);
            Label(560,18,160,20,"Turn "+Mathf.Min(call.turn+1,TheCall.MaxTurns)+" / "+TheCall.MaxTurns,small);
            var bubble=new Rect(146,66,570,42);
            Panel(bubble,new Color(.97f,.95f,.9f));
            GUI.Label(new Rect(bubble.x+12,bubble.y+4,bubble.width-24,bubble.height-8),"\""+call.markLine+"\"",paperBody);
            Label(146,112,570,22,call.reaction,small);
            DrawMeter(new Rect(20,150,340,10),"TRUST",call.trust,TrustGreen);
            DrawMeter(new Rect(376,150,340,10),"DOUBT",call.doubt,DoubtRed);
            if(!call.Over)
            {
                for(int i=0;i<3;i++)
                {
                    int option=i;
                    string label=TheCall.Approaches[call.options[i]].ToUpper()+"  ·  "+TheCall.Line(call,i);
                    if(Button(20,174+i*56,696,label,CanAct,48))TheCall.Choose(state,call,option,Report);
                }
                if(Button(20,348,300,"Hand the call back",true,40))TheCall.Abandon(state,call);
                Label(336,356,380,30,"Tip: the mark's line hints at what they want.",small);
            }
            else
            {
                string result=call.outcome==TheCall.Closed?"CLOSED · "+Money(call.payout)+" · "+TheCall.CloseMultiplier(call).ToString("0.0")+"× with half the suspicion":
                    call.outcome==TheCall.HungUp?"HUNG UP · partial take "+Money(call.payout)+" · the town is warier now":
                    call.outcome==TheCall.Stalled?"\"I'll think about it.\" The run carries on as normal.":
                    "Call handed back. "+w.name+" picks up where you left off.";
                Panel(new Rect(20,176,696,90),call.outcome==TheCall.Closed?new Color(.08f,.26f,.2f):call.outcome==TheCall.HungUp?new Color(.3f,.1f,.09f):new Color(.1f,.14f,.22f));
                Label(36,192,664,60,result,heading);
                if(Button(20,348,300,"Done",true,40,true))activeCall=null;
            }
            GUI.EndGroup();
        }
        void DrawMeter(Rect r,string label,float value,Color color)
        {
            Label(r.x,r.y-22,200,20,label+" "+Mathf.RoundToInt(value)+" / 100",small);
            Panel(new Rect(r.x,r.y,r.width,r.height),new Color(.14f,.2f,.28f));
            Panel(new Rect(r.x,r.y,r.width*Mathf.Clamp01(value/100),r.height),color);
        }
        void DrawCallPortrait(Rect r,Mark mark)
        {
            Panel(r,Marks.TypeColors[Mathf.Clamp(mark.typeIndex,0,Marks.TypeColors.Length-1)]);
            // Whole-number scale keeps portrait pixels square.
            float size=Mathf.Max(1,Mathf.Floor((Mathf.Min(r.width,r.height)-8)/Marks.PortraitSize))*Marks.PortraitSize;
            GUI.DrawTexture(new Rect(r.center.x-size*.5f,r.center.y-size*.5f,size,size),PortraitTexture(mark));
        }

        // ---------- Top bar readouts and banners ----------
        void DrawHeatReadout()
        {
            Color risk=C.suspicion>=75?DoubtRed:C.suspicion>=40?DirtyAmber:accent;
            Label(490,14,180,20,"Suspicion "+Mathf.CeilToInt(C.suspicion)+" / 100",small);
            Bar(490,33,170,C.suspicion,risk);
            int stage=Heat.Stage(state);
            Label(490,40,190,20,"Case file "+Mathf.FloorToInt(state.caseFile)+"% · "+Heat.Stages[stage],small);
            Bar(490,60,170,state.caseFile,stage>=3?DoubtRed:stage>=2?DirtyAmber:muted);
            if(GUI.Button(new Rect(486,12,184,60),GUIContent.none,GUIStyle.none))Navigate("Town & risk");
        }
        void DrawMoneyReadout()
        {
            Label(716,14,170,36,Money(state.cash),stat);
            moneyNote.normal.textColor=state.dirty>15000?DirtyAmber:muted;
            Label(716,48,200,20,"clean "+Money(Laundering.Clean(state))+" · dirty "+Money(state.dirty),moneyNote);
            if(GUI.Button(new Rect(676,12,200,60),GUIContent.none,GUIStyle.none))Navigate("Money");
        }
        bool DrawUnderworldBanner()
        {
            if(Heat.RaidPending(state))
            {
                Panel(new Rect(300,80,840,52),new Color(.32f,.07f,.07f,.96f));
                bool flash=(int)(animationTime*3)%2==0;
                Label(318,88,560,36,(flash?"● ":"○ ")+"RAID WARNING · police heading to the "+Management.Name(state.raidOffice)+" in "+Mathf.CeilToInt(state.raidSeconds)+"s",body);
                if(Button(944,86,180,"Respond",true,38,true))Navigate("Town & risk");
                return true;
            }
            if(Syndicate.OfferPending(state))
            {
                Panel(new Rect(300,80,840,52),new Color(.2f,.1f,.24f,.96f));
                Label(318,86,520,40,Syndicate.Boss+" will wash "+Money(Mathf.Min(state.offerAmount,state.dirty))+" of dirty cash. You'd get "+Money(Syndicate.OfferReturn(state))+" clean · "+Mathf.CeilToInt(state.offerUntil-state.elapsed)+"s",small);
                if(Button(850,86,136,"Accept",CanAct,38,true) && Syndicate.AcceptOffer(state)){Notify("Deal done. Vivienne sends a fake fruit basket.");Save();}
                if(Button(994,86,130,"Decline",true,38) && Syndicate.DeclineOffer(state))Save();
                return true;
            }
            return false;
        }

        // ---------- Town & risk: the case file ----------
        void DrawCaseFile()
        {
            Text("THE CASE FILE · "+Mathf.FloorToInt(state.caseFile)+"%");
            Rect row=GUILayoutUtility.GetRect(10,96,GUILayout.ExpandWidth(true));
            DrawCallPortrait(new Rect(row.x,row.y,88,88),Underworld.DetectiveMark);
            int stage=Heat.Stage(state);
            GUI.Label(new Rect(row.x+104,row.y,row.width-110,24),Heat.Detective+" · "+Heat.Stages[stage],heading);
            GUI.Label(new Rect(row.x+104,row.y+28,row.width-110,40),Heat.StageNotes[stage],small);
            Bar(row.x+104,row.y+74,row.width-110,state.caseFile,stage>=3?DoubtRed:stage>=2?DirtyAmber:accent);
            Text("The file grows with every run (burner phones: −20%), every automatic cooldown (+10), snitches and Syndicate tips. Over $15,000 in dirty cash also draws attention. It fades while every office stays under 40 suspicion. At 75% a raid is scheduled.",true);
            if(Heat.RaidPending(state))
            {
                var c=state.companies[state.raidOffice];
                Text("RAID WARNING · "+Management.Name(state.raidOffice)+" · "+Mathf.CeilToInt(state.raidSeconds)+"s");
                Text("A raid seizes 50% of your dirty cash, the office's burner phones and any pallet of cash, arrests one crew member, and shuts the office for 60s. It never takes property, desks or upgrades.",true);
                int shred=Heat.ShredCost(state);
                Action(state.evidenceShredded?"✓ Evidence shredded":"Shred evidence · "+(shred==0?"free with your shredder":Money(shred)+" (burn barrel)")+" · seizure halved, case −10",()=>{if(Heat.Shred(state))Save();},CanAct && !state.evidenceShredded && state.cash>=shred);
                Action(state.burnersDitched?"✓ Burners ditched":c.burners>0?"Ditch the burner phones · nobody gets arrested":"No burner phones to ditch",()=>{if(Heat.DitchBurners(state))Save();},CanAct && !state.burnersDitched && c.burners>0);
                Action(state.wentDark?"✓ Office gone dark":"Go dark · stop work until the raid; shorter shutdown, no arrests, seizure halved",()=>{if(Heat.GoDark(state))Save();},CanAct && !state.wentDark);
                int bribe=Heat.BribeCost(state);
                PrimaryAction("Bribe the desk sergeant · "+Money(bribe)+" · raid cancelled, case file to 40%",()=>{if(Heat.Bribe(state)){Save();Notify("The sergeant 'misplaced' the warrant.");}},CanAct && state.cash>=bribe);
                Text("Each bribe costs $1,500 more than the last. The police scanner (Den gear) adds 30s of warning.",true);
            }
            else if(state.lastRaidAt>=0)Text(state.raidCount+(state.raidCount==1?" raid":" raids")+" so far · last one hit the "+Management.Name(Mathf.Max(0,state.lastRaidOffice))+".",true);
            bool anyHeld=false;
            for(int i=0;i<state.companies.Length;i++)
            {
                if(!Management.Owned(state,i))continue;
                int office=i;
                foreach(var w in state.companies[i].workers)if(w.Held(state))
                {
                    if(!anyHeld){Space();Text("IN CUSTODY");anyHeld=true;}
                    var held=w;int bail=Heat.BailCost(w);
                    Action(held.name+" · "+Management.Name(office)+" · out in "+Mathf.CeilToInt(held.heldUntil-state.elapsed)+"s · Post bail "+Money(bail)+" (+10 loyalty)",()=>{if(Heat.Bail(state,office,held)){Save();Notify(held.name+" is back. They brought a souvenir mugshot.");}},CanAct && state.cash>=bail);
                }
            }
            if(anyHeld)Text("Crew in custody can't work and still draw wages. Unbailed crew come back resentful (−15 loyalty).",true);
            Space();
        }

        // ---------- Money & fronts ----------
        void DrawMoneyPanel()
        {
            Text("CLEAN "+Money(Laundering.Clean(state))+"  ·  DIRTY "+Money(state.dirty));
            Text("Scam takes, Moonshot cash-outs and contract payouts arrive dirty. Rent, wages, hires, gear and upgrades spend dirty money first. Properties, floor expansions, fronts and the Harbor renovation need clean money. Raids seize dirty cash, and piles over $15,000 get the detective's attention.",true);
            float daily=Laundering.DailyCapacity(state);
            Text(daily>0?"Your fronts wash "+Money(daily)+" a day (10% processing fee). Laundered so far: "+Money(state.laundered)+".":"No fronts yet. Buy one below to start washing money.",true);
            if(Syndicate.OfferPending(state))
            {
                Space();Text("SHADY OFFER · "+Mathf.CeilToInt(state.offerUntil-state.elapsed)+"s");
                Text(Syndicate.Boss+" will wash "+Money(Mathf.Min(state.offerAmount,state.dirty))+" right now and keep 35%. You'd get "+Money(Syndicate.OfferReturn(state))+" clean.",true);
                PrimaryAction("Take the deal",()=>{if(Syndicate.AcceptOffer(state))Save();},CanAct);
                Action("No thanks",()=>{if(Syndicate.DeclineOffer(state))Save();});
            }
            Space();Text("FRONT BUSINESSES");
            for(int f=0;f<Laundering.Fronts.Length;f++)
            {
                int front=f;bool owned=Laundering.Owns(state,f);
                Text(Laundering.Fronts[f]+" · washes "+Money(Laundering.FrontDaily[f])+"/day");
                Text(Laundering.FrontBlurbs[f],true);
                Action(owned?"✓ Owned":"Buy with clean money · "+Money(Laundering.FrontCost[f]),()=>
                {
                    if(Laundering.BuyFront(state,front)){PlayFeedback(false);Notify(Laundering.Fronts[front]+" is open for business. Sort of.");Log("Bought "+Laundering.Fronts[front]+".");Save();}
                },CanAct && !owned && Laundering.Clean(state)>=Laundering.FrontCost[f]);
            }
            Action("View fronts on the city map",OpenCity);
        }

        // ---------- The Velvet Syndicate ----------
        void DrawSyndicate()
        {
            Space();Text("THE VELVET SYNDICATE");
            Rect row=GUILayoutUtility.GetRect(10,96,GUILayout.ExpandWidth(true));
            DrawCallPortrait(new Rect(row.x,row.y,88,88),Underworld.BossMark);
            GUI.Label(new Rect(row.x+104,row.y,row.width-110,24),Syndicate.Boss,heading);
            string status=!Syndicate.Active(state)?"Hasn't noticed you yet. That changes at 25 scam runs.":
                Syndicate.TruceActive(state)?"Truce · quiet for "+Mathf.CeilToInt(state.truceUntil-state.elapsed)+"s":
                state.rivalMoveAt>state.elapsed?"Next move in "+Mathf.CeilToInt(state.rivalMoveAt-state.elapsed)+"s":"Plotting something.";
            GUI.Label(new Rect(row.x+104,row.y+28,row.width-110,40),status,small);
            if(state.undercutUntil>state.elapsed && state.undercutJob>=0)
                GUI.Label(new Rect(row.x+104,row.y+52,row.width-110,40),"Undercutting "+Management.Jobs[state.undercutJob].name+" (−20%) for "+Mathf.CeilToInt(state.undercutUntil-state.elapsed)+"s",small);
            Text("Every two days she makes a move: undercuts your busiest scheme, tips off the detective, offers to launder dirty cash at a 35% cut, or courts your least loyal crew. Beating her in a race buys two quiet days.",true);
            if(Syndicate.OfferPending(state))
            {
                PrimaryAction("Accept her laundering offer · get "+Money(Syndicate.OfferReturn(state))+" clean",()=>{if(Syndicate.AcceptOffer(state))Save();},CanAct);
                Action("Decline",()=>{if(Syndicate.DeclineOffer(state))Save();});
            }
            Action(Syndicate.TruceActive(state)?"Truce in place":"Buy a truce · "+Money(Syndicate.TruceCost)+" · 3 quiet days",()=>{if(Syndicate.Truce(state)){Save();Notify("Truce agreed. Nobody trusts it.");}},CanAct && Syndicate.Active(state) && !Syndicate.TruceActive(state) && state.cash>=Syndicate.TruceCost);
        }
        void DrawStoryModal()
        {
            Modal("RANK UP · "+Underworld.StoryTitles[Mathf.Clamp(state.storyRank,0,5)].ToUpper());
            Label(462,300,515,260,Underworld.Story[Mathf.Clamp(state.storyRank,0,5)]);
            if(Button(462,620,516,"Continue",true,40,true))storyModal=false;
        }

        // ---------- Crew loyalty (profile) ----------
        void DrawLoyalty(Worker w)
        {
            Text("LOYALTY · "+Mathf.RoundToInt(w.loyalty)+" / 100 · "+Crew.Standing(w));
            Rect bar=GUILayoutUtility.GetRect(10,8,GUILayout.ExpandWidth(true));
            Bar(bar.x,bar.y,bar.width,w.loyalty,w.loyalty<30?DoubtRed:w.loyalty<45?DirtyAmber:accent);GUILayout.Space(8);
            Text("Loyalty moves each payday: up when paid and happy, down when moody, underpaid for their skill, or left in custody. Under 30 they may talk to the detective; under 20 the Syndicate comes calling.",true);
            Action(w.salary>=Crew.MaxSalary?"Salary maxed":"Give a raise · +$"+Crew.RaiseAmount+"/day · +12 loyalty",()=>{if(Crew.Raise(state,w)){Save();Notify(w.name+" feels appreciated. For now.");}},CanAct && w.salary<Crew.MaxSalary);
            if(w.Held(state))
            {
                int bail=Heat.BailCost(w);
                PrimaryAction("Post bail · "+Money(bail)+" · out in "+Mathf.CeilToInt(w.heldUntil-state.elapsed)+"s otherwise",()=>{if(Heat.Bail(state,state.active,w))Save();},CanAct && state.cash>=bail);
            }
        }

        // ---------- City fronts ----------
        void CityFrontMarker(int front,Rect map)
        {
            Vector2 point=Laundering.FrontMapPoints[front];
            point=new Vector2(map.x+point.x/1635f*map.width,map.y+point.y/962f*map.height);
            bool owned=Laundering.Owns(state,front);
            Color color=owned?DirtyAmber:new Color(.6f,.6f,.7f);
            Panel(new Rect(point.x-1,point.y-20,2,21),color);
            var badge=new Rect(point.x-70,point.y-52,140,34);
            Panel(new Rect(badge.x-2,badge.y-2,badge.width+4,badge.height+4),citySelectedFront==front?color:new Color(.2f,.22f,.3f));
            if(cityMarkerStyle==null)cityMarkerStyle=new GUIStyle(button){fontSize=11,padding=new RectOffset(8,5,3,3)};
            string[] shortNames={"LAUNDROMAT","CAR WASH","PIZZA PLACE"};
            if(GUI.Button(badge,shortNames[front]+"\n"+(owned?"washing "+Money(Laundering.FrontDaily[front])+"/day":Money(Laundering.FrontCost[front])+" clean"),cityMarkerStyle))citySelectedFront=front;
        }
        void FrontCard(int front,float x,float y)
        {
            bool owned=Laundering.Owns(state,front);
            Panel(new Rect(x,y,296,208),new Color(.065f,.095f,.16f));
            Label(x+12,y+8,272,30,Laundering.Fronts[front].ToUpper(),heading);
            Label(x+12,y+40,272,40,Laundering.FrontBlurbs[front],small);
            Label(x+12,y+82,272,65,(owned?"OWNED":"FRONT BUSINESS")+"\nWashes "+Money(Laundering.FrontDaily[front])+" dirty cash a day\n10% processing fee",body);
            if(owned){if(Button(x+12,y+160,272,"Money & fronts",true,38))Navigate("Money");}
            else if(Button(x+12,y+160,272,"Buy · "+Money(Laundering.FrontCost[front])+" clean",CanAct && Laundering.Clean(state)>=Laundering.FrontCost[front],38,true))
            {
                if(Laundering.BuyFront(state,front)){PlayFeedback(false);Log("Bought "+Laundering.Fronts[front]+".");Save();}
            }
        }

        // ---------- Office: crew in custody ----------
        void DrawCustodyTag(Worker w,Vector2 p,float tile)
        {
            float y=p.y-tile*1.6f*WorkstationVisualScale;
            var r=new Rect(Mathf.Clamp(p.x-90,24,1236),Mathf.Max(145,y),180,30);
            Panel(r,new Color(.3f,.08f,.08f,.95f));
            GUI.Label(new Rect(r.x+8,r.y+5,r.width-16,20),w.name+" · in custody · "+Mathf.CeilToInt(w.heldUntil-state.elapsed)+"s",small);
        }
    }
}
