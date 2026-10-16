using UnityEngine;

namespace ScammerTown
{
    public partial class ScammerTownGame
    {
        bool shopModal, needsAssignmentOnly, managementPanel;
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
        bool GoalReady => state.goal==0 ? state.jobsCompleted>=1 : state.goal==1 ? state.totalJobIncome>=1000 :
            state.goal==2 ? state.companies[0].Stations>=2 : state.goal==3 ? state.companies[0].employeeCount>=2 : state.goal==4 && state.branchOwned;
        void DrawTopBar()
        {
            Panel(new Rect(24,12,1392,62),new Color(.065f,.095f,.16f));
            PixelIcon(0,new Rect(44,29,27,27),new Color(1,.76f,.38f));
            Label(86,20,420,25,cityView ? "Scammer Town" : CompanyName,heading);
            PixelIcon(5,new Rect(677,28,27,27),new Color(1,.76f,.38f));
            Label(716,25,170,36,Money(state.cash),stat);
            PixelIcon(1,new Rect(880,28,27,27),new Color(1,.76f,.38f));
            Label(919,29,165,30,C.workers.FindAll(w=>w.job>=0).Count+" working",heading);
            bool allowed=!workerModal && !shopModal && !managementPanel && !help && !confirmReset && !furnitureStorageOpen;
            if(Button(1184,23,100,state.paused ? "Resume" : "Pause",allowed,40))state.paused=!state.paused;
            if(Button(1296,23,100,"Menu",allowed,40))Navigate("Settings");
            Label(86,46,420,20,"SCAMMER TOWN / "+(cityView?"CITY":"OFFICE"),small);
            Color risk=C.suspicion>=75?new Color(1,.42f,.35f):C.suspicion>=40?new Color(1,.76f,.38f):accent;
            Label(490,22,180,24,"Suspicion "+Mathf.CeilToInt(C.suspicion)+" / 100",small);
            Bar(490,51,170,C.suspicion,risk);
        }
        void DrawBottomDock()
        {
            Panel(new Rect(220,816,1000,72),new Color(.065f,.095f,.16f));
            string[] labels={"Office","Team","Shop","City","Arrange","Contracts",GoalReady?"Claim reward":"Goals"};
            for(int i=0;i<labels.Length;i++)
            {
                bool selected=i==0&&!cityView&&!arrangingRoom&&!managementPanel&&!shopModal || i==1&&managementPanel&&tab=="Employees" || i==2&&shopModal || i==3&&cityView || i==4&&arrangingRoom || i==5&&managementPanel&&tab=="Contracts" || i==6&&managementPanel&&tab=="Goals";
                bool highlighted=selected || i==6&&GoalReady;
                Rect slot=DockButtonRect(i);
                if(Button(slot.x,slot.y,slot.width,"",true,slot.height,highlighted))
                {
                    if(i==0)Navigate("Operations");
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
        void DrawCompactAlert()
        {
            if(state.incidentCompany<0)return;
            Panel(new Rect(300,74,840,55),new Color(.24f,.15f,.075f));
            Label(314,88,450,30,Management.Name(state.incidentCompany)+" · Connection alert: "+Mathf.CeilToInt(state.incidentCountdown)+"s",body);
            if(Button(772,82,165,"Isolate · free",CanAct,38))ResolveThreat(false);
            if(Button(947,82,180,"Secure · $100",CanAct && state.cash>=100,38))ResolveThreat(true);
        }
        void DrawNextStep()
        {
            int unassigned=C.workers.FindIndex(w=>w.assignment<0 && w.job<0);
            int noDesk=C.workers.FindIndex(w=>w.station==null);
            Text("NEXT STEP");
            if(C.lockdown>0){Text("Connection restoring in "+Mathf.CeilToInt(C.lockdown)+"s",true);}
            else if(unassigned>=0)
            {
                Text(C.workers[unassigned].name+" is ready for work.",true);
                PrimaryAction("Choose an assignment",()=>{OpenWorkerProfile(unassigned);workerPage=1;});
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
            string[] names={"Workstations","Office upgrades","Skills & training","Hire employees","Decorations"};
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
