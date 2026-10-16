using UnityEngine;

namespace ScammerTown
{
    // A fictional townsperson on the other end of a run. Marks are derived from a
    // seed saved on the worker, so a run keeps the same mark across reloads.
    public sealed class Mark
    {
        public string first, last, type, belief;
        public int seed, job, typeIndex;
        public string Name => first+" "+last;
        public string Short => first+" "+last[0]+".";
    }

    public static class Marks
    {
        static readonly string[] First={"Gerald","Doris","Mildred","Barry","Nigel","Brenda","Clive","Wanda","Dudley","Peggy","Rupert","Agnes","Lance","Fern","Mortimer","Trudy","Cecil","Marge","Duncan","Bev","Reginald","Opal","Stanley","Gloria","Howard","Pam","Vernon","Shirley"};
        static readonly string[] Last={"Puddleton","Crumb","Wiggins","Flapjack","Bottomley","Pennywhistle","Muddle","Gullworth","Snodgrass","Tumble","Featherby","Noodleman","Biscuit","Honeycutt","Dimwaddle","Plunkett","Grubb","Fairweather","Trustwell","Easymark","Lollipop","Bumbleby"};
        public static readonly string[] Types={"Greedy","Lonely","Gullible","Skeptical","Show-off","Bored"};
        public static readonly string[] TypeBlurbs={
            "Wants it all, wants it by Friday.",
            "Just happy someone called.",
            "Believes the moon is a hologram.",
            "Asked for ID. Accepted a library card.",
            "Needs the neighbors to see it.",
            "Answered because it was Tuesday."
        };
        public static readonly Color[] TypeColors={
            new Color(.42f,.62f,.32f),new Color(.55f,.45f,.68f),new Color(.36f,.58f,.72f),
            new Color(.62f,.4f,.36f),new Color(.78f,.6f,.28f),new Color(.45f,.5f,.56f)
        };
        static readonly int[][] TypesByJob={
            new[]{2,5,1},new[]{0,5,4},new[]{0,4,2},new[]{4,0,3},new[]{5,1,4},new[]{4,0,3}
        };
        static readonly string[][] Beliefs={
            new[]{"thinks they won a jet ski","is claiming a free cruise to Lake Nowhere","won a lifetime supply of soup","won a raffle they never entered","is one form away from a free tractor"},
            new[]{"ordered 'designer' sneakers for $9","pre-ordered a self-folding sweater","found a hot tub at 95% off","bought forty discount air fryers","is waiting on a cashmere toaster cover"},
            new[]{"is all-in on MoonPebble coin","owns 1% of a moon crater (allegedly)","believes in turnip futures","was promised 400% returns, minimum","thinks the club's yacht is real"},
            new[]{"is bidding on a 'Picasso' napkin","wants a duke's left boot","is bidding on a haunted egg, 'probably royal'","thinks the auction house is a castle","needs that 'antique' plastic chair"},
            new[]{"bought front row for a band that split in 1987","has VIP passes to a festival in a parking lot","is going to the Moon Opera, row A","bought tickets to a sold-out hologram","has backstage passes to their own garage"},
            new[]{"is buying the Queen's 'spare' teaspoons","thinks the coin is from Atlantis","is buying the crown jewels' cousin","wants a knighthood certificate, laminated","bought a dragon egg, with certificate"}
        };
        static readonly string[][] Stages={
            new[]{"{crew} → {mark}: \"Congratulations, you've WON!\"","{mark} is reading the prize email twice.","{mark} agreed to a small 'release fee'.","{mark} is hovering over Confirm…"},
            new[]{"{crew} opened a 95%-off flash sale for {mark}.","{mark} added everything to the cart.","{mark} is reading the (very fake) reviews.","{mark} is at checkout…"},
            new[]{"{crew} invited {mark} to an 'exclusive' club.","{mark} watched the hype video. Twice.","{mark} told the whole family about it.","{mark} is going all-in…"},
            new[]{"{crew} sent {mark} a velvet-lined catalog.","{mark} is in a bidding war with a bot.","{mark} raised the bid. Again.","Going once… going twice… {mark} bids again!"},
            new[]{"{crew} posted 'LAST TICKETS!' for {mark}.","{mark} is panicking about selling out.","{mark} picked VIP. Of course.","{mark} is at checkout, heart racing…"},
            new[]{"{crew} slipped {mark} a 'royal' certificate.","{mark} is inspecting the wax seal.","{mark} called a cousin who 'knows antiques'.","{mark} is signing for the heirloom…"}
        };
        static readonly string[] Payoffs={"The jet ski is 'in the mail'.","The parcel is 'delayed'.","To the moon! (Not really.)","Sold, to the lucky mark!","Enjoy the show. There is no show.","Long live the mark."};
        static readonly string[] Partials={"{mark} got cold feet. Scraped {pay}.","{mark} smelled a rat. Only {pay}.","{mark} hung up halfway. {pay} salvaged.","{mark}'s nephew 'knows computers'. {pay}."};
        static readonly string[] OpenBarks={"Hello, is this {mark}?","Congratulations, {mark}!","Big news, {mark}!","Do you have a minute, {mark}?","This is a courtesy call…"};
        static readonly string[] MiddleBarks={"{mark}'s biting!","No, no, totally legit.","Trust me, I'm a professional.","Yes, that is a real fax number.","Hold please…","Mm-hm. Mm-hm. Wonderful."};
        static readonly string[] CloseBarks={"Just one tiny fee…","Reel it in…","Almost there…","Read me the numbers again?","Lovely doing business!"};
        static readonly string[] WinBarks={"KA-CHING!","Too easy.","Another happy customer!","Pleasure doing business!","Ring the bell!"};
        static readonly string[] LoseBarks={"They hung up on me…","Cold feet. Ugh.","So close!","Who says 'scam' out loud?!"};
        static readonly string[] LockdownBarks={"Act natural.","Nobody say anything.","Lights off!","We're a book club now."};
        static readonly string[] ParanoidBarks={"Was that a siren?","Close the blinds…","That van's been there all day.","Why is the pizza guy wearing a badge?"};
        static readonly string[] BreakBarks={"Coffee. Now.","Five minutes, boss.","My voice is gone."};
        static readonly string[] IdleBarks={"Got any leads?","Who's next on the board?","Ready when you are, boss."};
        static readonly string[] Fluff={
            "LOCAL CAT ELECTED TREASURER","MAN WINS RAFFLE, SUSPECTS NOTHING","COUNCIL DEBATES SECOND TRAFFIC LIGHT",
            "PIGEON STEALS ENTIRE BAGEL","AREA MAN 'VERY GOOD WITH COMPUTERS', SAYS AREA MAN"
        };
        static readonly string[] SchemeHeadlines={
            "LOCALS WARNED: YOU DID NOT WIN A JET SKI","'95% OFF' SHOP HAS NO ADDRESS, SHOPPERS FIND",
            "MOONPEBBLE COIN 'DEFINITELY GOING UP', SAYS MAN WHO OWNS IT","AUCTION 'PICASSO' REVEALED TO BE A NAPKIN",
            "FESTIVAL-GOERS FIND ONLY A PARKING LOT","ROYAL FAMILY DENIES SELLING SPOONS ONLINE"
        };
        static readonly string[] EventHeadlines={
            "","NEON FESTIVAL OPENS; TICKET DEMAND 'SUSPICIOUSLY HIGH'","MARKET DAY: BARGAIN HUNTERS OUT IN FORCE",
            "TOWN WATCH LAUNCHES 'KNOW YOUR CALLER' WEEK","COLLECTORS' WEEK DRAWS BIG SPENDERS"
        };

