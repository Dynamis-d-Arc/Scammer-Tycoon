using UnityEngine;

namespace ScammerTown
{
    public partial class ScammerTownGame
    {
        bool cityView;
        GUIStyle cityMarkerStyle;
        Texture2D cityArtwork, cityForeground;
        Texture2D[] cityVehicles;
        float animationTime;
        int citySelectedApartment;
        void OpenCity()
        {
            furnitureStorageOpen=false;managementPanel=false;shopModal=false;workerModal=false;CancelPlacement();citySelectedApartment=state.active;cityView=true;
        }
        void VisitSelectedApartment() { VisitApartment(citySelectedApartment); }
        void SelectCityApartment(int index) { if(index>=0 && index<state.companies.Length) citySelectedApartment=index; }
        void VisitApartment(int index)
        {
            if(!Rules.VisitApartment(state,index)) return;
            SelectCityApartment(index);
            furnitureStorageOpen=false;CancelPlacement();cityView=false;tab="Operations";scroll=Vector2.zero;
            managementPanel=false;workerModal=false;selectedWorker=0;selection="Apartment overview";
            Log("Now operating from " + CompanyName + ".");Save();
        }
        void PurchaseApartment()
        {
            if(!CanAct) return;
            if(!Rules.BuyApartment(state)) {Notify(state.branchOwned ? "You already own this apartment." : "You need $3,000 to buy Neon Apartments.");return;}
            Management.Ensure(state);PlayFeedback(false);
            Log("Bought Neon Apartments for $3,000. Includes one employee and one desktop.");Save();
        }
        void DrawCity()
        {
            if(cityArtwork==null)
            {
                cityArtwork=SoftAsset("city");
                cityForeground=Resources.Load<Texture2D>("Soft3D/city_foreground");


            }
            Panel(new Rect(0,70,1440,758),new Color(.035f,.05f,.09f));
            Label(24,88,1000,38,"SCAMMER TOWN / CITY VIEW",title);
            Label(24,130,1060,30,"A neighborhood of opportunities. Select an apartment marker, then visit or buy it in the property panel.",small);
            Rect map=new Rect(24,180,1056,620);
            GUI.DrawTexture(map,cityArtwork,ScaleMode.StretchToFill,true);
            DrawCityTraffic(map);

            CityApartmentMarker(0,map,"01  OLD TOWN");
            CityApartmentMarker(1,map,"02  NEON DISTRICT");
            CityApartmentMarker(2,map,"03  HARBOR");
            ApartmentCard(citySelectedApartment,1108,208,Management.Name(citySelectedApartment).ToUpper(),Management.Benefit(citySelectedApartment));
            for(int i=0;i<3;i++)
                if(Button(1110,426+i*38,296,(citySelectedApartment==i ? "● " : "")+Management.Name(i),true,33))SelectCityApartment(i);
            if(Button(1110,538,296,"Manage current apartment",true,30)){Navigate("Properties");}
            if(Button(1110,578,296,"Equipment shop",true,31)){OpenShop(0);}
            if(Button(1110,614,296,"Recruitment agency",true,31)){OpenShop(3);}
            if(Button(1110,650,296,"Go to selected apartment (C)",Management.Owned(state,citySelectedApartment),31))VisitSelectedApartment();

        }
        void CityApartmentMarker(int index,Rect map,string text)
        {
            Vector2 point=index==0?new Vector2(802,269):index==1?new Vector2(1035,351):new Vector2(416,438);
            point=new Vector2(map.x+point.x/1635f*map.width,map.y+point.y/962f*map.height);
            Color accent=index==0 ? lime : new Color(.46f,.82f,.82f);
            Panel(new Rect(point.x-1,point.y-26,2,27),accent);
            Panel(new Rect(point.x-4,point.y-4,8,8),accent);
            var badge=new Rect(point.x-95,point.y-73,190,47);
            Panel(new Rect(badge.x-2,badge.y-2,badge.width+4,badge.height+4),citySelectedApartment==index ? accent : new Color(.2f,.29f,.3f));
            var c=state.companies[index];
            string detail=Management.Owned(state,index) ? "\n"+Money(c.earned)+" · "+c.employeeCount+"/"+c.Capacity+" staff · "+c.workers.FindAll(w=>w.job>=0).Count+" working" : "\n"+(index==1 ? "$3,000" : "10 jobs + $7,500");
            if(cityMarkerStyle==null)cityMarkerStyle=new GUIStyle(button){fontSize=11,padding=new RectOffset(8,5,3,3)};
            if(GUI.Button(badge,text+detail,cityMarkerStyle))SelectCityApartment(index);
        }
        void ApartmentCard(int index,float x,float y,string name,string address)
        {
            bool owned=Management.Owned(state,index);var apartment=state.companies[index];
            Panel(new Rect(x,y,296,208),new Color(.065f,.095f,.16f));
            Label(x+12,y+8,272,30,name,heading);
            Label(x+12,y+40,272,40,address,small);
            int wages=0;foreach(var w in apartment.workers)wages+=w.salary;
            Label(x+12,y+82,272,65,owned ? (state.active==index ? "CURRENT" : "OWNED")+" · "+apartment.Size+"×"+apartment.Size+"\n"+apartment.employeeCount+" staff / "+apartment.Stations+" stations · "+apartment.workers.FindAll(w=>w.job>=0).Count+" working\nIncome "+Money(apartment.earned)+" total · Wages $"+wages+"/day" : index==1 ? "Includes Riley and a desktop.\nBuy to unlock faster operations." : "Includes Morgan and a 10×10 loft.\nRequires Neon + 10 completed jobs.");
            if(owned)
            {
                if(Button(x+12,y+160,272,"Go to "+Management.Name(index),true,38))VisitApartment(index);
            }
            else if(Button(x+12,y+160,272,index==1 ? "Buy · $3,000" : "Buy · $7,500",CanAct && (index==1 ? state.cash>=3000 : state.branchOwned && state.jobsCompleted>=10 && state.cash>=7500),38))
            {
                if(index==1)PurchaseApartment();
                else if(Management.BuyHarbor(state)){Log("Harbor Loft purchased. Morgan is ready for work.");PlayFeedback(false);Save();}
            }
        }

