using UnityEngine;

namespace ScammerTown
{
    public partial class ScammerTownGame
    {
        bool workerModal;
        int workerPage,shopPage;
        Vector2 workerScroll;
        void OpenWorkerProfile(int index)
        {
            if(index<0 || index>=C.workers.Count)return;
            furnitureStorageOpen=false;managementPanel=false;shopModal=false;arrangingRoom=false;selectedWorker=index;workerPage=0;workerScroll=Vector2.zero;workerModal=true;CancelPlacement();
        }
        string WorkerStatus(Worker w)
        {
            if(C.lockdown>0)return "Connection paused · "+Mathf.CeilToInt(C.lockdown)+"s";
            if(w.job>=0 && w.job!=3 && !C.layout.Exists(it=>it.id==w.station && it.Station && it.placed))return "Waiting for a placed desk";
            if(w.job>=0)return Management.Jobs[w.job].name+" · "+Mathf.CeilToInt(w.remaining)+"s";
            return w.assignment>=0 ? "Assigned: "+Management.Jobs[w.assignment].name : "Choose an assignment";
        }
        void DrawApartmentSummary()
        {
            DrawNextStep();
            Space();Text("THIS OFFICE");
            Text(Management.Benefit(state.active),true);
            int wages=0;foreach(var worker in C.workers)wages+=worker.salary;
            Text(Money(C.earned)+" earned in total\n"+Money(wages+Management.Rent(state.active))+" daily rent + wages",true);
            Space();Action("Expand floor",()=>Navigate("Properties"));
            Action("Browse workstations",()=>OpenShop(0));
            Action("Manage employees",()=>Navigate("Employees"));
            if(state.cash<800)
            {
                Space();Text("Need a cash boost?",true);
                Action(state.recoveryRemaining>0 ? "Odd job · "+Mathf.CeilToInt(state.recoveryRemaining)+"s" : state.recoveryCooldown>0 ? "Available in "+Mathf.CeilToInt(state.recoveryCooldown)+"s" : "Free odd job · +$100",()=>{state.recoveryRemaining=10;Save();},CanAct && state.recoveryRemaining<=0 && state.recoveryCooldown<=0);
            }
        }
        void DrawTeamList()
        {
            Text(C.employeeCount+" employees · "+C.workers.FindAll(w=>w.assignment<0).Count+" unassigned");
            PrimaryAction("Hire an employee",()=>OpenShop(3));
            Text("Search by name",true);teamSearch=GUILayout.TextField(teamSearch,40,GUILayout.Height(32));
            needsAssignmentOnly=GUILayout.Toggle(needsAssignmentOnly," Only unassigned employees");Space();
            int shown=0;
            for(int i=0;i<C.workers.Count;i++)
            {
                int index=i;var w=C.workers[i];
                if(needsAssignmentOnly && w.assignment>=0)continue;
                if(!string.IsNullOrEmpty(teamSearch) && w.name.IndexOf(teamSearch,System.StringComparison.OrdinalIgnoreCase)<0)continue;
                shown++;
                Action(w.name+" · Skill "+w.skill+"\n"+WorkerStatus(w),()=>OpenWorkerProfile(index));
            }
            if(shown==0)Text("No employees match. Clear the search or turn off the filter.",true);
        }
        void DrawShop()
        {
            switch(shopPage)
            {
                case 0:
                    Text("WORKSTATIONS");
                    Text("A complete desk, computer, keyboard and chair. Each uses 2 × 2 floor tiles.",true);
                    bool fits=RoomLayout.HasStationSpace(C);
                    if(!fits)
                    {
                        Text("No space for another desk. Expand your office or rearrange the furniture.",true);
                        Action("Manage floor space",()=>{shopModal=false;Navigate("Properties");});
                    }
                    GUILayout.BeginHorizontal();
                    GUILayout.BeginVertical(GUILayout.Width(316));DrawEquipmentCard("desktop","Desktop workstation",800,fits);GUILayout.EndVertical();
                    GUILayout.Space(18);
                    GUILayout.BeginVertical(GUILayout.Width(316));DrawEquipmentCard("laptop","Laptop workstation",650,fits);GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                    Space();Text("Choose a workstation, then click where you want it. You pay only when placed. Esc or right-click cancels.",true);break;
                case 1:DrawRoomUpgrades();break;
                case 2:
                    Text("Crew training · Tier "+C.employeeTier);Text("+3 percentage points of success per tier for Inbox shuffle and Neon pitch.",true);
                    Action("Train crew · "+Money(Rules.TierCost(C.employeeTier+1)),()=>Tier("crew",C.employeeTier,()=>C.employeeTier++),CanAct && C.employeeTier<5 && state.cash>=Rules.TierCost(C.employeeTier+1));
                    Text("Product training · Tier "+C.productTier);Text("+3 percentage points per tier for Parcel puzzle.",true);
                    Action("Train products · "+Money(Rules.TierCost(C.productTier+1)),()=>Tier("products",C.productTier,()=>C.productTier++),CanAct && C.productTier<5 && state.cash>=Rules.TierCost(C.productTier+1));
                    Space();Text("Security · Tier "+C.securityTier);Text("Protection against connection incidents: "+Rules.Chance(C.securityTier)+"%.",true);
                    Action("Improve security · "+Money(Rules.TierCost(C.securityTier+1)),()=>Tier("security",C.securityTier,()=>C.securityTier++),CanAct && C.securityTier<5 && state.cash>=Rules.TierCost(C.securityTier+1));break;
                case 3:
                    Text("HIRE AN EMPLOYEE");
                    Text("New employees need a placed workstation and an assignment. Salaries are paid each game day.",true);
                    if(C.employeeCount>=C.Capacity)Action("Office full · expand floor",()=>{shopModal=false;Navigate("Properties");});
                    Text("Standard employee");Text("Skill 1 · $35/day salary",true);
                    Action("Recruit · $750",()=>Recruit(0),CanAct && C.employeeCount<C.Capacity && state.cash>=750);
                    Space();Text("Specialist");Text("Skill 3 · $60/day salary",true);
                    Action("Recruit · $1,200",()=>Recruit(1),CanAct && C.employeeCount<C.Capacity && state.cash>=1200);break;
            }
        }
        void DrawFloorExpansion()
        {
            Text("OFFICE FLOOR · "+C.Size+" × "+C.Size);
            if(C.level>=Rules.MaxOfficeLevel){Text("Maximum floor size · 10,000 tiles",true);return;}
            int next=C.Size+5,cost=Rules.ExpansionCost(C.level);
            Text("Next: "+next+" × "+next+" · +"+(next*next-C.Size*C.Size)+" tiles",true);
            Action("Expand floor · "+Money(cost),()=>
            {
                Buy("Expand floor",cost,()=>RoomLayout.Expand(C));
                roomPan=Vector2.zero;zoom=1;CancelPlacement();
            },CanAct && state.cash>=cost);
            if(state.cash<cost)Text("Need "+Money(cost-state.cash)+" more",true);
        }
        void DrawEquipmentCard(string kind,string name,int price,bool fits)
        {
            Rect preview=GUILayoutUtility.GetRect(220,190);
            Panel(preview,new Color(.1f,.13f,.19f));
            GUI.BeginGroup(preview);
            float tile=70;Vector2 anchor=new Vector2(preview.width*.5f,160);
            SoftSprite(kind+"_0_0",anchor,tile);
            GUI.EndGroup();
            Text(name);
            Text("Owned here: "+(kind=="desktop" ? C.desktops : C.laptops),true);
            Action("Place · "+Money(price),()=>BeginEquipmentPlacement(kind),CanAct && fits && state.cash>=price);
            if(state.cash<price)Text("Need "+Money(price-state.cash)+" more",true);
        }
        void DrawEmployeeModal()
        {
            var w=SelectedWorker;if(w==null){workerModal=false;return;}
            Panel(new Rect(0,0,1440,900),new Color(.015f,.025f,.05f,.78f));
            Rect window=new Rect(320,140,800,620);
            Panel(window,new Color(.065f,.095f,.16f));
            Label(352,162,600,42,w.name,title);
            Label(352,209,650,28,"Skill "+w.skill+" / 5   ·   $"+w.salary+" per day   ·   "+w.completed+" jobs completed",small);
            if(Button(1036,162,60,"×",true,36))workerModal=false;
            string[] tabs={"Profile","Assignment","Desk"};
            for(int i=0;i<3;i++)
            {
                GUI.backgroundColor=workerPage==i ? new Color(.63f,.8f,.72f) : Color.white;
                if(Button(352+i*247,252,235,tabs[i],true,38)){workerPage=i;workerScroll=Vector2.zero;}
            }
            GUI.backgroundColor=Color.white;
            if(workerPage==1)DrawAssignmentChoices(w);
            else
            {
                GUILayout.BeginArea(new Rect(352,316,736,408));workerScroll=GUILayout.BeginScrollView(workerScroll);
                if(workerPage==0)
                {
                    Text(WorkerStatus(w));
                    if(w.job>=0){GUILayout.Label(Mathf.RoundToInt((1-w.remaining/w.duration)*100)+"% complete",small);Rect progress=GUILayoutUtility.GetRect(700,10);Bar(progress.x,progress.y,progress.width,(1-w.remaining/w.duration)*100,lime);Space();}
                    Text("Assignment: "+(w.assignment>=0 ? Management.Jobs[w.assignment].name+" · repeats automatically" : "None"));
                    Action(w.assignment>=0 ? "Change assignment" : "Choose assignment",()=>workerPage=1);
                    if(w.assignment>=0)Action("Stop after current job",()=>{w.assignment=-1;Save();},CanAct);
                    Text("Mood "+Mathf.RoundToInt(w.mood)+" / 100 · Higher mood improves success",true);
                    Action("Give bonus · $100 · +20 mood",()=>Buy("Employee bonus",100,()=>w.mood=Mathf.Min(100,w.mood+20)),CanAct && state.cash>=100 && w.mood<100);
                }
                else
                {
                    Text("Current desk: "+(w.station ?? "No placed desk"));
                    Text("Desk swaps become available when both employees are between jobs. Stop their assignments first if needed.",true);
                    foreach(var item in C.layout)if(item.Station && item.placed)
                    {
                        string id=item.id;Action((w.station==id ? "✓ " : "Assign to ")+id,()=>{if(Management.Assign(C,w,id))Save();else Notify("That desk's employee is busy. Stop their assignment and wait for the current job.");},CanAct && w.job<0);
                    }
                }
                GUILayout.EndScrollView();GUILayout.EndArea();
            }
            if(Event.current.type==EventType.MouseDown && !window.Contains(Event.current.mousePosition)) {workerModal=false;Event.current.Use();}
        }
        void DrawAssignmentChoices(Worker w)
        {
            for(int i=0;i<Management.Jobs.Length;i++)
            {
                int job=i;var j=Management.Jobs[i];float x=352+(i%2)*376,y=314+(i/2)*188;
                bool locked=i==2 && state.jobsCompleted<5;
                bool needsDesk=i!=3 && w.station==null;
                Panel(new Rect(x,y,360,176),new Color(.09f,.14f,.22f));
                Label(x+14,y+12,332,28,j.name,heading);
                float seconds=j.seconds/(Management.Speed(state.active)*(1+.12f*C.computerUpgrade+.05f*C.lightingUpgrade+.04f*C.comfortUpgrade));
                Label(x+14,y+45,332,45,Money(j.pay*(state.active==2 ? 1.15f : 1))+" per success · "+Mathf.CeilToInt(seconds)+"s\n"+Mathf.RoundToInt(Management.Chance(C,w,i))+"% success"+ (i==3 ? " · No desk needed" : " · Partial results pay 15%"),small);
                Label(x+14,y+92,332,30,locked ? "Unlock: complete 5 jobs" : needsDesk ? "Place a desk before assigning this job" : w.assignment==i ? "Currently assigned · repeats automatically" : "Repeats automatically · no fee",small);
                if(Button(x+14,y+128,332,locked ? "Locked" : w.assignment==i ? "Assigned" : "Assign "+w.name,CanAct && !locked && !needsDesk && w.assignment!=i,34))
                {
                    if(Management.SetAssignment(state,state.active,w,job))
                    {Save();PlayFeedback(false);workerPage=0;Notify(w.name+" assigned to "+j.name+(w.job!=i ? " after the current job." : ". Jobs repeat automatically."));}
                }
            }
            Label(352,701,736,35,"Changes take effect after the current job. Esc or × closes this profile.",small);
        }
    }
}