        static int Hash(int a,int b)
        {
            unchecked
            {
                uint h=2166136261;
                h=(h^(uint)a)*16777619;h=(h^(uint)b)*16777619;
                h^=h>>15;h*=2246822519;h^=h>>13;
                return (int)(h&0x7fffffff);
            }
        }
        static int Hash(string text)
        {
            unchecked{uint h=2166136261;if(text!=null)foreach(char ch in text)h=(h^ch)*16777619;return (int)(h&0x7fffffff);}
        }
        static T Pick<T>(T[] items,int seed) => items[seed%items.Length];

        public static Mark For(int seed,int job)
        {
            job=Mathf.Clamp(job,0,Beliefs.Length-1);
            int type=Pick(TypesByJob[job],Hash(seed,3));
            return new Mark{seed=seed,job=job,typeIndex=type,type=Types[type],
                first=Pick(First,Hash(seed,1)),last=Pick(Last,Hash(seed,2)),belief=Pick(Beliefs[job],Hash(seed,4))};
        }
        public static Mark Current(Worker w) => w==null || w.job<0 ? null : For(w.markSeed,w.job);

        public static int StageCount => 4;
        public static int Stage(Worker w)
        {
            if(w==null || w.job<0 || w.duration<=0)return -1;
            float progress=1-w.remaining/w.duration;
            return progress<.28f?0:progress<.56f?1:progress<.82f?2:3;
        }
        static string Fill(string template,string crew,Mark mark,int pay=0) =>
            template.Replace("{crew}",crew).Replace("{mark}",mark.first).Replace("{pay}","$"+pay.ToString("N0"));
        public static string StageLine(Worker w,Mark mark,int stage) => Fill(Stages[mark.job][Mathf.Clamp(stage,0,StageCount-1)],w.name,mark);
        public static string Outcome(Worker w,Mark mark,bool won,int pay) =>
            won?mark.first+" paid $"+pay.ToString("N0")+". "+Payoffs[mark.job]:Fill(Pick(Partials,Hash(mark.seed,5)),w.name,mark,pay);