        void DrawCityTraffic(Rect map)
        {
            // Wrap beyond the map edge; scenery is drawn above vehicles for occlusion.
            for(int i=0;i<6;i++)
            {
                float travel=Mathf.Repeat(animationTime*.55f+i*3.1f,17)-1;
                float x=i%2==0 ? 8.22f : 13.64f;
                DrawCityVehicle(map,0,x,i%2==0 ? travel : 15-travel);
            }
            for(int i=0;i<3;i++) DrawCityVehicle(map,1,Mathf.Repeat(animationTime*.48f+i*5,13)+3,9.2f);
            DrawCityVehicle(map,2,.55f,Mathf.PingPong(animationTime*.17f,2.1f)+5.25f);
            DrawCityVehicle(map,2,1.65f,Mathf.PingPong(animationTime*.14f+1,2.2f)+10.2f);
        }
        void DrawCityVehicle(Rect map,int kind,float x,float y)
        {
            if(x<0 || x>14.3f || y<0 || y>14.3f) return;
            Vector2 p=IsometricCityArtwork.Project(x,y);
            // Use the retained geometric city mask solely for depth occlusion.
            if(cityForeground!=null)
            {
                int mx=Mathf.Clamp(Mathf.RoundToInt(p.x),0,cityForeground.width-1);
                int my=Mathf.Clamp(cityForeground.height-1-Mathf.RoundToInt(p.y),0,cityForeground.height-1);
                if(cityForeground.GetPixel(mx,my).a>.25f)return;
            }
            p=new Vector2(813+(p.x-800)*1.108f,35+(p.y-105)*1.235f);
            float size=kind==2?53:35;
            AtlasSprite(SoftAsset("props"),kind==2?2:kind,2,4,3,new Rect(map.x+p.x/1635f*map.width-size*.5f,map.y+p.y/962f*map.height-size*.75f,size,size*.89f));
        }

    }
}
