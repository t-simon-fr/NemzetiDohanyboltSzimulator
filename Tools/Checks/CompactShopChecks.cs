using System;
using System.Linq;
using UnityEngine;

public static class CompactShopChecks
{
    private static int passed;
    private static MvpShopController game;
    private static Transform hand;
    private static void Check(bool condition,string label){if(!condition)throw new InvalidOperationException(label);passed++;}
    public static object CashModel()
    {
        passed=0;var drawer=new ShopCashDrawer();Check(drawer.Total==6000,"Drawer starts with real 6000 Ft");
        Check(ShopCatalog.Denominations.SequenceEqual(new[]{20000,10000,5000,2000,1000,500,200,100,50,20,10,5}),"All current forint denominations");
        var pieces=new int[12];pieces[3]=2;Check(!drawer.CanSettle(5000,pieces),"Unavailable notes cannot be spent");
        pieces[3]=1;pieces[6]=4;Check(drawer.CanSettle(5000,pieces),"Available pieces can be reserved");
        drawer.Settle(5000,pieces);Check(drawer.Total==8200&&drawer.Count(5000)==1&&drawer.Count(2000)==0,"Tender and change settle exactly");
        int before=drawer.Total;Check(drawer.Exchange(5000)&&drawer.Total==before&&drawer.Count(2000)==2&&drawer.Count(1000)==2,"Exchange conserves value and changes pieces");
        Check(!drawer.Exchange(20000)&&!drawer.Exchange(5),"Empty or smallest denominations cannot be exchanged");
        drawer.Spend(450);Check(drawer.Total==before-450&&drawer.Count(5)==4,"Purchase preserves small change where possible");
        var ledger=new ShopLedger(6000);Check(!ledger.TryImprove(6005)&&ledger.TryImprove(2000)&&ledger.Improvements==2000&&ledger.Cash==4000,"Investment paid once through ledger");
        drawer.OfferTender(5000);int saved=drawer.Total;
        Check(drawer.PendingTender==5000&&drawer.Count(5000)>0&&drawer.Total==saved,"Customer tender remains separate until settlement");
        Check(drawer.Exchange(5000)&&drawer.PendingTender==5000,"Offered money can be exchanged without losing ownership");
        drawer.CancelTender();Check(drawer.PendingTender==0&&drawer.Total==saved,"Cancellation returns customer tender");
        return new{passed,failed=0};
    }
    public static object BaseDay()=>RunDay(false);
    public static object ExpandedDay()=>RunDay(true);
    private static object RunDay(bool expanded)
    {
        game=MvpShopController.Instance;if(game==null||!Application.isPlaying)throw new InvalidOperationException("Play compact scene first.");
        passed=0;game.ManualTick=true;hand=new GameObject("Temporary Compact Check Hand").transform;hand.position=Vector3.one*1000;
        try
        {
            game.ResetDemo();game.ClosePanel();Check(game.Ledger.Cash==6000&&game.Drawer.Total==6000,"Opening funds agree");
            Check(Enumerable.Range(0,ShopCatalog.CigaretteCount).All(i=>game.StockCount(i)==8)&&Enumerable.Range(ShopCatalog.CigaretteCount,ShopCatalog.Count-ShopCatalog.CigaretteCount).All(i=>game.StockCount(i)==0),"Full cigarette starter rack and locked convenience stock");
            Check(!game.OrderSupply(ShopCatalog.CigaretteCount)&&!game.OrderSupply(-1),"Locked and invalid product rejected");
            var counter=GameObject.Find("Service counter").transform;Vector3 counterPosition=counter.position;
            game.OpenSupplier();Check(game.Modal==ShopModal.None&&game.Devices.FocusKind==ShopDeviceKind.Monitor&&ShopInputGate.Blocked,"Monitor focus replaces supplier modal");
            game.Devices.OpenApp(ShopComputerApp.Orders);CheckVisibleControl(WorldControlKind.Order,0);
            game.ClosePanel();Check(!game.Devices.Focused&&!ShopInputGate.Blocked,"Device releases input gate");
            if(expanded)
            {
                Check(game.TryExpand()&&game.Layout.Expanded&&game.Ledger.Cash==4000&&game.Drawer.Total==4000,"Expansion unlocks space and costs cash");
                Check(counter.position==counterPosition&&!game.TryExpand(),"Counter anchored and duplicate expansion rejected");
                for(int type=ShopCatalog.CigaretteCount;type<ShopCatalog.Count;type++)
                {
                    Check(game.OrderSupply(type),"Order convenience stock after expansion");game.Tick(8.1f);
                    for(int j=0;j<3;j++)
                    {
                        Check(game.TryCreateSupply(type,out var item),"Unpack convenience stock");PlaceOnShelf(item,type);
                    }
                    Check(game.StockCount(type)==3&&game.WarehouseCount(type)==0,"Public shelf stocked by player");
                }
            }
            game.OpenShop();Check(!game.TryExpand(),"Expansion only possible before opening");
            bool wrongChecked=false;int selfServed=0,guard=0;
            while(game.Ledger.Sales<8&&guard++<15000)
            {
                if(game.ActiveCustomer==null||game.Stage==ServiceStage.Thanks||game.Stage==ServiceStage.None){game.Tick(.1f);continue;}
                var customer=game.ActiveCustomer;int type=customer.Product;
                if(game.Stage==ServiceStage.Order)
                {
                    if(!wrongChecked)
                    {
                        var wrong=UnityEngine.Object.FindObjectsByType<ShopProduct>().First(p=>p.ProductId==ShopCatalog.Ids[(type+1)%ShopCatalog.CigaretteCount]&&!p.IsHeld);
                        Offer(wrong);Check(game.Stage==ServiceStage.Order&&game.Ledger.Sales==0,"Wrong product remains recoverable");
                        Check(wrong.PickUp(hand),"Recover wrong product");wrong.ReturnToPickupPosition();
                        // Original position was the tray: explicitly return it to a matching empty shelf.
                        Check(wrong.PickUp(hand),"Carry recovered product");Physics.SyncTransforms();Check(wrong.TryPlace(EmptyShelf((type+1)%ShopCatalog.CigaretteCount)),"Return wrong product to rack");wrongChecked=true;
                    }
                    var item=UnityEngine.Object.FindObjectsByType<ShopProduct>().First(p=>p.ProductId==ShopCatalog.Ids[type]&&!p.IsHeld);Offer(item);
                    Check(game.Stage==ServiceStage.Payment&&game.Modal==ShopModal.None,"Prepared product waits for world cash register");
                }
                else if(customer.BasketItem!=null)
                {
                    selfServed++;Check(type>=ShopCatalog.CigaretteCount&&customer.BasketItem.IsHeld,"Customer independently collected convenience item");
                }
                int before=game.Ledger.Cash,count=game.StockCount(type);
                Check(!game.SubmitChange(customer.Tender-ShopCatalog.Prices[type]+5)&&game.Ledger.Cash==before,"Incorrect change cannot charge customer");
                game.OpenCash();Check(game.Devices.FocusKind==ShopDeviceKind.Till&&game.Modal==ShopModal.None,"Physical register focus");
                CheckVisibleControl(WorldControlKind.Pay,0);
                SelectChange(customer.Tender-ShopCatalog.Prices[type]);
                Check(game.SubmitChange(game.ChangeSelected),"Physical pieces settle payment");
                Check(game.StockCount(type)==count-1&&game.Ledger.Cash==before+ShopCatalog.Prices[type]&&game.Drawer.Total==game.Ledger.Cash,"Inventory, ledger and physical cash agree");
                Check(!game.SubmitChange(0)&&!game.Devices.Focused,"No duplicate payment and camera released");
            }
            Check(guard<15000&&game.Ledger.Sales==8,"Complete eight-customer day");
            for(int i=0;i<1500&&game.Phase!=ShopPhase.Summary;i++)game.Tick(.1f);
            Check(game.Phase==ShopPhase.Summary&&game.Missed==0,"All customers leave and day closes");
            Check(game.Drawer.Total==game.Ledger.Cash&&game.Ledger.Cash==6000+game.Ledger.Revenue-game.Ledger.Purchases-game.Ledger.Improvements-300,"End-of-day accounting includes investment separately");
            Check(expanded?selfServed==4:game.Ledger.Revenue==19000&&game.Ledger.Profit==5400,"Self-service visitors / cigarette day economics");
            int cash=game.Ledger.Cash,units=Enumerable.Range(0,ShopCatalog.Count).Sum(game.StockCount);game.NextDay();
            Check(game.Day==2&&game.Ledger.Cash==cash&&game.Drawer.Total==cash&&game.Layout.Expanded==expanded&&Enumerable.Range(0,ShopCatalog.Count).Sum(game.StockCount)==units,"Next day retains drawer, stock and expansion");
            return new{passed,failed=0,expanded,selfServed,cash};
        }
        finally
        {
            foreach(var p in UnityEngine.Object.FindObjectsByType<ShopProduct>().Where(p=>p.IsHeld))p.ReturnToPickupPosition();
            UnityEngine.Object.DestroyImmediate(hand.gameObject);game.ResetDemo();game.ManualTick=false;
        }
    }
    private static ProductSlot EmptyShelf(int type)=>UnityEngine.Object.FindObjectsByType<ProductSlot>().First(s=>s.Occupant==null&&s.isActiveAndEnabled&&s.name.StartsWith(ShopCatalog.IsCigarette(type)?"Cigarette slot":"Public slot")&&s.CanAccept(Probe(type)));
    private static ShopProduct Probe(int type)=>UnityEngine.Object.FindObjectsByType<ShopProduct>().First(p=>p.ProductId==ShopCatalog.Ids[type]);
    private static void PlaceOnShelf(ShopProduct item,int type)
    {
        Check(item.PickUp(hand),"Carry supplied product");Physics.SyncTransforms();Check(item.TryPlace(EmptyShelf(type)),"Place on type-restricted fixed shelf");
    }
    private static void Offer(ShopProduct item)
    {
        Check(item.PickUp(hand),"Pick requested pack");Physics.SyncTransforms();Check(item.TryPlace(game.DeliverySlot),"Place on blue tray");
    }
    private static void SelectChange(int due)
    {
        for(int attempt=0;attempt<(game.Drawer.Total+game.Drawer.PendingTender)/50+ShopCatalog.Denominations.Length;attempt++)
        {
            game.ClearChange();int rest=due;
            foreach(int v in ShopCatalog.Denominations)
            {
                int pieces=Math.Min(rest/v,game.Drawer.Count(v));for(int i=0;i<pieces;i++)game.AddChange(v);rest-=pieces*v;
            }
            if(rest==0){Check(game.ChangeSelected==due,"Change assembled from actual drawer pieces");return;}
            game.ExchangeCash();
        }
        throw new InvalidOperationException("Not enough change after exchanges: "+due);
    }
    private static void CheckVisibleControl(WorldControlKind kind,int value)
    {
        var control=UnityEngine.Object.FindObjectsByType<ShopWorldControl>().First(c=>c.Kind==kind&&(kind!=WorldControlKind.Order||c.Value==value));
        game.Devices.RegisterDrawer.Advance(1f);
        var camera=Camera.main;Vector3 point=camera.WorldToScreenPoint(control.transform.position);
        Check(point.z>0&&point.x>0&&point.x<camera.pixelWidth&&point.y>0&&point.y<camera.pixelHeight,"Device control fits focused camera");
        Physics.SyncTransforms();var ray=camera.ScreenPointToRay(point);
        Check(Physics.Raycast(ray,out var hit,4f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<ShopWorldControl>()!=null,"Device control can be reached by mouse ray");
    }
    public static object EdgeCases()
    {
        game=MvpShopController.Instance;passed=0;game.ManualTick=true;
        hand=new GameObject("Temporary Compact Edge Hand").transform;hand.position=Vector3.one*1000;
        try
        {
            game.ResetDemo();game.ClosePanel();game.TryExpand();
            Check(game.OrderSupply(ShopCatalog.CigaretteCount),"Prepare public stock for cancellation test");game.Tick(8.1f);
            game.TryCreateSupply(ShopCatalog.CigaretteCount,out var item);PlaceOnShelf(item,ShopCatalog.CigaretteCount);
            ProductSlot source=item.CurrentSlot;
            game.OpenShop();
            for(int i=0;i<3000 && !(game.ActiveCustomer!=null&&game.ActiveCustomer.Product==ShopCatalog.CigaretteCount);i++)
            {
                if(game.ActiveCustomer!=null)game.SkipCurrent();game.Tick(.1f);
            }
            // Visit index 3 asks for water; earlier guests are dismissed deliberately.
            Check(game.ActiveCustomer!=null&&game.ActiveCustomer.BasketItem==item,"Customer arrives carrying shelf item");
            int cash=game.Ledger.Cash;Check(game.Drawer.PendingTender>0,"Payment tender staged");
            game.OpenCash();game.AddChange(100);game.SkipCurrent();
            Check(item.gameObject.activeSelf&&!item.IsHeld&&item.CurrentSlot==source&&source.Occupant==item,"Cancelled purchase returns item to reserved shelf");
            Check(game.Drawer.PendingTender==0&&game.Drawer.Total==cash&&game.Ledger.Cash==cash&&game.ChangeSelected==0,"Cancellation returns tender without charging either party");
            Check(!game.Devices.Focused&&!ShopInputGate.Blocked,"Cancellation releases device and player");
            game.ResetDemo();game.OpenShop();
            for(int i=0;i<100;i++)game.Tick(.1f);
            var pack=UnityEngine.Object.FindObjectsByType<ShopProduct>().First(p=>p.ProductId=="cig_red");Offer(pack);
            Check(pack.PickUp(hand),"Player recovers unpaid prepared product");
            game.Tick(.1f);Check(game.Stage==ServiceStage.Order&&game.Drawer.PendingTender==0,"Removing prepared product cancels pending cash");
            pack.ReturnToPickupPosition();
            game.ResetDemo();game.OrderSupply(0);game.OpenShop();game.RequestClose();game.Tick(.1f);
            Check(game.Phase==ShopPhase.Summary&&game.WarehouseCount(0)==3&&game.Ledger.Cash==1200&&game.Drawer.Total==1200,"Early close retains paid pending shipments and charges fee once");
            game.Tick(100);Check(game.Ledger.Cash==1200,"Closing fee cannot repeat");
            game.ResetDemo();Check(!game.Layout.Expanded&&game.AvailableProducts==ShopCatalog.CigaretteCount&&game.Drawer.Total==6000,"Reset restores compact starter state");
            game.Ledger.TryImprove(6000);game.Drawer.Spend(6000);game.OpenShop();game.RequestClose();game.Tick(.1f);
            Check(game.Phase==ShopPhase.Summary&&game.Ledger.Cash==-300&&game.Drawer.Total==0,"Insolvency has debt rather than negative physical money");
            game.NextDay();Check(game.Day==1&&game.Phase==ShopPhase.Summary,"Insolvent next day is blocked safely");
            return new{passed,failed=0};
        }
        finally{UnityEngine.Object.DestroyImmediate(hand.gameObject);game.ResetDemo();game.ManualTick=false;}
    }
    public static object DeviceControls()
    {
        game=MvpShopController.Instance;passed=0;game.ResetDemo();game.ClosePanel();game.OpenSupplier();
        foreach(var app in new[]{ShopComputerApp.Desktop,ShopComputerApp.Orders,ShopComputerApp.Furnishing})
        {
            game.Devices.OpenApp(app);
            foreach(var c in UnityEngine.Object.FindObjectsByType<ShopWorldControl>().Where(c=>c.transform.IsChildOf(GameObject.Find("Order screen").transform)))CheckRay(c);
        }
        game.ClosePanel();game.OpenShop();game.ManualTick=true;hand=new GameObject("Temporary Device Hand").transform;hand.position=Vector3.one*1000;
        for(int i=0;i<100;i++)game.Tick(.1f);Offer(UnityEngine.Object.FindObjectsByType<ShopProduct>().First(p=>p.ProductId=="cig_red"));game.OpenCash();game.Devices.RegisterDrawer.Advance(1f);
        foreach(var c in UnityEngine.Object.FindObjectsByType<ShopWorldControl>().Where(c=>c.Kind==WorldControlKind.Denomination||c.Kind==WorldControlKind.Pay||c.Kind==WorldControlKind.Exchange||c.Kind==WorldControlKind.Clear||c.Kind==WorldControlKind.Undo))CheckRay(c);
        UnityEngine.Object.DestroyImmediate(hand.gameObject);game.ResetDemo();game.ManualTick=false;return new{passed,failed=0};
    }
    private static void CheckRay(ShopWorldControl c)
    {
        Vector3 screen=Camera.main.WorldToScreenPoint(c.transform.position);
        Check(screen.z>0&&screen.x>0&&screen.x<Camera.main.pixelWidth&&screen.y>0&&screen.y<Camera.main.pixelHeight,"Every device control stays on screen: "+c.name);
        Physics.SyncTransforms();Check(Physics.Raycast(Camera.main.ScreenPointToRay(screen),out var hit,4f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<ShopWorldControl>() is ShopWorldControl actual&&actual.Kind==c.Kind&&actual.Value==c.Value,"Mouse ray reaches correct device control: "+c.name);
    }
    public static object PlayerRoute()
    {
        game=MvpShopController.Instance;passed=0;game.ManualTick=true;game.ResetDemo();game.ClosePanel();
        var player=UnityEngine.Object.FindAnyObjectByType<FirstPersonController>();var body=player.GetComponent<CharacterController>();
        Vector3 original=player.transform.position;Quaternion rotation=player.transform.rotation;
        player.enabled=false;
        try
        {
            Physics.SyncTransforms();
            Walk(body,new Vector3(-2.12f,0,-.7f));Walk(body,new Vector3(-2.12f,0,-3.25f));
            Check(player.transform.position.y>-.05f,"Player can walk around counter without falling");
            Check(game.TryExpand(),"Unlock extension for physical route check");
            Walk(body,new Vector3(.13f,0,-3.25f));Walk(body,new Vector3(3.2f,0,-3.25f));Walk(body,new Vector3(5.1f,0,-3.25f));Walk(body,new Vector3(5.1f,0,-1.25f));
            Check(player.transform.position.y>-.05f,"Player can reach self-service shelf on real floor");
            Walk(body,new Vector3(5.1f,0,-3.25f));Walk(body,new Vector3(.13f,0,-3.25f));
            for(int i=0;i<80;i++)body.Move(new Vector3(0,-.025f,-.04f));
            Check(player.transform.position.z> -4.3f && player.transform.position.y>-.05f,"Door boundary prevents leaving shop floor and getting stuck below map");
            return new{passed,failed=0};
        }
        finally
        {
            body.enabled=false;player.transform.SetPositionAndRotation(original,rotation);body.enabled=true;
            player.enabled=true;game.ResetDemo();game.ManualTick=false;
        }
    }
    private static void Walk(CharacterController body,Vector3 target)
    {
        for(int i=0;i<300;i++)
        {
            Vector3 d=target-body.transform.position;d.y=0;if(d.magnitude<.055f)break;
            Vector3 step=Vector3.ClampMagnitude(d,.04f);body.Move(step+Vector3.down*.025f);
        }
        Vector3 rest=target-body.transform.position;rest.y=0;Check(rest.magnitude<.09f,"Player route reached: "+target+" actual: "+body.transform.position);
    }
    public static object PreviewMonitor(){game=MvpShopController.Instance;game.ResetDemo();game.ClosePanel();game.OpenSupplier();return game.Devices.FocusKind.ToString();}
    public static object PreviewCounter(){game=MvpShopController.Instance;game.ResetDemo();game.ClosePanel();UnityEngine.Object.FindAnyObjectByType<FirstPersonController>().transform.rotation=Quaternion.Euler(0,180,0);return "counter";}
    public static object PreviewRack()
    {
        game=MvpShopController.Instance;game.ResetDemo();game.ClosePanel();UnityEngine.Object.FindAnyObjectByType<FirstPersonController>().transform.rotation=Quaternion.identity;Camera.main.transform.localRotation=Quaternion.identity;return "rack";
    }
    public static object PreviewCash()
    {
        game=MvpShopController.Instance;game.ResetDemo();game.OpenShop();game.ManualTick=true;hand=new GameObject("Temporary preview hand").transform;hand.position=Vector3.one*1000;
        for(int i=0;i<100;i++)game.Tick(.1f);Offer(UnityEngine.Object.FindObjectsByType<ShopProduct>().First(p=>p.ProductId=="cig_red"));
        UnityEngine.Object.DestroyImmediate(hand.gameObject);game.OpenCash();game.AddChange(2000);game.AddChange(500);game.AddChange(200);game.AddChange(100);return "cash";
    }
}