        // Short speech-bubble lines. Reactions always show; ambient chatter appears
        // for a few seconds out of every nine, staggered per desk.
        public static string Bark(Company c,Worker w,int slot,float time)
        {
            if(w==null)return null;
            if(w.reaction>0)
            {
                if(w.status!=null && w.status.StartsWith("Scam landed"))return Pick(WinBarks,Hash(Hash(w.name),w.completed));
                if(w.status!=null && w.status.StartsWith("Partial"))return Pick(LoseBarks,Hash(Hash(w.name),w.completed));
                return null;
            }
            float clock=time+slot*3.7f;
            if(Mathf.Repeat(clock,9)>2.6f)return null;
            int beat=Hash(Hash(w.name),Mathf.FloorToInt(clock/9));
            var mark=Current(w);
            if(w.breakRemaining>0)return Pick(BreakBarks,beat);
            if(c.lockdown>0)return Pick(LockdownBarks,beat);
            if(c.suspicion>=75 && beat%2==0)return Pick(ParanoidBarks,beat);
            if(mark==null)return Pick(IdleBarks,beat);
            int stage=Stage(w);
            return Fill(Pick(stage<=0?OpenBarks:stage<3?MiddleBarks:CloseBarks,beat),w.name,mark);
        }

        public static string Headline(TycoonState s,out string kicker)
        {
            float suspicion=0;int scheme=-1;float resistance=0;
            for(int i=0;i<s.companies.Length;i++)
            {
                if(!Management.Owned(s,i))continue;
                var c=s.companies[i];suspicion=Mathf.Max(suspicion,c.suspicion);
                if(c.townResistance!=null)for(int j=0;j<c.townResistance.Length && j<SchemeHeadlines.Length;j++)
                    if(c.townResistance[j]>resistance){resistance=c.townResistance[j];scheme=j;}
            }
            int evt=LivingTown.EventAt(s.elapsed);
            if(s.lastRaidAt>=0 && s.elapsed-s.lastRaidAt<Rules.DaySeconds && s.lastRaidOffice>=0)
            {kicker="Officers report finding 'mostly headsets'.";return "POLICE RAID "+Management.Name(s.lastRaidOffice).ToUpper()+" 'BOOK CLUB'";}
            if(Heat.RaidPending(s)){kicker="Plodd declined to comment, then commented at length.";return "DETECTIVE PLODD 'VERY CLOSE', SAYS DETECTIVE PLODD";}
            if(suspicion>=75){kicker="Detective seen buying a new notebook.";return "POLICE 'LOOKING INTO' ODD PHONE CALLS";}
            if(s.caseFile>=50){kicker="She has a corkboard now. It has red string.";return "DETECTIVE ASSIGNED TO PHONE-SCAM WAVE";}
            if(scheme>=0 && resistance>=40){kicker="Town wariness of "+Management.Jobs[scheme].name+" is climbing.";return SchemeHeadlines[scheme];}
            if(s.undercutJob>=0 && s.elapsed<s.undercutUntil){kicker="Velvet Syndicate spokesperson: 'We have no spokesperson.'";return "KNOCK-OFF "+Management.Jobs[s.undercutJob].name.ToUpper()+" FLOODS TOWN";}
            if(suspicion>=40){kicker="Neighbors report 'a lot of phone calls'.";return "BASEMENT NOISE COMPLAINTS UP 300%";}
            if(evt>0){kicker=LivingTown.EventEffects[evt];return EventHeadlines[evt];}
            kicker="Nothing to see here. Probably.";return Pick(Fluff,s.day);
        }

