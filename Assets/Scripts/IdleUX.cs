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
            if(w.Held(state))return "In custody · "+Mathf.CeilToInt(w.heldUntil-state.elapsed)+"s";
            if(w.onCall)return "You're on this call";
            if(w.breakRemaining>0)return "Coffee break · "+Mathf.CeilToInt(w.breakRemaining)+"s";
            if(C.lockdown>0)return "Office paused · "+Mathf.CeilToInt(C.lockdown)+"s";
            if(!Management.HasPlacedDesk(C,w))return "Waiting for a placed desk";
            if(w.job>=0)return Management.Jobs[w.job].name+" · "+Mathf.CeilToInt(w.remaining)+"s";
            if(w.assignment>=0 && C.shelvedUntil!=null && C.shelvedUntil[w.assignment]>state.elapsed)return "Scheme shelved · "+Mathf.CeilToInt(C.shelvedUntil[w.assignment]-state.elapsed)+"s";
            return w.assignment>=0 ? "Assigned: "+Management.Jobs[w.assignment].name : "Choose an assignment";
        }
        void DrawApartmentSummary()
        {
            DrawNextStep();
            Space();Text("THIS OFFICE");
            Text("Your team earns automatically. Upgrade when you want to grow.",true);
            Text("At full suspicion, the team takes an automatic break and loses the unbanked Moonshot pot. Bank it in Town & risk before suspicion reaches 100.",true);
            Action("Town events & risk decisions",()=>Navigate("Town & risk"));
            Text(Management.Benefit(state.active),true);
            int wages=0;foreach(var worker in C.workers)wages+=worker.salary;
            Text(Money(C.earned)+" earned in total\n"+Money(wages+Management.Rent(state.active))+" daily rent + wages",true);
            Space();Action("Expand floor",()=>Navigate("Properties"));
            Action("Browse workstations",()=>OpenShop(0));
            Action("Manage employees",()=>Navigate("Employees"));
        }
        void DrawTeamList()
        {
            Text(C.employeeCount+" employees · "+C.workers.FindAll(w=>w.assignment<0).Count+" unassigned");
            PrimaryAction("Start idle employees",()=>{Management.StartIdleTeam(state);Save();},CanAct && C.workers.Exists(w=>w.assignment<0 && w.job<0 && Management.HasPlacedDesk(C,w)));
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
                Action(w.name+" · Skill "+w.skill+" · "+LivingTown.Traits[w.trait]+"\n"+WorkerStatus(w),()=>OpenWorkerProfile(index));
            }
            if(shown==0)Text("No employees match. Clear the search or turn off the filter.",true);
        }
        void DrawDecorationCard(FurnitureDefinition furniture,ref bool denHeader,ref bool decorHeader)
        {
            if(furniture.Den && !denHeader){denHeader=true;Space();Text("DEN GEAR · tools of the trade");}
            if(!furniture.Den && !decorHeader){decorHeader=true;Space();Text("DECORATIONS");}
            Rect preview=GUILayoutUtility.GetRect(220,150);
            Panel(preview,new Color(.1f,.13f,.19f));
            GUI.BeginGroup(preview);SoftSprite(furniture.kind+"_0",new Vector2(preview.width*.5f,140),48);GUI.EndGroup();
            int cost=FurnitureCatalog.Price(furniture.kind);
            Text(furniture.name+" · "+furniture.footprint.x+" × "+furniture.footprint.y+" tiles");
            Text(OfficeProgression.DecorBenefit(furniture.kind),true);
            Text("Owned here: "+C.decorations.FindAll(d=>d.kind==furniture.kind).Count,true);
            Action("Buy to Storage · "+Money(cost),()=>
            {
                var item=FurnitureCatalog.BuyDecoration(state,furniture.kind);
                if(item==null)return;
                Save();PlayFeedback(false);Notify("Bought "+item.Name+". Open Storage to place it.");
            },CanAct && state.cash>=cost);
            Space();
        }
        void DrawShop()
        {
            switch(shopPage)
            {
                case 4:
                    Text("DEN GEAR & DECORATIONS");
                    Text("Only placed items grant bonuses; stored items give nothing. Den gear bonuses count once per office. Décor speed caps at 8%; morale recovery at 0.14/sec.",true);
                    Action("Open Storage",OpenFurnitureStorage);
                    bool denHeader=false,decorHeader=false;
                    foreach(var furniture in FurnitureCatalog.All)if(furniture.Den)DrawDecorationCard(furniture,ref denHeader,ref decorHeader);
                    Text("Burner phones · pack of 3");
                    Text("You have "+C.burners+" here. Calls with burners add 20% less to the case file. Ditch them during a raid warning so nobody gets arrested; raids seize them.",true);
                    Action("Buy burner phones · "+Money(Heat.BurnerPackCost),()=>{if(Heat.BuyBurners(state)){Save();PlayFeedback(false);Notify("Three new burners. Untraceable. Probably.");}},CanAct && state.cash>=Heat.BurnerPackCost);
                    Space();
                    foreach(var furniture in FurnitureCatalog.All)if(!furniture.Den)DrawDecorationCard(furniture,ref denHeader,ref decorHeader);
                    Action("Open Storage",OpenFurnitureStorage);break;
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
                    DrawCampaignSkillTree();
                    Space();Text("OTHER TRAINING");
                    Text("Crew training · Tier "+C.employeeTier);Text("+3 success points per tier for Phantom, Moonshot and Echo.",true);
                    Action("Train crew · "+Money(Rules.TierCost(C.employeeTier+1)),()=>Tier("crew",C.employeeTier,()=>C.employeeTier++),CanAct && C.employeeTier<5 && state.cash>=Rules.TierCost(C.employeeTier+1));
                    Text("Campaign training · Tier "+C.productTier);Text("+3 success points per tier for Mirage, Velvet and Crown.",true);
                    Action("Train products · "+Money(Rules.TierCost(C.productTier+1)),()=>Tier("products",C.productTier,()=>C.productTier++),CanAct && C.productTier<5 && state.cash>=Rules.TierCost(C.productTier+1));
                    Space();Text("Security · Tier "+C.securityTier);Text("Suspicion builds "+(C.securityTier*8)+"% slower. Connection checks are automatic.",true);
                    Action("Improve security · "+Money(Rules.TierCost(C.securityTier+1)),()=>Tier("security",C.securityTier,()=>C.securityTier++),CanAct && C.securityTier<5 && state.cash>=Rules.TierCost(C.securityTier+1));break;
                case 3:
                    Text("HIRE AN EMPLOYEE");
                    Text("New hires queue Phantom Giveaway and start at an available desk. Without a placed desk they wait; salaries still apply.",true);
                    if(C.employeeCount>=C.Capacity)Action("Office full · expand floor",()=>{shopModal=false;Navigate("Properties");});
                    Text("Standard employee");Text("Skill 1 · $35/day salary",true);
                    Action("Recruit · $750",()=>Recruit(0),CanAct && C.employeeCount<C.Capacity && state.cash>=750);
                    Space();Text("Specialist");Text("Skill 3 · $60/day salary",true);
                    for(int specialty=1;specialty<=Management.Jobs.Length;specialty++)
                    {
                        int choice=specialty;
                        Action("Recruit "+OfficeProgression.Specialty(choice)+" · $1,200",()=>Recruit(choice),CanAct && C.employeeCount<C.Capacity && state.cash>=1200 && EmpireProgression.Unlocked(state,state.active,choice-1));
                        if(!EmpireProgression.Unlocked(state,state.active,choice-1))Text("Requires "+EmpireProgression.Requirement(choice-1),true);
                    }
                    Text("Specialists work 15% faster with +5 percentage points success on their specialty.",true);break;
            }
        }
        void DrawFloorExpansion()
        {
            Text("OFFICE FLOOR · "+C.Size+" × "+C.Size);
            if(C.level>=Rules.MaxOfficeLevel){Text("Maximum floor size · 10,000 tiles",true);return;}
            int next=C.Size+5,cost=Rules.ExpansionCost(C.level);
            Text("Next: "+next+" × "+next+" · +"+(next*next-C.Size*C.Size)+" tiles",true);
            Action("Expand floor · "+Money(cost)+" clean",()=>
            {
                Buy("Expand floor",cost,()=>RoomLayout.Expand(C),true);
                pixelCameraCompany=-1;PixelCamera();CancelPlacement();
            },CanAct && Laundering.Clean(state)>=cost);
            if(Laundering.Clean(state)<cost)Text("Need "+Money(cost-Laundering.Clean(state))+" more clean money. Launder through a front.",true);
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
            string[] tabs={"Profile","Scams","Desk","Take the call"};
            for(int i=0;i<4;i++)
            {
                if(Button(352+i*186,252,178,tabs[i],true,38,workerPage==i)){workerPage=i;workerScroll=Vector2.zero;}
            }
            GUI.backgroundColor=Color.white;
            if(workerPage==3)DrawCallTab(w);
            else if(workerPage==1)DrawAssignmentChoices(w);
            else
            {
                GUILayout.BeginArea(new Rect(352,316,736,408));workerScroll=GUILayout.BeginScrollView(workerScroll);
                if(workerPage==0)
                {
                    DrawLoyalty(w);Space();
                    DrawWorkerPersonality(w);
                    Text(OfficeProgression.Specialty(w.specialty)+" · matching jobs: +15% speed, +5 points success",true);
                    Text("Office skill tree: Phantom +"+OfficeProgression.SkillBonus(C,0)+" / Mirage +"+OfficeProgression.SkillBonus(C,1)+" / Moonshot +"+OfficeProgression.SkillBonus(C,2)+" success points.",true);
                    Action("Open campaign skill tree",()=>OpenShop(2));
                    Text("Office décor: +"+Mathf.RoundToInt(OfficeProgression.DecorSpeed(C)*100)+"% speed; +"+OfficeProgression.DecorMorale(C).ToString("0.00")+" morale/sec",true);
                    Text(WorkerStatus(w));
                    if(w.job>=0){GUILayout.Label(Mathf.RoundToInt((1-w.remaining/w.duration)*100)+"% complete",small);Rect progress=GUILayoutUtility.GetRect(700,10);Bar(progress.x,progress.y,progress.width,(1-w.remaining/w.duration)*100,lime);Space();}
                    Text("Assignment: "+(w.assignment>=0 ? Management.Jobs[w.assignment].name+" · repeats automatically" : "None"));
                    Action(w.assignment>=0 ? "Change assignment" : "Choose assignment",()=>workerPage=1);
                    if(w.assignment>=0)Action("Stop after current job",()=>{w.assignment=-1;Save();},CanAct);
                    Text("Mood "+Mathf.RoundToInt(w.mood)+" / 100 · Higher mood improves success",true);
                    Action("Give bonus · $100 · +20 mood, +5 loyalty",()=>Buy("Employee bonus",100,()=>Crew.Bonus(w)),CanAct && state.cash>=100 && w.mood<100);
                    Space();Text("Specialty training · $300. Stop assignments and finish the current job before retraining.",true);
                    for(int specialty=1;specialty<=Management.Jobs.Length;specialty++)
                    {
                        int choice=specialty;
                        Action("Train: "+OfficeProgression.Specialty(choice),()=>{if(OfficeProgression.Train(state,w,choice)){Save();Notify("Specialty updated.");}},CanAct && w.job<0 && w.specialty!=choice && state.cash>=300 && EmpireProgression.Unlocked(state,state.active,choice-1));
                        if(!EmpireProgression.Unlocked(state,state.active,choice-1))Text("Requires "+EmpireProgression.Requirement(choice-1),true);
                    }
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
            workerScroll=GUI.BeginScrollView(new Rect(352,306,736,426),workerScroll,new Rect(0,0,712,Management.Jobs.Length*180+60));
            for(int i=0;i<Management.Jobs.Length;i++)
            {
                int job=i;var j=Management.Jobs[i];float x=0,y=i*180;
                bool locked=!EmpireProgression.Unlocked(state,state.active,i);
                bool needsDesk=!Management.HasPlacedDesk(C,w);
                Panel(new Rect(x,y,710,168),new Color(.09f,.14f,.22f));
                Label(x+14,y+12,680,28,j.name,heading);
                float seconds=Management.Duration(C,w,state.active,i);
                Label(x+14,y+45,680,42,Money(LivingTown.PreviewPay(state,state.active,w,i,true))+" next success · "+Mathf.CeilToInt(seconds)+"s · "+Mathf.RoundToInt(Management.Chance(C,w,i))+"% success\n"+LivingTown.WorkerMechanic(w,i)+" · Partial take: "+Money(EmpireProgression.Pay(C,state.active,i,false)),small);
                Label(x+14,y+88,680,30,locked ? "Requires "+EmpireProgression.Requirement(i) : needsDesk ? "Place a laptop or desktop first" : "Next run suspicion +"+LivingTown.Heat(state,C,w,i).ToString("0.0")+" · Town resistance "+Mathf.RoundToInt(LivingTown.Resistance(C,i)),small);
                if(Button(x+14,y+124,680,locked ? "Locked" : w.assignment==i ? "Assigned" : "Assign "+w.name,CanAct && !locked && !needsDesk && w.assignment!=i,34))
                {
                    if(Management.SetAssignment(state,state.active,w,job))
                    {Save();PlayFeedback(false);workerPage=0;Notify(w.name+" assigned to "+j.name+(w.job!=i ? " after the current job." : ". Jobs repeat automatically."));}
                }
            }
            Label(8,Management.Jobs.Length*180,690,50,"Success includes skills, training, headsets, specialty and mood (98% cap). Shop > Skills & training.",small);
            GUI.EndScrollView();
        }
        void DrawCampaignSkillTree()
        {
            Text("CAMPAIGN SKILL TREE");
            Text("Permanent upgrades for everyone in "+CompanyName+". Buy the starter, choose campaign branches, then unlock mastery. Bonuses add percentage points; campaign success caps at 98%. No effect on suspicion.",true);
            DrawCampaignSkillNode(0);
            Text("BASICS  →  CHOOSE A BRANCH  →  EXPERTISE  →  MASTERY",true);
            for(int job=0;job<Management.Jobs.Length;job++)
            {
                if(job%3==0)GUILayout.BeginHorizontal();else GUILayout.Space(10);
                GUILayout.BeginVertical(GUILayout.Width(210));
                Text(Management.Jobs[job].name);
                Text("Current bonus: +"+OfficeProgression.SkillBonus(C,job)+" points",true);
                DrawCampaignSkillNode(OfficeProgression.FocusFor(job));
                Text("↓ Requires focus",true);
                DrawCampaignSkillNode(OfficeProgression.ExpertiseFor(job));
                GUILayout.EndVertical();
                if(job%3==2 || job==Management.Jobs.Length-1)GUILayout.EndHorizontal();
            }
            DrawCampaignSkillNode(7);
        }
        void DrawCampaignSkillNode(int node)
        {
            bool owned=OfficeProgression.HasSkill(C,node);
            int required=OfficeProgression.SkillPrerequisites(node),cost=OfficeProgression.SkillCost(node);
            bool ready=(C.campaignSkills&required)==required;
            Text(OfficeProgression.SkillNames[node]);Text(OfficeProgression.SkillEffect(node),true);
            if(!ready)Text(node==7?"Requires Phantom, Mirage and Moonshot expertise":OfficeProgression.FocusNode(node)?"Requires Campaign basics":"Requires the matching focus skill",true);
            else if(!owned && state.cash<cost)Text("Need "+Money(cost-state.cash)+" more",true);
            PrimaryAction(owned?"Unlocked":!ready?"Locked":"Unlock · "+Money(cost),()=>
            {
                if(OfficeProgression.UnlockSkill(state,node)){Save();PlayFeedback(false);Notify(OfficeProgression.SkillNames[node]+" unlocked for "+CompanyName+".");}
            },CanAct && !owned && ready && state.cash>=cost);
        }
    }
}
