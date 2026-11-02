using UnityEngine;

public sealed class WarehouseBin : MonoBehaviour
{
    [SerializeField] private WarehouseArea warehouse;
    [SerializeField] private TextMesh label;
    public void Configure(WarehouseArea area,TextMesh text){warehouse=area;label=text;}
    public string Prompt => "E – Üres kartondoboz kidobása a papírgyűjtőbe";
    private void Update()
    {if(label!=null)label.text="PAPÍRGYŰJTŐ\nÜres kartondobozok\nKidobva: "+warehouse.DiscardedCartons;}
    public bool Use(PlayerInteractor player)
    {
        if(player.HeldCarton==null){warehouse.Shop.Notify("A papírgyűjtőbe üres kartondobozt hozhatsz. Az árut előbb pakold ki.");return false;}
        return player.ThrowAwayCarton();
    }
}
