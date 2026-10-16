using UnityEngine;

namespace ScammerTown
{
    // Scam proceeds arrive dirty. cash is the total; dirty is the part of it that
    // still needs washing (0 <= dirty <= cash). Everyday spending uses dirty money
    // first; properties, floor space and fronts need clean money.
    public static class Laundering
    {
        public static readonly string[] Fronts={"Suds & Duds Laundromat","Sparkle Car Wash","Totally Legit Pizza"};
        public static readonly string[] FrontBlurbs={"Washes clothes. Mostly money.","The cars are clean. The money is cleaner.","Nobody has ever eaten the pizza."};
        public static readonly int[] FrontCost={2500,8000,20000};
        public static readonly int[] FrontDaily={1500,4000,10000};
        // Position on the city artwork (1635 × 962 source pixels).
        public static readonly Vector2[] FrontMapPoints={new Vector2(844,600),new Vector2(1013,680),new Vector2(1287,488)};
        public const float Fee=.1f;

        public static float Clean(TycoonState s) => Mathf.Max(0,s.cash-s.dirty);
        public static void Income(TycoonState s,float amount)
        {
            if(amount<=0)return;
            s.cash+=amount;s.dirty+=amount;
        }
        public static void Spend(TycoonState s,float amount)
        {
            s.cash-=amount;s.dirty=Mathf.Clamp(s.dirty-amount,0,Mathf.Max(0,s.cash));
        }
        public static bool PayClean(TycoonState s,float amount)
        {
            if(amount<0 || Clean(s)<amount)return false;
            s.cash-=amount;s.dirty=Mathf.Min(s.dirty,s.cash);return true;
        }
        public static void Seize(TycoonState s,float amount)
        {
            amount=Mathf.Clamp(amount,0,s.dirty);s.cash-=amount;s.dirty-=amount;
        }
        public static bool Owns(TycoonState s,int front) => front>=0 && front<Fronts.Length && (s.fronts&(1<<front))!=0;
        public static bool BuyFront(TycoonState s,int front)
        {
            if(s.paused || front<0 || front>=Fronts.Length || Owns(s,front) || !PayClean(s,FrontCost[front]))return false;
            s.fronts|=1<<front;return true;
        }
        public static float DailyCapacity(TycoonState s)
        {
            float total=0;for(int f=0;f<Fronts.Length;f++)if(Owns(s,f))total+=FrontDaily[f];return total;
        }
        // Fronts wash a steady trickle, keeping a 10% "processing fee".
        public static void Advance(TycoonState s,float dt)
        {
            float amount=Mathf.Min(s.dirty,DailyCapacity(s)/Rules.DaySeconds*dt);
            if(amount<=0)return;
            float fee=amount*Fee;
            s.dirty-=amount;s.cash-=fee;s.laundered+=amount-fee;s.dirty=Mathf.Min(s.dirty,s.cash);
        }
    }
}
