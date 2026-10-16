using UnityEngine;

namespace ScammerTown
{
    public static class EmpireProgression
    {
        public static readonly string[] UpgradeNames={"Call-center headsets","Office network","Deal room","Break room"};
        public static readonly string[] UpgradeEffects={"+2 success points per level, all schemes.","+8% job speed per level.","+8% successful take per level; +5% suspicion per level.","+0.04 morale/sec per level; automatic breaks 3s shorter per level."};
        public static readonly string[] PaceNames={"Balanced","Cautious","High pressure"};
        public static readonly string[] PaceEffects={"Normal speed, take and suspicion.","20% slower jobs; 30% less suspicion.","20% faster jobs; +10% successful take; 35% more suspicion."};
        public static int Level(Company c,int upgrade) => upgrade==0?c.headsetUpgrade:upgrade==1?c.networkUpgrade:upgrade==2?c.dealUpgrade:upgrade==3?c.breakUpgrade:-1;
        public static int Cost(Company c,int upgrade) => (upgrade==0?650:upgrade==1?900:upgrade==2?1400:800)*(Level(c,upgrade)+1);
        public static bool Buy(TycoonState s,int upgrade)
        {
            if(upgrade<0 || upgrade>=4 || s.paused || Level(s.Current,upgrade)>=3 || !Rules.Pay(s,Cost(s.Current,upgrade)))return false;
            switch(upgrade){case 0:s.Current.headsetUpgrade++;break;case 1:s.Current.networkUpgrade++;break;case 2:s.Current.dealUpgrade++;break;case 3:s.Current.breakUpgrade++;break;}
            s.Current.deliveryUntil=s.elapsed+10;
            return true;
        }
        public static bool SetPace(TycoonState s,int pace)
        {
            if(s.paused || pace<0 || pace>=PaceNames.Length)return false;
            s.Current.operationPace=pace;return true;
        }
        public static float Speed(Company c) => c.operationPace==1?.8f:c.operationPace==2?1.2f:1;
        public static float Take(Company c) => (1+.08f*c.dealUpgrade)*(c.operationPace==2?1.1f:1)*(OfficeProgression.Placed(c,"cashcounter")?1.05f:1);
        public static float Suspicion(Company c) => (1-.08f*c.securityTier)*(1+.05f*c.dealUpgrade)*(c.operationPace==1?.7f:c.operationPace==2?1.35f:1)*(OfficeProgression.Placed(c,"shredder")?.9f:1);
        public static float BreakSeconds(Company c) => 30-3*c.breakUpgrade-(OfficeProgression.Placed(c,"scanner")?5:0);
        public static int Pay(Company c,int office,int job,bool won) => Mathf.RoundToInt(Management.Jobs[job].pay*(won?(office==2?1.15f:1)*Take(c):.15f));
        public static int RequiredRuns(int job) => job<2?0:job==2?5:job==3?10:job==4?20:35;
        public static int RequiredComputer(int job) => job<3?0:job-2;
        public static bool Unlocked(TycoonState s,int office,int job) => job>=0 && job<Management.Jobs.Length && office>=0 && office<s.companies.Length && Management.ScamProgress(s)>=RequiredRuns(job) && s.companies[office].computerUpgrade>=RequiredComputer(job);
        public static string Requirement(int job) => RequiredRuns(job)==0?"Available now":RequiredRuns(job)+" scam runs"+(RequiredComputer(job)>0?" + computers Lv "+RequiredComputer(job):"");
        public static int ContractTarget(int job) => job==0?5:job==1?4:job==5?2:3;
        public static int ContractReward(int job) => job==0?400:job==1?700:job==2?1000:job==3?1200:job==4?1800:2600;
        public static string ComputerName(int tier) => tier==0?"Starter computers":tier==1?"Flat screens & slim laptops":tier==2?"Dual-screen workstations":"Triple-screen command desks";
        public static string EquipmentAtlas(int appearance,bool staffed) => !staffed?"equipment_empty":appearance==2?"equipment_vest":appearance==3?"equipment_beanie":appearance==4?"equipment_leather":"equipment_crew";
        public static int EquipmentRow(string kind,int tier) => (kind=="laptop"?3:0)+Mathf.Clamp(tier,1,3)-1;
    }
}
