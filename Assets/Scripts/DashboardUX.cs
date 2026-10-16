using UnityEngine;

namespace ScammerTown
{
    public partial class ScammerTownGame
    {
        bool shopModal, needsAssignmentOnly, managementPanel=true;
        string teamSearch="";
        Vector2 shopScroll;
        Texture2D uiNormal,uiHover,uiPressed,uiPanel,uiAccent,uiAccentHover,uiAccentPressed,uiDisabled;
        readonly Color accent=new Color(.28f,.95f,.77f);
        Texture2D Solid(Color color)
        {
            var t=new Texture2D(1,1);t.SetPixel(0,0,color);t.Apply();return t;
        }
        void Navigate(string destination)
        {
            furnitureStorageOpen=false;CancelPlacement();arrangingRoom=false;scroll=Vector2.zero;GUIUtility.keyboardControl=0;
            if(destination=="Shop"){OpenShop(0);return;}
            shopModal=false;workerModal=false;
            if(destination=="City"){OpenCity();return;}
            cityView=false;tab=destination;managementPanel=destination!="Operations";
        }
        void OpenShop(int page)
        {
            Management.Ensure(state);
            furnitureStorageOpen=false;CancelPlacement();arrangingRoom=false;workerModal=false;managementPanel=false;
            shopPage=page;shopScroll=Vector2.zero;shopModal=true;
        }
        bool GoalReady => Management.GoalReady(state);
        void DrawTopBar()
        {
            Panel(new Rect(24,12,1392,62),new Color(.065f,.095f,.16f));
            PixelIcon(0,new Rect(44,29,27,27),new Color(1,.76f,.38f));
            Label(86,20,420,25,cityView ? "Scammer Town" : CompanyName,heading);
            PixelIcon(5,new Rect(677,28,27,27),new Color(1,.76f,.38f));
            DrawMoneyReadout();
            PixelIcon(1,new Rect(880,28,27,27),new Color(1,.76f,.38f));
            Label(919,29,245,30,C.workers.FindAll(w=>w.job>=0 && w.job<Management.Jobs.Length).Count+" running scams",heading);
            bool allowed=!workerModal && !shopModal && !managementPanel && !help && !confirmReset && !furnitureStorageOpen;
            if(Button(1184,23,100,state.paused ? "Resume" : "Pause",allowed,40))state.paused=!state.paused;
            if(Button(1296,23,100,"Menu",allowed,40))Navigate("Settings");
            Label(86,46,420,20,"SCAMMER TOWN / "+(cityView?"CITY":"OFFICE"),small);
            DrawHeatReadout();
        }
        void DrawBottomDock()
        {
            Panel(new Rect(220,816,1000,72),new Color(.065f,.095f,.16f));
            string[] labels={"Scams","Team","Shop","City","Arrange","Contracts",GoalReady?"Claim reward":"Goals"};
            for(int i=0;i<labels.Length;i++)
            {
                bool selected=i==0&&managementPanel&&tab=="Scams" || i==1&&managementPanel&&tab=="Employees" || i==2&&shopModal || i==3&&cityView || i==4&&arrangingRoom || i==5&&managementPanel&&tab=="Contracts" || i==6&&managementPanel&&tab=="Goals";
                bool highlighted=selected || i==6&&GoalReady;
                Rect slot=DockButtonRect(i);
                if(Button(slot.x,slot.y,slot.width,"",true,slot.height,highlighted))
                {
                    if(i==0)Navigate("Scams");
                    else if(i==1)Navigate("Employees");
                    else if(i==2)OpenShop(0);
                    else if(i==3)OpenCity();
                    else if(i==4){cityView=false;arrangingRoom=!arrangingRoom;CancelPlacement();}
                    else Navigate(i==5?"Contracts":"Goals");
                }
                PixelIcon(i<5?i:i==5?6:7,new Rect(slot.center.x-12,slot.y+5,24,24),highlighted&&GUI.enabled?new Color(.055f,.1f,.15f):new Color(1,.76f,.38f));
                GUI.Label(new Rect(slot.x,slot.y+31,slot.width,22),labels[i],highlighted&&GUI.enabled?dockSelectedLabel:dockLabel);
            }
        }
        public static Rect DockButtonRect(int index) => new Rect(232+index*140,824,136,56);
        public static Rect RoomToolRect(int index) => new Rect(232+index*164,758,156,40);
        void DrawScamBoard()
        {
            DrawGazette(GUILayoutUtility.GetRect(900,70),false);
            GUILayout.Space(12);
            int hooked=C.workers.FindAll(w=>w.job>=0 && Management.HasPlacedDesk(C,w)).Count;
            Text("ON THE HOOK  ·  "+hooked+(hooked==1?" mark":" marks")+" on the line right now  ·  "+state.marksMet.ToString("N0")+" marks contacted so far");
            DrawHookedMarks(GUILayoutUtility.GetRect(900,240));
            GUILayout.Space(14);
            Text(state.scamsCompleted+" scam runs completed  ·  "+Money(state.scamIncome)+" scam proceeds");
            Text(Management.Goal(state),true);
            if(state.legacyScamCredit>0 || state.legacyScamIncomeCredit>0)
                Text("Earlier save progress is credited toward goals and unlocks. The totals above track scam runs since this update.",true);
            int desks=C.workers.FindAll(w=>Management.HasPlacedDesk(C,w)).Count;
            Text(desks+" crew with placed desks  ·  Suspicion "+Mathf.CeilToInt(C.suspicion)+" / 100",true);
            if(C.lockdown>0)Text("Laying low for "+Mathf.CeilToInt(C.lockdown)+"s. Queued scams resume automatically.",true);
            if(desks==0)PrimaryAction("Place a workstation to start scamming",()=>OpenShop(0));
            Text("TOWN TODAY · "+LivingTown.Events[LivingTown.EventAt(state.elapsed)],true);
            PrimaryAction("Town events, suspicion & Moonshot cash-out",()=>Navigate("Town & risk"));
            Space();Text("THE SCHEMES  ·  pick one and put your crew on it");
            for(int job=0;job<Management.Jobs.Length;job++)
            {
                if(job%3==0)GUILayout.BeginHorizontal();else GUILayout.Space(12);
                DrawSchemeCard(job,desks);
                if(job%3==2 || job==Management.Jobs.Length-1)GUILayout.EndHorizontal();
            }
            Space();Text("SCAM EMPIRE");
            Text("Moonshot: 5 runs. Velvet: 10 runs + computers Lv 1. Echo: 20 runs + Lv 2. Crown: 35 runs + Lv 3. Harbor Loft: 10 runs, Neon ownership and $7,500. Bonus minigames do not advance scam goals.",true);
            Action("Upgrade equipment & operating style",()=>OpenShop(1));
            Action("Buy den gear: shredders, scanners, cash counters",()=>OpenShop(4));
            Action("Scam contracts",()=>Navigate("Contracts"));
            Action("Improve scam skills",()=>OpenShop(2));
            Action("Manage individual assignments",()=>Navigate("Employees"));
            Action("Office overview",()=>Navigate("Overview"));
            Action("View the office",()=>Navigate("Operations"));
        }
        // A pinned case file for one scheme.
        void DrawSchemeCard(int job,int desks)
        {
            int choice=job;var scam=Management.Jobs[job];
            bool locked=!EmpireProgression.Unlocked(state,state.active,job);
            int assigned=C.workers.FindAll(w=>w.assignment==choice).Count;
            float wariness=LivingTown.Resistance(C,job);
            GUILayout.BeginVertical(paperCard,GUILayout.Width(284));
            GUILayout.Label(scam.name.ToUpper(),paperTitle);
            GUILayout.Label(scam.description,paperSmall);GUILayout.Space(6);
            GUILayout.Label(Money(EmpireProgression.Pay(C,state.active,job,true))+" take  ·  "+scam.seconds+"s  ·  "+(assigned==0?"nobody on it":assigned+" crew on it"),paperBody);
            GUILayout.Label("Partial take "+Money(EmpireProgression.Pay(C,state.active,job,false))+"  ·  Suspicion +"+OfficeProgression.ActualSuspicion(C,job,true).ToString("0.0")+" per run",paperSmall);
            GUILayout.Label("Town wariness "+Mathf.RoundToInt(wariness)+" / 100"+(wariness>0?"  ·  −"+(wariness*.15f).ToString("0.0")+" success":""),paperSmall);
            Rect bar=GUILayoutUtility.GetRect(240,6);
            Panel(bar,new Color(.78f,.72f,.6f));Panel(new Rect(bar.x,bar.y,bar.width*Mathf.Clamp01(wariness/100),bar.height),wariness>=50?StringRed:new Color(.85f,.55f,.2f));
            GUILayout.Space(6);
            GUILayout.Label(LivingTown.Mechanic(job),paperSmall);
            if(locked)GUILayout.Label("LOCKED · "+EmpireProgression.Requirement(job),paperAlert);
            GUILayout.Space(6);
            bool available=C.workers.Exists(w=>Management.HasPlacedDesk(C,w) && w.assignment!=choice);
            PrimaryAction(locked?"Locked":!available && desks>0?"Whole crew is on it":"Put crew on "+scam.name,()=>
            {
                int count=Management.AssignScamCrew(state,choice);
                if(count>0){Save();PlayFeedback(false);Notify(count+" crew on "+scam.name+". Current calls finish first.");}
            },CanAct && !locked && available);
            Action(state.elapsed<C.shelvedUntil[job]?"Shelved · "+Mathf.CeilToInt(C.shelvedUntil[job]-state.elapsed)+"s":"Lie low on this one · 60s · −20 wariness",()=>{if(LivingTown.Shelve(state,choice))Save();},CanAct && !locked && state.elapsed>=C.shelvedUntil[job]);
            GUILayout.EndVertical();
            if(Event.current.type==EventType.Repaint){var card=GUILayoutUtility.GetLastRect();DrawPin(new Vector2(card.center.x,card.y+8));}
        }
        void DrawCompactAlert()
        {
            if(DrawUnderworldBanner())return;
            if(state.incidentCompany<0)return;
            Panel(new Rect(300,74,840,55),new Color(.24f,.15f,.075f));
            Label(314,88,450,30,Management.Name(state.incidentCompany)+" · Connection alert: "+Mathf.CeilToInt(state.incidentCountdown)+"s",body);
            if(Button(772,82,165,"Isolate · free",CanAct,38))ResolveThreat(false);
            if(Button(947,82,180,"Secure · $100",CanAct && state.cash>=100,38))ResolveThreat(true);
        }
        void DrawNextStep()
        {
            int unassigned=C.workers.FindIndex(w=>w.assignment<0 && w.job<0 && Management.HasPlacedDesk(C,w));
            int noDesk=C.workers.FindIndex(w=>!Management.HasPlacedDesk(C,w));
            Text("NEXT STEP");
            if(C.lockdown>0){Text("Connection restoring in "+Mathf.CeilToInt(C.lockdown)+"s",true);}
            else if(unassigned>=0)
            {
                Text(C.workers[unassigned].name+" is ready for work.",true);
                PrimaryAction("Start earning automatically",()=>{Management.StartIdleTeam(state);Save();});
            }
            else if(noDesk>=0)
            {
                Text(C.workers[noDesk].name+" needs a placed workstation.",true);
                PrimaryAction("Get a workstation",()=>OpenShop(0));
            }
            else
            {
                Text("Your team is working automatically. Add a workstation and hire someone to grow.",true);
                bool spareDesk=C.layout.Exists(item=>item.Station && item.placed && Management.At(C,item)==null);
                PrimaryAction(spareDesk ? "Hire for your empty desk" : "Add a workstation",()=>OpenShop(spareDesk ? 3 : 0));
            }
        }
        void PrimaryAction(string label,System.Action action,bool enabled=true)
        {
            Action(label,action,enabled,true);
        }
        void DrawShopModal()
        {
            Panel(new Rect(0,0,1440,900),new Color(.015f,.025f,.05f,.78f));
            Rect window=new Rect(230,115,980,670);
            Panel(window,new Color(.065f,.095f,.16f));
            Label(258,137,550,36,"SHOP & RECRUITMENT",title);
            Label(258,181,650,26,"Buying for "+CompanyName+"   ·   "+Money(state.cash)+" available",small);
            if(Button(1084,140,98,"Close",true,36))shopModal=false;
            string[] names={"Workstations","Office upgrades","Skills & training","Hire employees","Den & décor"};
            for(int i=0;i<names.Length;i++)
            {
                if(Button(258,232+i*58,190,names[i],true,46,shopPage==i)){shopPage=i;shopScroll=Vector2.zero;}
            }
            GUI.backgroundColor=Color.white;
            Label(258,550,184,70,"Work continues while you browse. Purchases apply to this office.",small);
            if(state.incidentCompany>=0)
            {
                Label(258,584,190,48,"Connection alert · "+Mathf.CeilToInt(state.incidentCountdown)+"s",body);
                if(Button(258,638,190,"Isolate · free",CanAct,34))ResolveThreat(false);
                if(Button(258,679,190,"Secure · $100",CanAct && state.cash>=100,34))ResolveThreat(true);
            }
            else if(state.paused)
            {
                Label(258,584,190,48,"Resume the game to make purchases.",body);
                if(Button(258,638,190,"Resume game",true,34))state.paused=false;
            }
            Label(258,722,190,28,"Esc to close",small);
            GUILayout.BeginArea(new Rect(474,231,704,516));
            shopScroll=GUILayout.BeginScrollView(shopScroll);
            DrawShop();
            GUILayout.EndScrollView();GUILayout.EndArea();
            if(Event.current.type==EventType.MouseDown && !window.Contains(Event.current.mousePosition)){shopModal=false;Event.current.Use();}
        }
    }
}
