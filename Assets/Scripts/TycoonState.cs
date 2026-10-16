using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScammerTown
{
    [Serializable] public class Company
    {
        public int level = 1, desktops = 1, laptops, burners = 3;
        public int employeeTier = 1, productTier = 1, securityTier;
        public float heat;
        public float suspicion;
        public int campaignSkills;
        public bool ai, neonTheme;
        public int employeeCount = 1;
        public List<RoomItem> layout;
        public List<Worker> workers;
        public int computerUpgrade, comfortUpgrade, lightingUpgrade, decorUpgrade;
        public float earned, lockdown;
        public bool heatWarned;
        public int furnishingLayoutVersion;
        public List<DecorationOwned> decorations;
        public int Size => level * 5;
        public int Capacity => Size * Size / 5;
        public int Stations => desktops + laptops;
    }

    [Serializable] public class TycoonState
    {
        public int version = 1, active, day = 1, attempts, wins;
        public int staffVersion;
        public float cash = 4820, energy = 100, elapsed, cycle, raidSeconds = -1;
        public bool neon, paused;
        public Company[] companies = { new Company(), new Company() };
        public bool branchOwned, harborOwned, muted;
        public int idleVersion, managementVersion, goal, jobsCompleted, lastBillDay, incidentCompany=-1;
        public ShortContract contract;
        public bool contractActive;
        public int contractsCompleted;
        public float contractCooldown;
        public float totalJobIncome, nextIncident, incidentCountdown, recoveryCooldown, recoveryRemaining;
        public Company Current => companies[active];
    }

    public static class Rules
    {
        public static readonly int[] Rates = { 0, 20, 30, 40, 50, 67 };
        public const float CycleSeconds = 10, DaySeconds = 120;
        public static int StaffedStations(Company company) => Mathf.Min(company.Stations, company.employeeCount, 20);
        public static void MigrateStaff(TycoonState state)
        {
            if(state.staffVersion >= 1) return;
            foreach(var company in state.companies) company.employeeCount = Mathf.Max(1,company.employeeCount);
            state.companies[0].neonTheme = false;
            state.companies[1].neonTheme = true;
            state.companies[state.active].neonTheme = state.neon;
            state.staffVersion = 1;
        }
        public static bool BuyApartment(TycoonState state)
        {
            if(state.branchOwned || state.paused || !Pay(state,3000)) return false;
            state.companies[1] = new Company { neonTheme=true };
            RoomLayout.Ensure(state.companies[1]);
            state.branchOwned=true;
            return true;
        }
        public static bool VisitApartment(TycoonState state,int index)
        {
            if(index<0 || index>=state.companies.Length || !Management.Owned(state,index)) return false;
            if(index!=state.active)
            {
                state.Current.neonTheme=state.neon;

                state.active=index;
                state.neon=state.Current.neonTheme;
                state.cycle=0;
            }
            return true;
        }
        public static int Chance(int tier) => Rates[Mathf.Clamp(tier, 0, 5)];
        public static int TierCost(int nextTier) => nextTier * nextTier * 500;
        public const int MaxOfficeLevel = 20;
        public static int ExpansionCost(int level) => level * level * 1500;
        public static bool Pay(TycoonState s, float amount)
        {
            if (amount < 0 || s.cash < amount) return false;
            s.cash -= amount;
            return true;
        }
        public static bool Valid(TycoonState s)
        {
            if(s!=null && (!Finite(s.contractCooldown) || s.contractCooldown<0 || s.contractsCompleted<0))return false;
            if(s!=null && s.contractActive)
            {
                var contract=s.contract;
                if(contract==null)return false;
                if(contract.job<0 || contract.job>2 || contract.company<0 || contract.company>2 || !Management.Owned(s,contract.company) || contract.target!=(contract.job==0?5:contract.job==1?4:3) || contract.reward!=(contract.job==0?400:contract.job==1?700:1000) || contract.progress<0 || contract.progress>=contract.target || !Finite(contract.deadline) || contract.deadline<0)return false;
            }
            if (s == null || s.version != 1 || s.companies == null || (s.companies.Length != 2 && s.companies.Length != 3) || s.active < 0 || s.active >= s.companies.Length || !Management.Owned(s,s.active)) return false;
            if (!Finite(s.cash) || s.cash < 0 || !Finite(s.elapsed) || s.elapsed < 0 || !Finite(s.cycle) || s.cycle < 0 || s.cycle > CycleSeconds) return false;
            foreach (var c in s.companies)
                if (c == null || c.employeeCount < 0 || c.employeeCount > 2000 || c.level < 1 || c.level > MaxOfficeLevel || c.desktops < 0 || c.laptops < 0 || c.Stations > c.Capacity || c.burners < 0 || c.burners > 999 || c.employeeTier < 1 || c.employeeTier > 5 || c.productTier < 1 || c.productTier > 5 || c.securityTier < 0 || c.securityTier > 5) return false;
            if(s.managementVersion>0)
            {
                if(s.goal<0 || s.goal>5 || s.jobsCompleted<0 || !Finite(s.totalJobIncome) || s.totalJobIncome<0 || !Finite(s.nextIncident) || !Finite(s.incidentCountdown) || !Finite(s.recoveryCooldown) || !Finite(s.recoveryRemaining) || s.recoveryRemaining<0 || s.incidentCompany < -1 || s.incidentCompany>=s.companies.Length) return false;
                foreach(var c in s.companies)
                {
                    if(!Finite(c.suspicion) || c.suspicion<0 || c.suspicion>100)return false;
                    if(!OfficeProgression.ValidSkills(c.campaignSkills))return false;
                    if(c.workers!=null)foreach(var worker in c.workers)if(worker!=null && !OfficeProgression.ValidVoucher(worker))return false;
                    if(c.workers!=null)foreach(var worker in c.workers)if(worker!=null && (worker.specialty<0 || worker.specialty>4))return false;
                    if(c.workers==null || c.workers.Count!=c.employeeCount || !Finite(c.lockdown) || c.lockdown<0 || !Finite(c.earned) || c.earned<0) return false;
                    foreach(int upgrade in new[]{c.computerUpgrade,c.comfortUpgrade,c.lightingUpgrade,c.decorUpgrade}) if(upgrade<0 || upgrade>3)return false;
                    foreach(var w in c.workers) if(w==null || w.skill<1 || w.skill>5 || w.salary<0 || w.job < -1 || w.job>3 || w.assignment < -1 || w.assignment>3 || !Finite(w.mood) || w.mood<0 || w.mood>100 || !Finite(w.remaining) || !Finite(w.duration) || w.duration<0 || (w.job>=0 && (w.duration<=0 || w.remaining<0 || w.remaining>w.duration)))return false;
                }
            }
            return true;
        }
        static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
    }
}