        public const int PortraitSize=32;
        // 32×32 bust in the house style: lit from the upper left, three-tone skin and
        // hair, near-black outline. Row 0 is the top. Features follow the seed and type.
        public static Color32[] Portrait(Mark mark)
        {
            const int n=PortraitSize;var px=new Color32[n*n];var body=new bool[n*n];
            int seed=mark.seed;
            Color bg=TypeColors[Mathf.Clamp(mark.typeIndex,0,TypeColors.Length-1)];
            Color[] skins={new Color(1f,.84f,.7f),new Color(.92f,.72f,.55f),new Color(.78f,.55f,.38f),new Color(.59f,.39f,.26f),new Color(.41f,.27f,.17f)};
            Color[] hairs={new Color(.13f,.1f,.1f),new Color(.38f,.24f,.13f),new Color(.87f,.72f,.38f),new Color(.72f,.32f,.16f),new Color(.66f,.66f,.7f),new Color(.93f,.93f,.95f)};
            Color[] shirts={new Color(.77f,.28f,.28f),new Color(.27f,.43f,.71f),new Color(.9f,.78f,.35f),new Color(.35f,.59f,.43f),new Color(.59f,.38f,.67f),new Color(.86f,.55f,.67f),new Color(.3f,.3f,.36f)};
            Color skin=Pick(skins,Hash(seed,11)),hair=Pick(hairs,Hash(seed,12)),shirt=Pick(shirts,Hash(seed,13));
            Color ink=new Color(.016f,.008f,.004f),white=new Color(.96f,.95f,.92f),mouth=new Color(.55f,.2f,.2f);
            int style=Hash(seed,14)%5,extra=Hash(seed,15)%7,t=mark.typeIndex;
            Color Shade(Color c,float k) => new Color(Mathf.Clamp01(c.r*k),Mathf.Clamp01(c.g*k*(k<1?.96f:1)),Mathf.Clamp01(c.b*k*(k<1?.92f:1)),1);
            bool Inside(float x,float y,float cx,float cy,float rx,float ry) => (x-cx)*(x-cx)/(rx*rx)+(y-cy)*(y-cy)/(ry*ry)<=1;
            void Put(int x,int y,Color c,bool part=true){if(x<0 || y<0 || x>=n || y>=n)return;px[y*n+x]=c;if(part)body[y*n+x]=true;}
            // Background: the mark type's colour, a little lighter at the top.
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)px[y*n+x]=Shade(bg,1.08f-.22f*y/n);
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float fx=x+.5f,fy=y+.5f;
                if(fy>=23 && Inside(fx,fy,16,34,13.5f,10.5f))
                {
                    Color c=fx>20?Shade(shirt,.78f):fx<9?Shade(shirt,1.08f):shirt;
                    if(Mathf.Abs(fx-16)<(fy-22)*.55f && fy<28)c=Shade(skin,.82f);
                    Put(x,y,c);
                }
                else if(fx>12.5f && fx<19.5f && fy>=19 && fy<25)Put(x,y,Shade(skin,fx>17?.7f:.8f));
                if(Inside(fx,fy,16,13.5f,6.6f,8.2f) || Inside(fx,fy,9.3f,14.5f,1.4f,2.1f) || Inside(fx,fy,22.7f,14.5f,1.4f,2.1f))
                {
                    Color c=skin;
                    if(fx>19.5f || fy>19.5f)c=Shade(skin,.8f);
                    else if(fx<13 && fy<12)c=Shade(skin,1.07f);
                    Put(x,y,c);
                }
            }
            // Hair, with a one-pixel rim light along its upper-left edge.
            var hairMask=new bool[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float fx=x+.5f,fy=y+.5f;bool h=false;
                bool crown=Inside(fx,fy,16,12.5f,7.6f,9f) && fy<9.5f+.9f*Mathf.Sin(fx*1.4f);
                bool sides=Inside(fx,fy,16,12.5f,7.6f,9f) && Mathf.Abs(fx-16)>5.6f && fy<13.5f;
                if(style==0)h=Inside(fx,fy,16,13.5f,7.2f,8.6f) && Mathf.Abs(fx-16)>5.2f && fy>10 && fy<15;
                if(style==1 || style==3)h=crown || sides;
                if(style==2)h=crown || (Inside(fx,fy,16,15,8.8f,11.5f) && Mathf.Abs(fx-16)>5.4f && fy<25);
                if(style==3)h|=Inside(fx,fy,16,3.6f,3.2f,3);
                if(style==4)foreach(var c in new[]{new Vector2(9.5f,8),new Vector2(12,5),new Vector2(16,4),new Vector2(20,5),new Vector2(22.5f,8),new Vector2(8.8f,11.5f),new Vector2(23.2f,11.5f)})
                    h|=Inside(fx,fy,c.x,c.y,2.8f,2.8f) && fy<13;
                if(!h)continue;
                hairMask[y*n+x]=true;
                Put(x,y,fx>19?Shade(hair,.72f):hair);
            }
            for(int y=1;y<n;y++)for(int x=1;x<n;x++)
                if(hairMask[y*n+x] && x<17 && (!hairMask[(y-1)*n+x] || !hairMask[y*n+x-1]))px[y*n+x]=Shade(hair,1.4f);
            if(style==0){Put(12,7,Shade(skin,1.18f),false);Put(13,6,Shade(skin,1.18f),false);}
            // Hat.
            if(extra==4)for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float fx=x+.5f,fy=y+.5f;
                if(Inside(fx,fy,16,8.5f,7.8f,5.5f) && fy<8.5f)Put(x,y,fx>19?Shade(shirt,.75f):shirt);
                else if(fy>=8 && fy<9.6f && fx>7 && fx<26)Put(x,y,Shade(shirt,.6f));
            }
            // Face: brows, eyes and expression by mark type.
            Color brow=Shade(hair,.55f),pupil=t==0?new Color(.3f,.78f,.35f):ink;
            void Eye(int x,bool wide)
            {
                Put(x,14,white,false);Put(x+1,14,pupil,false);
                if(wide){Put(x,13,white,false);Put(x+1,13,white,false);Put(x,15,white,false);Put(x+1,15,pupil,false);}
            }
            if(t==4)
            {
                for(int x=10;x<=21;x++)Put(x,14,ink,false);
                for(int x=10;x<=14;x++)Put(x,15,ink,false);for(int x=17;x<=21;x++)Put(x,15,ink,false);
                Put(11,14,new Color(.55f,.6f,.8f),false);Put(18,14,new Color(.55f,.6f,.8f),false);
            }
            else if(t==3)
            {
                Eye(18,false);for(int x=12;x<=13;x++)Put(x,14,ink,false);
                Put(17,11,brow,false);Put(18,11,brow,false);Put(19,12,brow,false);Put(12,12,brow,false);Put(13,12,brow,false);
            }
            else
            {
                Eye(12,t==2);Eye(18,t==2);
                if(t==5){Put(12,13,Shade(skin,.75f),false);Put(13,13,Shade(skin,.75f),false);Put(18,13,Shade(skin,.75f),false);Put(19,13,Shade(skin,.75f),false);}
                int browY=t==2?11:12;
                Put(12,browY,brow,false);Put(13,browY,brow,false);Put(18,browY,brow,false);Put(19,browY,brow,false);
                if(t==1){Put(11,browY+1,brow,false);Put(20,browY+1,brow,false);}
            }
            if(extra==1 && t!=4){for(int x=11;x<=14;x++){Put(x,13,ink,false);Put(x,16,ink,false);}for(int x=17;x<=20;x++){Put(x,13,ink,false);Put(x,16,ink,false);}Put(15,14,ink,false);Put(16,14,ink,false);}
            Put(16,16,Shade(skin,.74f),false);Put(16,17,Shade(skin,.74f),false);Put(17,17,Shade(skin,.68f),false);
            if(t==1){Put(11,17,new Color(.95f,.55f,.55f),false);Put(12,17,new Color(.95f,.55f,.55f),false);Put(19,17,new Color(.95f,.55f,.55f),false);Put(20,17,new Color(.95f,.55f,.55f),false);}
            if(extra==2)for(int x=14;x<=18;x++)Put(x,18,Shade(hair,.85f),false);
            if(extra==3)for(int y=17;y<=21;y++)for(int x=10;x<=21;x++)if(Inside(x+.5f,y+.5f,16,13.5f,6.6f,8.2f))Put(x,y,x>19?Shade(hair,.72f):hair,false);
            if(t==2){Put(15,19,ink,false);Put(16,19,ink,false);Put(15,20,mouth,false);Put(16,20,mouth,false);}
            else if(t==4){for(int x=14;x<=18;x++)Put(x,19,white,false);Put(13,18,ink,false);Put(19,18,ink,false);}
            else if(t==0){Put(15,19,ink,false);Put(16,19,ink,false);Put(17,19,ink,false);Put(18,18,ink,false);}
            else if(t==1){Put(15,19,mouth,false);Put(16,19,mouth,false);Put(14,20,mouth,false);Put(17,20,mouth,false);}
            else{Put(15,19,mouth,false);Put(16,19,mouth,false);Put(17,19,mouth,false);}
            // Near-black outline around the figure, as on every sprite in the game.
            var outline=new bool[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                if(body[y*n+x])continue;
                bool edge=(x>0 && body[y*n+x-1]) || (x<n-1 && body[y*n+x+1]) || (y>0 && body[(y-1)*n+x]) || (y<n-1 && body[(y+1)*n+x]);
                if(edge)outline[y*n+x]=true;
            }
            for(int i=0;i<px.Length;i++)if(outline[i])px[i]=ink;
            return px;
        }
    }
}
