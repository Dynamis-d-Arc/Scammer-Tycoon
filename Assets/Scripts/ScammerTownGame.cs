using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ScammerTown
{
    public partial class ScammerTownGame : MonoBehaviour
    {
        public static bool SuppressDiskSaves;
        public TycoonState state = new TycoonState();
        Texture2D basement, neon, diamond;
        readonly List<string> events = new List<string>();
        string tab = "Operations", selection = "Desktop", notice = "", saveStatus = "";
        float noticeUntil, saveClock, zoom = 1;
        Vector2 scroll;
        Vector2 viewportScroll;
        bool confirmReset, help;
        [NonSerialized] GUIStyle title, heading, body, small, button, primaryButton, disabledButton, dockLabel, dockSelectedLabel, stat, card;
        readonly Color ink = new Color(.94f, .96f, 1f), muted = new Color(.58f, .69f, .8f), lime = new Color(.25f, .95f, .76f);
        string SavePath => Path.Combine(Application.persistentDataPath, "scammer-town-v1.json");
        Company C => state.Current;
        bool CanAct => !state.paused;

        void Awake()
        {
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            basement = Resources.Load<Texture2D>("ScamDen/scamden_basement_sheet");
            neon = Resources.Load<Texture2D>("ScamDen/scamden_neon_sheet");
            diamond = new Texture2D(128, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 128; x++)
            {
                float d = Mathf.Abs((x - 63.5f) / 64) + Mathf.Abs((y - 31.5f) / 32);
                diamond.SetPixel(x, y, d > 1 ? Color.clear : d > .94f ? new Color(.12f,.18f,.2f) : Color.white);
            }
            diamond.Apply();
            Load();
            Rules.MigrateStaff(state);
            Management.Ensure(state);SetupFeedback();
            Log("Welcome to Scammer Town. Build your fictional empire.");
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if(!state.paused) animationTime+=dt;
            Management.Tick(state,dt,()=>UnityEngine.Random.value,(message,amount)=>
            {Log(message);if(amount>0)RewardFeedback(amount);else Notify(message);});
            saveClock += dt;
            if (saveClock > 15) { Save(); saveClock = 0; }
        }

        void Buy(string what, int cost, Action action)
        {
            if (!CanAct) return;
            if (!Rules.Pay(state, cost)) { Notify("Need " + Money(cost) + " to " + what.ToLower() + "."); return; }
            action(); Management.Ensure(state); Log(what + " · −" + Money(cost)); Notify(what + " complete!");PlayFeedback(false); Save();
        }
        void Tier(string name, int current, Action action)
        {
            if (current < 5) Buy("Upgrade " + name, Rules.TierCost(current + 1), action);
        }
        void Transfer()
        {
            if (!state.branchOwned) return;
            Company source = C, destination = state.companies[state.active==0 ? 1 : 0];
            int capacity = destination.Capacity - destination.Stations;
            int movingLaptops = Mathf.Min(capacity, source.laptops);
            int movingDesktops = Mathf.Min(capacity - movingLaptops, source.desktops);
            destination.laptops += movingLaptops; source.laptops -= movingLaptops;
            destination.desktops += movingDesktops; source.desktops -= movingDesktops;
            int phones = Mathf.Min(999 - destination.burners, source.burners);
            destination.burners += phones; source.burners -= phones;
            source.ai = false;
            source.neonTheme=state.neon;
            state.active = state.active==0 ? 1 : 0; state.neon=C.neonTheme; state.cycle = 0;cityView=false;
            Log("Moved " + (movingLaptops + movingDesktops) + " stations and " + phones + " phones to " + CompanyName + "."); Save();
        }
        string CompanyName => Management.Name(state.active);
        static string Money(float amount) => "$" + Mathf.FloorToInt(amount).ToString("N0");
        void Notify(string message) { notice = message; noticeUntil = Time.unscaledTime + 5; }
        void Log(string message) { events.Insert(0, message); if (events.Count > 30) events.RemoveAt(events.Count - 1); }
        public void Save()
        {
            if (SuppressDiskSaves) return;
            Management.Ensure(state);state.Current.neonTheme=state.neon;
            try
            {
                AtomicSave.Write(SavePath, JsonUtility.ToJson(state, true));
                saveStatus = "Saved " + DateTime.Now.ToString("HH:mm");
            }
            catch (Exception e) { saveStatus = "Save failed"; Debug.LogWarning(e.Message); }
        }
        void Load()
        {
            foreach (string path in new[] { SavePath, SavePath + ".bak" })
            {
                if (!File.Exists(path)) continue;
                try { var loaded = JsonUtility.FromJson<TycoonState>(File.ReadAllText(path)); if (Rules.Valid(loaded)) { state = loaded; state.paused = false; saveStatus = "Save restored"; return; } }
                catch (Exception e) { Debug.LogWarning("Cannot load save: " + e.Message); }
            }
        }
        void OnApplicationQuit() => Save();
        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnDestroy() { if (diamond != null) Destroy(diamond);  if(keyboardArt!=null) foreach(var texture in keyboardArt) Destroy(texture); if(employeeArt!=null) foreach(var texture in employeeArt) Destroy(texture); if(typingArmArt!=null) foreach(var texture in typingArmArt) Destroy(texture); foreach(var texture in new[]{uiPanel,uiNormal,uiHover,uiPressed,uiAccent,uiAccentHover,uiAccentPressed,uiDisabled})if(texture!=null)Destroy(texture); if(rewardSound!=null)Destroy(rewardSound);if(buySound!=null)Destroy(buySound); }

        void Styles()
        {
            if (title != null && primaryButton != null && disabledButton != null && dockLabel != null && dockSelectedLabel != null) return;
            // Unity can retain old styles across a live script reload while new fields are null.
            foreach(var texture in new[]{uiPanel,uiNormal,uiHover,uiPressed,uiAccent,uiAccentHover,uiAccentPressed,uiDisabled})
                if(texture!=null)Destroy(texture);
            cityMarkerStyle=null;
            title = new GUIStyle(GUI.skin.label) { fontSize = 29, fontStyle = FontStyle.Bold, normal = { textColor = ink } };
            heading = new GUIStyle(title) { fontSize = 18 };
            body = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true, normal = { textColor = ink } };
            small = new GUIStyle(body) { fontSize = 13, normal = { textColor = muted } };
            stat = new GUIStyle(title) { fontSize = 25, normal = { textColor = lime } };
            button = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle=FontStyle.Bold, wordWrap = true, alignment = TextAnchor.MiddleCenter, padding = new RectOffset(12, 12, 5, 5), normal = { textColor = ink } };
            uiNormal=PixelSurface(new Color(.12f,.18f,.26f));uiHover=PixelSurface(new Color(.18f,.29f,.37f));uiPressed=PixelSurface(new Color(.075f,.14f,.20f));
            button.normal.background=uiNormal;button.hover.background=uiHover;button.active.background=uiPressed;
            button.onNormal.background=uiPressed;button.onHover.background=uiHover;
            button.border=new RectOffset(8,8,8,8);
            button.hover.textColor=Color.white;button.active.textColor=accent;button.onNormal.textColor=ink;
            button.focused.background=uiHover;button.focused.textColor=Color.white;
            button.onActive.background=uiPressed;button.onFocused.background=uiHover;
            button.onHover.textColor=ink;button.onActive.textColor=accent;button.onFocused.textColor=ink;
            uiAccent=PixelSurface(new Color(.28f,.88f,.73f));uiAccentHover=PixelSurface(new Color(.44f,.98f,.83f));
            uiAccentPressed=PixelSurface(new Color(.19f,.69f,.57f));uiDisabled=PixelSurface(new Color(.085f,.12f,.17f));
            primaryButton=new GUIStyle(button);
            SetButtonState(primaryButton.normal,uiAccent,new Color(.025f,.12f,.13f));
            SetButtonState(primaryButton.hover,uiAccentHover,new Color(.025f,.12f,.13f));
            SetButtonState(primaryButton.focused,uiAccentHover,new Color(.025f,.12f,.13f));
            SetButtonState(primaryButton.active,uiAccentPressed,new Color(.025f,.12f,.13f));
            disabledButton=new GUIStyle(button);
            SetButtonState(disabledButton.normal,uiDisabled,new Color(.56f,.64f,.72f));
            dockLabel=new GUIStyle(body){alignment=TextAnchor.MiddleCenter,fontSize=14,fontStyle=FontStyle.Bold};
            dockSelectedLabel=new GUIStyle(dockLabel);dockSelectedLabel.normal.textColor=new Color(.025f,.12f,.13f);
            uiPanel=PixelSurface(Color.white);
            card = new GUIStyle(GUI.skin.box) { normal = { background = uiPanel }, border=new RectOffset(8,8,8,8) };
        }
        void Panel(Rect r, Color c)
        {
            Color old=GUI.color;
            if(r.width<32 || r.height<32 || r.width>=1440){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);}
            else
            {
                GUI.color=new Color(0,0,0,c.a*.22f);GUI.Box(new Rect(r.x,r.y+3,r.width,r.height),GUIContent.none,card);
                GUI.color=c;GUI.Box(r,GUIContent.none,card);
            }
            GUI.color=old;
        }
        void Label(float x, float y, float w, float h, string text, GUIStyle style = null) => GUI.Label(new Rect(x,y,w,h), text, style ?? body);
        static void SetButtonState(GUIStyleState target,Texture2D background,Color color)
        { target.background=background;target.textColor=color; }
        bool Button(float x, float y, float w, string text, bool enabled = true, float height = 40, bool primary = false)
        {
            bool old = GUI.enabled; GUI.enabled = old && enabled;
            bool hit = GUI.Button(new Rect(x,y,w,height), text, !GUI.enabled?disabledButton:primary?primaryButton:button); GUI.enabled = old; return hit;
        }
        void Bar(float x, float y, float w, float value, Color color)
        { Panel(new Rect(x,y,w,5), new Color(.14f,.2f,.28f)); Panel(new Rect(x,y,w * Mathf.Clamp01(value / 100),5),color); }

        void OnGUI()
        {
            Styles();
            if(Event.current.type==EventType.KeyDown && Event.current.keyCode==KeyCode.Escape)
            {
                if(confirmReset)confirmReset=false;
                else if(help)help=false;
                else if(furnitureStorageOpen)furnitureStorageOpen=false;
                else if(workerModal)workerModal=false;
                else if(shopModal)shopModal=false;
                else if(managementPanel){managementPanel=false;GUIUtility.keyboardControl=0;}
                else if(heldItem!=null)CancelPlacement();
                else if(arrangingRoom){arrangingRoom=false;CancelPlacement();}
                else if(cityView){cityView=false;tab="Operations";}
                Event.current.Use();
            }
            if(Event.current.type==EventType.KeyDown && !help && !confirmReset && !workerModal && !shopModal && !managementPanel && !furnitureStorageOpen && GUIUtility.keyboardControl==0)
            {
                var key=Event.current.keyCode;
                if(key==KeyCode.C || key==KeyCode.Alpha4){if(cityView)VisitSelectedApartment();else OpenCity();Event.current.Use();}
                else if(key==KeyCode.Alpha1){Navigate("Operations");Event.current.Use();}
                else if(key==KeyCode.Alpha2){Navigate("Employees");Event.current.Use();}
                else if(key==KeyCode.Alpha3){OpenShop(0);Event.current.Use();}
            }
            // Keep text readable in small windows; scroll instead of shrinking indefinitely.
            float scale = Mathf.Max(.85f, Mathf.Min(Screen.width / 1440f, Screen.height / 900f));
            GUI.matrix = Matrix4x4.TRS(new Vector3(Mathf.Max(0,(Screen.width - 1440 * scale)/2), Mathf.Max(0,(Screen.height - 900 * scale)/2),0), Quaternion.identity, new Vector3(scale,scale,1));
            bool smallViewport=Screen.width/scale<1440 || Screen.height/scale<900;
            if(smallViewport)viewportScroll=GUI.BeginScrollView(new Rect(0,0,Screen.width/scale,Screen.height/scale),viewportScroll,new Rect(0,0,1440,900));
            Panel(new Rect(0,0,1440,900), new Color(.035f,.05f,.09f));
            bool enabled=GUI.enabled;
            GUI.enabled=!help && !confirmReset && !workerModal && !shopModal && !managementPanel && !furnitureStorageOpen;
            if(cityView)DrawCity();else DrawRoom();
            DrawTopBar();DrawBottomDock();DrawCompactAlert();
            GUI.enabled=enabled;
            if(managementPanel)DrawInspector();
            if(shopModal)DrawShopModal();
            if(workerModal)DrawEmployeeModal();
            if(furnitureStorageOpen)DrawFurnitureStorage();
            if (help) DrawHelp();
            if (confirmReset) DrawReset();
            if (Time.unscaledTime < noticeUntil) { Panel(new Rect(380,684,670,48),new Color(.24f,.15f,.075f)); Label(398,690,634,36,notice); }
            if(smallViewport)GUI.EndScrollView();
            GUI.matrix=Matrix4x4.identity;
        }

        void DrawInspector()
        {
            Panel(new Rect(0,0,1440,900),new Color(.015f,.025f,.05f,.78f));
            Rect window=new Rect(420,130,600,640);
            Panel(window,new Color(.065f,.095f,.16f));
            string caption=tab=="Employees" ? "TEAM" : tab=="Properties" ? "EXPAND OFFICE" : tab=="Overview" ? "YOUR OFFICE" : tab.ToUpper();
            Label(450,152,420,36,caption,heading);
            if(Button(886,150,104,"Close",true,36))managementPanel=false;
            GUILayout.BeginArea(new Rect(450,210,540,520));scroll=GUILayout.BeginScrollView(scroll);
            switch(tab)
            {
                case "Contracts":DrawContracts();break;
                case "Overview":
                case "Operations":DrawApartmentSummary();break;
                case "Employees":DrawTeamList();break;
                case "Equipment":Action("Open shop",()=>OpenShop(0));break;
                case "Goals":
                    Text(Management.Goal(state));
                    if(state.goal<5)PrimaryAction("Claim "+Money(Management.GoalReward(state.goal)),()=>
                    {
                        int reward=Management.GoalReward(state.goal);
                        if(Management.ClaimGoal(state)){RewardFeedback(reward);Save();}
                    },CanAct && GoalReady);
                    Text(GoalReady ? "Your reward is ready to collect." : "Complete this milestone to unlock its reward.",true);
                    Space();Text("RECENT ACTIVITY");
                    foreach(var entry in events)Text(entry,true);
                    break;
                case "Settings":
                    Text(saveStatus+" · Autosaves every 15 seconds",true);
                    Action("Save now",()=>{Save();Notify(saveStatus);});
                    Action(state.neon ? "Lighting: Neon" : "Lighting: Amber",()=>{state.neon=!state.neon;Save();});
                    Action(state.muted ? "Sound: Off" : "Sound: On",()=>{state.muted=!state.muted;Save();});
                    Action("How to play",()=>{managementPanel=false;help=true;});
                    Text("Keyboard: 1 Office · 2 Team · 3 Shop · 4 City. Esc closes a panel.",true);
                    Action("Start a new game",()=>{managementPanel=false;confirmReset=true;});break;
                case "Properties":
                    Text(CompanyName);Text(Management.Benefit(state.active));
                    Text(C.Size+" × "+C.Size+" · "+C.Capacity+" workstation slots");
                    DrawFloorExpansion();
                    Action("View all properties",OpenCity);
                    if(state.branchOwned)Action("Move equipment & visit · $250",()=>Buy("Relocate",250,Transfer),CanAct && state.cash>=250);
                    break;
            }
            GUILayout.EndScrollView();GUILayout.EndArea();
            if(Event.current.type==EventType.MouseDown && !window.Contains(Event.current.mousePosition)){managementPanel=false;Event.current.Use();}
        }
        void Text(string text,bool dim=false) { GUILayout.Label(text,dim ? small : body); GUILayout.Space(10); }
        void Space() => GUILayout.Space(18);
        void Action(string text,Action action,bool enabled=true,bool primary=false)
        {
            bool previous=GUI.enabled; GUI.enabled=previous && enabled;
            if(GUILayout.Button(text,!GUI.enabled?disabledButton:primary?primaryButton:button,GUILayout.MinHeight(46))) action();
            GUI.enabled=previous; GUILayout.Space(10);
        }
        void TierPanel(string label,int tier,Action upgrade)
        {
            Text(label + " · " + (tier==0 ? "NONE" : "TIER " + tier));
            for(int i=1;i<=5;i++) Text((i==tier ? "● " : "○ ") + "Tier " + i + "    " + Rules.Chance(i) + "%",i!=tier);
            if(tier<5) Action("Upgrade · " + Money(Rules.TierCost(tier+1)),upgrade,CanAct);
            else Text("Maximum tier reached."); Space();
        }
        void Modal(string caption)
        {
            Panel(new Rect(0,0,1440,900),new Color(.015f,.025f,.05f,.78f));
            Panel(new Rect(430,210,580,480),new Color(.065f,.095f,.16f));
            Label(462,240,520,45,caption,title);
        }
        void DrawHelp()
        {
            Modal("HOW TO PLAY");
            Label(462,300,515,290,"1. Click an employee → Assignment → choose a job. It repeats automatically.\n\n2. Shop: buy workstations, hire employees, or improve your office.\n\n3. Expand floor: make room for more desks. Use Arrange furniture to move items; R rotates.\n\n4. City: buy and switch between apartments. Every owned office keeps working while the game runs.\n\nScroll to zoom. Drag empty floor to pan. Esc closes panels. No offline income.");
            if(Button(462,620,516,"Back to town")) help=false;
        }
        void DrawReset()
        {
            Modal("START A NEW GAME?");
            Label(462,310,515,100,"This replaces your saved progress with a fresh company. Your project files and artwork are unaffected.");
            if(Button(462,460,516,"Keep playing")) confirmReset=false;
            if(Button(462,520,516,"Reset saved progress")) { state=new TycoonState();Rules.MigrateStaff(state);Management.Ensure(state);selectedWorker=0;workerModal=false;cityView=false;events.Clear();confirmReset=false;Save();Log("New company established."); }
        }
    }
}
