using System.Linq;
using UnityEngine;

public sealed partial class ShopReputation
{
    private bool showingLast;
    public ShopReputationReport VisibleReport => showingLast && State.LastReport!=null ? State.LastReport : State.Preview();
    public string SelectedTabId => showingLast && State.LastReport!=null ? "Reputation:Last" : "Reputation:Today";
    private static readonly string[] FactorNames = { "Választék / készlet", "Tisztaság", "Árak", "Kiszolgálási idő" };
    private static string Number(double value) => value.ToString("0.0");
    public static string Change(double value) => (value>0?"+":"")+Number(value);
    public void AddControls(System.Collections.Generic.List<ShopKeyboardTarget> controls, Rect body)
    {
        controls.Add(new ShopKeyboardTarget("Reputation:Today","Ma",new Rect(body.x+760,body.y+12,130,40),()=>showingLast=false));
        controls.Add(new ShopKeyboardTarget("Reputation:Last","Lezárt nap",new Rect(body.x+902,body.y+12,230,40),()=>showingLast=true,State.LastReport!=null));
    }
    public void Draw(Rect body)
    {
        var report=VisibleReport;
        float x=body.x+20,y=body.y;
        ShopComputerTheme.Label(new Rect(x,y+6,700,55),"Hírnév  "+Number(State.Value)+" / 100",32,true);
        string subtitle=showingLast?report.Day+". nap  •  Változás: "+Change(report.Change):"Mai megfigyelések  •  Frissítés napzáráskor";
        ShopComputerTheme.Label(new Rect(x,y+64,1080,34),subtitle,20);
        for(int i=0;i<4;i++)
        {
            float row=y+112+i*68;
            ShopComputerTheme.Label(new Rect(x,row,390,30),FactorNames[i],23,true);
            ShopComputerTheme.Label(new Rect(x+400,row,250,30),report.Evidence[i]?report.Scores[i].ToString("0")+" / 100":"Még nincs adat",22,true);
            ShopUi.Fill(new Rect(x,row+51,650,1),ShopComputerTheme.Rule);
        }
        ShopComputerTheme.Label(new Rect(x+706,y+112,390,32),"Megfigyelések",23,true);
        int index=0;
        foreach(string reason in report.Reasons.Take(3))
            ShopComputerTheme.Label(new Rect(x+706,y+154+index++*70,390,66),reason,19);
        ShopComputerTheme.Label(new Rect(x,y+396,1090,32),"Mért nyitvatartás: "+report.Seconds.ToString("0")+" mp  •  Lezárt látogatás: "+report.Visits+
            "  •  Átlagos várakozás: "+(report.ServiceVisits>0?report.MeanServiceSeconds.ToString("0")+" mp":"még nincs adat"),18);
        ShopComputerTheme.Label(new Rect(x,y+433,1090,30),"Forgalom: "+(1/State.ArrivalScale).ToString("0.00")+"×  •  A nagyobb kosarak és a törzsvendégek esélye is fokozatosan nő.",17);
    }
    public void DrawSummary()
    {
        var report=State.LastReport;if(report==null)return;
        GUILayout.Label("Hírnév: "+Number(report.Value)+" / 100  ("+Change(report.Change)+")",ShopUi.Text(16,true));
        GUILayout.Label("Választék "+report.Scores[0].ToString("0")+" • Tisztaság "+report.Scores[1].ToString("0")+
            " • Árak "+report.Scores[2].ToString("0")+" • Kiszolgálás "+report.Scores[3].ToString("0"),ShopUi.Text(15));
        foreach(string reason in report.Reasons.Take(2)) GUILayout.Label(reason,ShopUi.Text(14));
    }
}
