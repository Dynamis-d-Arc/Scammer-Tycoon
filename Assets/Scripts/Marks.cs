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

        // 16×16 portrait, row 0 at the top. Features follow the mark's seed and type.
        public static Color32[] Portrait(Mark mark)
        {
            const int n=16;var px=new Color32[n*n];
            int seed=mark.seed;
            Color32 bg=TypeColors[mark.typeIndex];
            Color32[] skins={new Color32(255,214,178,255),new Color32(234,184,140,255),new Color32(198,140,96,255),new Color32(150,100,66,255),new Color32(104,68,44,255)};
            Color32[] hairs={new Color32(30,24,22,255),new Color32(96,60,34,255),new Color32(222,184,96,255),new Color32(184,82,40,255),new Color32(168,168,176,255),new Color32(236,236,240,255)};
            Color32[] shirts={new Color32(196,72,72,255),new Color32(70,110,180,255),new Color32(230,200,90,255),new Color32(90,150,110,255),new Color32(150,96,170,255),new Color32(220,140,170,255)};
            Color32 skin=Pick(skins,Hash(seed,11)),hair=Pick(hairs,Hash(seed,12)),shirt=Pick(shirts,Hash(seed,13));
            Color32 shadow=new Color32((byte)(skin.r*.82f),(byte)(skin.g*.78f),(byte)(skin.b*.78f),255);
            Color32 ink=new Color32(28,22,30,255);
            int style=Hash(seed,14)%5,extra=Hash(seed,15)%6;
            void Set(int x,int y,Color32 c){if(x>=0 && x<n && y>=0 && y<n)px[y*n+x]=c;}
            void Block(int x0,int y0,int x1,int y1,Color32 c){for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)Set(x,y,c);}
            Block(0,0,15,15,bg);
            Block(3,13,12,15,shirt);Block(6,11,9,12,skin);Set(6,12,shadow);Set(9,12,shadow);
            Block(4,3,11,10,skin);Block(5,10,10,10,shadow);Set(3,6,skin);Set(12,6,skin);Set(3,7,shadow);Set(12,7,shadow);
            if(style==1 || style==3){Block(4,2,11,3,hair);Set(4,4,hair);Set(11,4,hair);}
            if(style==2){Block(4,2,11,3,hair);Block(3,3,4,10,hair);Block(11,3,12,10,hair);}
            if(style==3)Block(6,0,9,1,hair);
            if(style==4){Block(4,1,11,3,hair);for(int x=3;x<=12;x+=2)Set(x,1,bg);Set(3,4,hair);Set(12,4,hair);}
            if(style==0){Set(4,5,hair);Set(11,5,hair);Set(4,4,hair);Set(11,4,hair);}
            // Eyes and expression by type.
            int t=mark.typeIndex;
            Color32 eye=t==0?new Color32(80,200,90,255):ink;
            Set(6,6,eye);Set(9,6,eye);
            if(t==2){Set(6,5,ink);Set(9,5,ink);}
            if(t==3){Set(8,4,hair);Set(9,4,hair);Set(10,4,hair);}
            if(t==5){Set(5,6,shadow);Set(10,6,shadow);Set(6,5,shadow);Set(9,5,shadow);}
            if(t==1){Set(5,8,new Color32(240,130,130,255));Set(10,8,new Color32(240,130,130,255));}
            bool smile=t==0 || t==2 || t==4;
            Set(7,9,ink);Set(8,9,ink);if(smile){Set(6,8,ink);Set(9,8,ink);}
            if(t==4 || extra==0){Block(5,6,7,6,ink);Block(8,6,10,6,ink);Set(5,5,ink);Set(10,5,ink);}
            else if(extra==1){Set(5,6,ink);Set(7,6,ink);Set(8,6,ink);Set(10,6,ink);}
            if(extra==2)Block(6,8,9,8,hair);
            if(extra==3){Block(4,8,11,10,hair);Set(7,9,ink);Set(8,9,ink);}
            if(extra==4){Block(3,2,12,3,shirt);Block(3,3,13,3,ink);}
            return px;
        }
    }
}
