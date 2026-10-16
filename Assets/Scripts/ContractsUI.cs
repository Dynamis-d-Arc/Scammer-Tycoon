using UnityEngine;
namespace ScammerTown
{
    public partial class ScammerTownGame
    {
        void DrawContracts()
        {
            Text("CAMPAIGN CONTRACTS");
            Text("Campaign results also raise office suspicion. A freeze does not stop the contract clock. All campaigns are fictional.",true);
            Text("Complete jobs in the accepting office within 120 seconds. Partial results count. Normal job earnings continue. One contract at a time; no entry fee or failure penalty. Timer pauses with the game.",true);
            var c=state.contract;
            if(state.contractActive && c!=null)
            {
                Text(Management.Name(c.company)+" · "+Management.Jobs[c.job].name);
                Text(c.progress+" / "+c.target+" jobs · "+Mathf.CeilToInt(Mathf.Max(0,c.deadline-state.elapsed))+"s left · reward "+Money(c.reward));
                Action("Visit contract office",()=>{VisitApartment(c.company);});
                Action("Abandon (no penalty; 30s cooldown)",()=>{state.contract=null;state.contractActive=false;state.contractCooldown=state.elapsed+30;Save();},CanAct);
                return;
            }
            if(state.elapsed<state.contractCooldown){Text("Next contract available in "+Mathf.CeilToInt(state.contractCooldown-state.elapsed)+"s.");return;}
            for(int job=0;job<3;job++)
            {
                int choice=job;int target=job==0?5:job==1?4:3,reward=job==0?400:job==1?700:1000;
                Space();Text(Management.Jobs[job].name+" · "+target+" jobs / 120s · "+Money(reward));
                Action("Accept for "+CompanyName,()=>{if(OfficeProgression.Accept(state,choice)){Save();Notify("Contract accepted. Assign your team to "+Management.Jobs[choice].name+".");}},CanAct && (job!=2 || state.jobsCompleted>=5));
            }
            Text("Completed contracts: "+state.contractsCompleted,true);
        }
    }
}
