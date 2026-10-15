using UnityEngine;

namespace ScammerTown
{
    public partial class ScammerTownGame
    {
        bool shopModal, needsAssignmentOnly, managementPanel;
        string teamSearch="";
        Vector2 shopScroll;
        Texture2D uiNormal,uiHover,uiPressed,uiPanel;
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
            furnitureStorageOpen=false;CancelPlacement();arrangingRoom=false;workerModal=false;managementPanel=false;
            shopPage=page;shopScroll=Vector2.zero;shopModal=true;
        }
        bool GoalReady => state.goal==0 ? state.jobsCompleted>=1 : state.goal==1 ? state.totalJobIncome>=1000 :
            state.goal==2 ? state.companies[0].Stations>=2 : state.goal==3 ? state.companies[0].employeeCount>=2 : state.goal==4 && state.branchOwned;
        void DrawTopBar()
        {
            Panel(new Rect(345,12,750,62),new Color(.065f,.095f,.16f));
            PixelIcon(0,new Rect(364,28,27,27),new Color(1,.76f,.38f));
            Label(404,27,258,32,cityView ? "Scammer Town" : CompanyName,heading);
            PixelIcon(5,new Rect(677,28,27,27),new Color(1,.76f,.38f));
            Label(716,25,170,36,Money(state.cash),stat);
            PixelIcon(1,new Rect(880,28,27,27),new Color(1,.76f,.38f));
            Label(919,29,165,30,C.workers.FindAll(w=>w.job>=0).Count+" working",heading);
            bool allowed=!workerModal && !shopModal && !managementPanel && !help && !confirmReset && !furnitureStorageOpen;
            if(Button(1104,12,92,state.paused ? "Play" : "Pause",allowed,62))state.paused=!state.paused;
            if(Button(1206,12,92,"Menu",allowed,62))Navigate("Settings");
            Label(24,22,290,24,"SCAMMER TOWN / "+(cityView?"CITY":"OFFICE"),small);
        }
        void DrawBottomDock()
        {
            Panel(new Rect(455,803,530,88),new Color(.065f,.095f,.16f));
            string[] labels={"Office","Team","Shop","City","Arrange"};
            for(int i=0;i<5;i++)
            {
                bool selected=i==0&&!cityView&&!arrangingRoom || i==3&&cityView || i==4&&arrangingRoom;
                GUI.backgroundColor=selected?new Color(.28f,.95f,.77f):Color.white;
                if(Button(465+i*103,813,98,"",true,68))
                {
                    if(i==0)Navigate("Operations");
                    else if(i==1)Navigate("Employees");
                    else if(i==2)OpenShop(0);
                    else if(i==3)OpenCity();
                    else {cityView=false;arrangingRoom=!arrangingRoom;CancelPlacement();}
                }
                GUI.backgroundColor=Color.white;
                if(selected)Panel(new Rect(465+i*103,813,98,68),new Color(.28f,.95f,.77f));
                PixelIcon(i,new Rect(498+i*103,820,30,30),selected?new Color(.055f,.1f,.15f):new Color(1,.76f,.38f));
                var labelStyle=new GUIStyle(body){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold,normal={textColor=selected?new Color(.035f,.08f,.12f):ink}};
                GUI.Label(new Rect(465+i*103,853,98,24),labels[i],labelStyle);
            }
            if(Button(1190,840,225,GoalReady?"Claim goal reward":"Goals",true,42))Navigate("Goals");
            Label(24,856,410,28,"1 Office   2 Team   3 Shop   4 City",small);
        }
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
            GUI.backgroundColor=accent;Action(label,action,enabled);GUI.backgroundColor=Color.white;
        }
        void DrawShopModal()
        {
            Panel(new Rect(0,0,1440,900),new Color(.015f,.025f,.05f,.78f));
            Rect window=new Rect(230,115,980,670);
            Panel(window,new Color(.065f,.095f,.16f));
            Label(258,137,550,36,"SHOP & RECRUITMENT",title);
            Label(258,181,650,26,"Buying for "+CompanyName+"   ·   "+Money(state.cash)+" available",small);
            if(Button(1084,140,98,"Close",true,36))shopModal=false;
            string[] names={"Workstations","Office upgrades","Training","Hire employees"};
            for(int i=0;i<4;i++)
            {
                GUI.backgroundColor=shopPage==i ? accent : Color.white;
                if(Button(258,232+i*58,190,names[i],true,46)){shopPage=i;shopScroll=Vector2.zero;}
            }
            GUI.backgroundColor=Color.white;
            Label(258,505,184,70,"Work continues while you browse. Purchases apply to this office.",small);
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
