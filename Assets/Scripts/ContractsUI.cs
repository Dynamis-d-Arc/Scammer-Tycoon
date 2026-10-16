using UnityEngine;
namespace ScammerTown
{
    public partial class ScammerTownGame
    {
        void DrawContracts()
        {
            Text("SCAM CONTRACTS");
            Text("You can ignore these and keep earning normally. Automatic office breaks do not stop the contract clock. All campaigns are fictional.",true);
            Text("Complete scam runs in the accepting office within 120 seconds. Partial takes count. Normal scam proceeds continue. One contract at a time; no entry fee or failure penalty. Timer pauses with the game.",true);
            var c=state.contract;
            if(state.contractActive && c!=null)
            {
                Text(Management.Name(c.company)+" · "+Management.Jobs[c.job].name);
                Text(c.progress+" / "+c.target+" scam runs · "+Mathf.CeilToInt(Mathf.Max(0,c.deadline-state.elapsed))+"s left · reward "+Money(c.reward));
                Action("Visit contract office",()=>{VisitApartment(c.company);});
                Action("Abandon (no penalty; 30s cooldown)",()=>{state.contract=null;state.contractActive=false;state.contractCooldown=state.elapsed+30;Save();},CanAct);
                return;
            }
            if(state.elapsed<state.contractCooldown){Text("Next contract available in "+Mathf.CeilToInt(state.contractCooldown-state.elapsed)+"s.");return;}
            for(int job=0;job<Management.Jobs.Length;job++)
            {
                int choice=job;int target=EmpireProgression.ContractTarget(job),reward=EmpireProgression.ContractReward(job);
                Space();Text(Management.Jobs[job].name+" · "+target+" scam runs / 120s · "+Money(reward));
                Action("Accept for "+CompanyName,()=>{if(OfficeProgression.Accept(state,choice)){Save();Notify("Contract accepted. Assign your team to "+Management.Jobs[choice].name+".");}},CanAct && EmpireProgression.Unlocked(state,state.active,job));
                if(!EmpireProgression.Unlocked(state,state.active,job))Text("Requires "+EmpireProgression.Requirement(job),true);
            }
            Text("Completed contracts: "+state.contractsCompleted,true);
        }
    }
}
