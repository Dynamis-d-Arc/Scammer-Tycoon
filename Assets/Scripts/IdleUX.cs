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
            if(w.voucher!=null)return "Fictional voucher game · "+w.voucher.progress+"/4 matched";
            if(C.lockdown>0)return "Office paused · "+Mathf.CeilToInt(C.lockdown)+"s";
            if(w.job>=0 && w.job!=3 && !C.layout.Exists(it=>it.id==w.station && it.Station && it.placed))return "Waiting for a placed desk";
            if(w.job>=0)return Management.Jobs[w.job].name+" · "+Mathf.CeilToInt(w.remaining)+"s";
            return w.assignment>=0 ? "Assigned: "+Management.Jobs[w.assignment].name : "Choose an assignment";
        }
        void DrawApartmentSummary()
        {
            DrawNextStep();
            Space();Text("THIS OFFICE");
            Text("Your team earns automatically. Upgrade when you want to grow.",true);
            Text("At full suspicion, the team takes a short automatic break, then resumes. No fine and no action needed.",true);
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
            PrimaryAction("Start idle employees",()=>{Management.StartIdleTeam(state);Save();},CanAct && C.workers.Exists(w=>w.assignment<0 && w.job<0));
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
                case 4:
                    Text("DECORATIONS");
                    Text("Only placed furniture grants bonuses. Speed bonus caps at 8%; morale recovery at 0.14/sec. Stored furniture gives no bonus.",true);
                    Action("Open Storage",OpenFurnitureStorage);
                    foreach(var furniture in FurnitureCatalog.All)
                    {
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
                    Text("Crew training · Tier "+C.employeeTier);Text("+3 percentage points of success per tier for Phantom Giveaway and Moonshot Club.",true);
                    Action("Train crew · "+Money(Rules.TierCost(C.employeeTier+1)),()=>Tier("crew",C.employeeTier,()=>C.employeeTier++),CanAct && C.employeeTier<5 && state.cash>=Rules.TierCost(C.employeeTier+1));
                    Text("Campaign training · Tier "+C.productTier);Text("+3 percentage points per tier for Mirage Storefront.",true);
                    Action("Train products · "+Money(Rules.TierCost(C.productTier+1)),()=>Tier("products",C.productTier,()=>C.productTier++),CanAct && C.productTier<5 && state.cash>=Rules.TierCost(C.productTier+1));
                    Space();Text("Security · Tier "+C.securityTier);Text("Suspicion builds "+(C.securityTier*8)+"% slower. Connection checks are automatic.",true);
                    Action("Improve security · "+Money(Rules.TierCost(C.securityTier+1)),()=>Tier("security",C.securityTier,()=>C.securityTier++),CanAct && C.securityTier<5 && state.cash>=Rules.TierCost(C.securityTier+1));break;
                case 3:
                    Text("HIRE AN EMPLOYEE");
                    Text("New hires start earning automatically. They use an available desk, or do repair shifts until you give them a campaign. Salaries are automatic.",true);
                    if(C.employeeCount>=C.Capacity)Action("Office full · expand floor",()=>{shopModal=false;Navigate("Properties");});
                    Text("Standard employee");Text("Skill 1 · $35/day salary",true);
                    Action("Recruit · $750",()=>Recruit(0),CanAct && C.employeeCount<C.Capacity && state.cash>=750);
                    Space();Text("Specialist");Text("Skill 3 · $60/day salary",true);
                    for(int specialty=1;specialty<=4;specialty++)
                    {
                        int choice=specialty;
                        Action("Recruit "+OfficeProgression.Specialty(choice)+" · $1,200",()=>Recruit(choice),CanAct && C.employeeCount<C.Capacity && state.cash>=1200);
                    }
                    Text("Specialists work 15% faster with +5 percentage points success on their specialty. Repairs remain guaranteed.",true);break;
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
            string[] tabs={"Profile","Campaigns","Desk","Laptop game"};
            for(int i=0;i<4;i++)
            {
                if(Button(352+i*186,252,178,tabs[i],true,38,workerPage==i)){workerPage=i;workerScroll=Vector2.zero;}
            }
            GUI.backgroundColor=Color.white;
            if(workerPage==3)DrawVoucherGame(w);
            else if(workerPage==1)DrawAssignmentChoices(w);
            else
            {
                GUILayout.BeginArea(new Rect(352,316,736,408));workerScroll=GUILayout.BeginScrollView(workerScroll);
                if(workerPage==0)
                {
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
                    Action("Give bonus · $100 · +20 mood",()=>Buy("Employee bonus",100,()=>w.mood=Mathf.Min(100,w.mood+20)),CanAct && state.cash>=100 && w.mood<100);
                    Space();Text("Specialty training · $300. Stop assignments and finish the current job before retraining.",true);
                    for(int specialty=1;specialty<=4;specialty++)
                    {
                        int choice=specialty;
                        Action("Train: "+OfficeProgression.Specialty(choice),()=>{if(OfficeProgression.Train(state,w,choice)){Save();Notify("Specialty updated.");}},CanAct && w.job<0 && w.specialty!=choice && state.cash>=300);
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
            for(int i=0;i<Management.Jobs.Length;i++)
            {
                int job=i;var j=Management.Jobs[i];float x=352+(i%2)*376,y=314+(i/2)*188;
                bool locked=i==2 && state.jobsCompleted<5;
                bool needsDesk=i!=3 && w.station==null;
                Panel(new Rect(x,y,360,176),new Color(.09f,.14f,.22f));
                Label(x+14,y+12,332,28,j.name,heading);
                float seconds=Management.Duration(C,w,state.active,i);
                Label(x+14,y+45,332,45,Money(j.pay*(state.active==2 ? 1.15f : 1))+" per success · "+Mathf.CeilToInt(seconds)+"s\n"+Mathf.RoundToInt(Management.Chance(C,w,i))+"% success"+ (i==3 ? " · No desk needed" : " · Partial results pay 15%"),small);
                Label(x+14,y+92,332,30,locked ? "Unlock: complete 5 campaigns / shifts" : needsDesk ? "Place a laptop or desktop first" : "Suspicion +"+OfficeProgression.SuspicionGain(i)+" per result"+(i==3?" · Legitimate work":" · +3 on failure"),small);
                if(Button(x+14,y+128,332,locked ? "Locked" : w.assignment==i ? "Assigned" : "Assign "+w.name,CanAct && !locked && !needsDesk && w.assignment!=i,34))
                {
                    if(Management.SetAssignment(state,state.active,w,job))
                    {Save();PlayFeedback(false);workerPage=0;Notify(w.name+" assigned to "+j.name+(w.job!=i ? " after the current job." : ". Jobs repeat automatically."));}
                }
            }
            Label(352,701,736,45,"Success includes office skills, training, specialty and mood (98% cap). Skill tree: Shop > Skills & training. Campaigns are fictional; suspicion still applies.",small);
        }
        void DrawVoucherGame(Worker w)
        {
            GUI.BeginGroup(new Rect(352,302,736,434));
            Panel(new Rect(0,0,736,376),new Color(.19f,.23f,.30f));
            Panel(new Rect(10,10,716,358),new Color(.025f,.045f,.075f));
            Panel(new Rect(363,3,10,4),muted);
            Panel(new Rect(20,20,696,34),new Color(.10f,.16f,.24f));
            Label(32,25,450,24,"PHANTOM OS   /   Voucher Wallet",heading);
            Label(556,26,150,22,"SIMULATION ONLY",small);
            var r=w.voucher;
            int target=r==null?0:r.symbols[r.progress];
            DrawGiftCard(new Rect(28,78,176,112),target,false);
            Label(28,56,190,22,r==null?"PREVIEW CARD":"MATCH THIS CARD",small);
            Label(222,62,480,27,r==null?"Optional bonus · no timer":r.progress+" / 4 redeemed   ·   Take your time",heading);
            Label(222,95,480,44,r==null?"Match four cards for extra cash. Your employee keeps earning in the background.":"Mistakes "+r.mistakes+" / "+r.allowance+"   ·   Reward "+Money(r.reward)+"\nSelect the matching card in the redemption tray.",body);
            if(r==null)
            {
                string status="Skill "+w.skill+" · "+((w.skill-1)/2)+" mistakes allowed · Bonus "+Money(180+w.skill*25+OfficeProgression.SkillBonus(C,0)*5);
                if(!OfficeProgression.HasLaptop(C,w))status="Assign a placed laptop in the Desk tab first.";
                else if(state.elapsed<w.voucherReadyAt)status="Wallet cooling down · "+Mathf.CeilToInt(w.voucherReadyAt-state.elapsed)+"s";
                Label(222,139,480,26,status,small);
                if(Button(222,168,338,"Play for bonus cash",CanAct && OfficeProgression.HasLaptop(C,w) && state.elapsed>=w.voucherReadyAt,32,true))
                {if(OfficeProgression.BeginVoucher(state,w,UnityEngine.Random.Range(0,int.MaxValue)))Save();}
            }
            else
            {
                Bar(222,148,480,r.progress*25,accent);
                if(Button(222,168,338,"Skip bonus · no penalty",CanAct,32))
                {OfficeProgression.FinishVoucher(state,state.active,w,false,(m,a)=>{Log(m);Notify(m);});Save();}
            }
            if(state.paused && Button(574,168,128,"Resume",true,32))state.paused=false;
            Label(28,200,680,22,"REDEMPTION TRAY   ·   Select a fictional gift card",small);
            for(int i=0;i<5;i++)
                if(DrawGiftCard(VoucherCardRect(i),i,w.voucher!=null && CanAct))
                {OfficeProgression.ChooseVoucher(state,w,i,(m,a)=>{Log(m);Notify(m);if(a>0)RewardFeedback(a);});Save();}
            Label(28,344,680,22,"Optional extra cash · No failure penalty · 60s between attempts · Idle work continues",small);
            // Laptop deck: inset keys, a trackpad and a centered hinge.
            Panel(new Rect(0,376,736,48),new Color(.22f,.27f,.34f));
            Panel(new Rect(310,374,116,4),new Color(.045f,.06f,.09f));
            for(int row=0;row<3;row++)for(int key=0;key<16;key++)
                Panel(new Rect(28+key*30,383+row*10,26,7),new Color(.075f,.10f,.15f));
            Panel(new Rect(554,383,150,30),new Color(.14f,.19f,.26f));
            GUI.EndGroup();
        }
        public static Rect VoucherCardRect(int index) => new Rect(28+index*138,224,132,112);
        [System.NonSerialized] GUIStyle voucherCardStyle;
        bool DrawGiftCard(Rect r,int symbol,bool interactive)
        {
            Color[] colors={new Color(.28f,.23f,.57f),new Color(.55f,.26f,.12f),new Color(.10f,.38f,.29f),new Color(.13f,.32f,.55f),new Color(.48f,.18f,.39f)};
            string[] brands={"LUNAR LOOT","STAR ARCADE","FERN MARKET","VOLT PLAY","PRISM CLUB"};
            if(voucherCardStyle==null || voucherCardStyle.normal.background!=uiPanel)
            {
                voucherCardStyle=new GUIStyle(button);
                voucherCardStyle.normal.background=uiPanel;voucherCardStyle.hover.background=uiPanel;
                voucherCardStyle.focused.background=uiPanel;voucherCardStyle.active.background=uiPressed;
            }
            Color old=GUI.backgroundColor;GUI.backgroundColor=interactive && r.Contains(Event.current.mousePosition)?Color.Lerp(colors[symbol],Color.white,.18f):colors[symbol];
            bool clicked=false;
            if(interactive)clicked=GUI.Button(r,new GUIContent("","Redeem fictional "+OfficeProgression.VoucherSymbols[symbol]+" card"),voucherCardStyle);
            else GUI.Box(r,GUIContent.none,voucherCardStyle);
            GUI.backgroundColor=old;
            Label(r.x+9,r.y+6,r.width-18,22,brands[symbol],small);
            DrawVoucherGlyph(new Rect(r.x+10,r.y+34,32,32),symbol);
            Label(r.x+48,r.y+37,r.width-54,26,OfficeProgression.VoucherSymbols[symbol],small);
            Panel(new Rect(r.x+10,r.y+78,r.width-20,1),new Color(.5f,.65f,.75f,.5f));
            Label(r.x+9,r.y+85,r.width-18,22,"PLAY CREDIT",small);
            return clicked;
        }
        void DrawVoucherGlyph(Rect r,int symbol)
        {
            string[] patterns={"0011100/0110000/1100000/1100000/1100000/0110000/0011100","0001000/0001000/1111111/0111110/0011100/0110110/1100011","0000011/0001111/0011110/0111100/1111000/1110000/1000000","0001100/0011000/0110000/1111110/0001100/0011000/0110000","0011100/0111110/1111111/0111110/0011100/0001000/0001000"};
            string[] rows=patterns[symbol].Split('/');
            for(int y=0;y<7;y++)for(int x=0;x<7;x++)if(rows[y][x]=='1')Panel(new Rect(r.x+x*r.width/7,r.y+y*r.height/7,r.width/7,r.height/7),ink);
        }
        void DrawCampaignSkillTree()
        {
            Text("CAMPAIGN SKILL TREE");
            Text("Permanent upgrades for everyone in "+CompanyName+". Buy the starter, choose campaign branches, then unlock mastery. Bonuses add percentage points; campaign success caps at 98%. No effect on suspicion or guaranteed repair shifts.",true);
            DrawCampaignSkillNode(0);
            Text("BASICS  →  CHOOSE A BRANCH  →  EXPERTISE  →  MASTERY",true);
            GUILayout.BeginHorizontal();
            for(int job=0;job<3;job++)
            {
                if(job>0)GUILayout.Space(10);
                GUILayout.BeginVertical(GUILayout.Width(210));
                Text(Management.Jobs[job].name);
                Text("Current bonus: +"+OfficeProgression.SkillBonus(C,job)+" points",true);
                DrawCampaignSkillNode(job+1);
                Text("↓ Requires focus",true);
                DrawCampaignSkillNode(job+4);
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
            DrawCampaignSkillNode(7);
        }
        void DrawCampaignSkillNode(int node)
        {
            bool owned=OfficeProgression.HasSkill(C,node);
            int required=OfficeProgression.SkillPrerequisites(node),cost=OfficeProgression.SkillCost(node);
            bool ready=(C.campaignSkills&required)==required;
            Text(OfficeProgression.SkillNames[node]);Text(OfficeProgression.SkillEffect(node),true);
            if(!ready)Text(node==7?"Requires all three expertise skills":node<=3?"Requires Campaign basics":"Requires the matching focus skill",true);
            else if(!owned && state.cash<cost)Text("Need "+Money(cost-state.cash)+" more",true);
            PrimaryAction(owned?"Unlocked":!ready?"Locked":"Unlock · "+Money(cost),()=>
            {
                if(OfficeProgression.UnlockSkill(state,node)){Save();PlayFeedback(false);Notify(OfficeProgression.SkillNames[node]+" unlocked for "+CompanyName+".");}
            },CanAct && !owned && ready && state.cash>=cost);
        }
    }
}
