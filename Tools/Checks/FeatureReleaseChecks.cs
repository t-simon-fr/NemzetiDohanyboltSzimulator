using System;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

public static class FeatureReleaseChecks
{
    private static int passed;
    private static void Check(bool valid,string label){if(!valid)throw new InvalidOperationException(label);passed++;}
    private static void Fund(MvpShopController game,int amount=100000)
    {
        typeof(ShopLedger).GetProperty("OpeningCash").SetValue(game.Ledger,game.Ledger.OpeningCash+amount);
        typeof(ShopLedger).GetProperty("Cash").SetValue(game.Ledger,game.Ledger.Cash+amount);
        game.Drawer.Settle(amount,new int[ShopCatalog.Denominations.Length]);
    }
    private static void SelectCoins(MvpShopController game)
    {
        var offer=game.Activities.CoinOffer;int remaining=offer.Price;
        foreach(int index in Enumerable.Range(0,offer.Coins.Count).OrderByDescending(i=>offer.Coins[i]))
            if(offer.Coins[index]<=remaining){game.ToggleCustomerCoin(index);remaining-=offer.Coins[index];}
        Check(remaining==0&&offer.Total==offer.Price,"Customer palm has a selectable exact amount");
    }
    private static void SelectChange(MvpShopController game,int due)
    {
        for(int attempt=0;attempt<100;attempt++)
        {
            game.ClearChange();int rest=due;
            foreach(int value in ShopCatalog.Denominations)
            {
                int count=Math.Min(rest/value,game.Drawer.Count(value));for(int i=0;i<count;i++)game.AddChange(value);rest-=count*value;
            }
            if(rest==0){Check(game.ChangeSelected==due,"Select finite physical cash for change");return;}game.ExchangeCash();
        }
        throw new InvalidOperationException("No change could be selected.");
    }
    public static object Rules()
    {
        passed=0;
        Check(ShopLayout.ExpansionPrice==24000&&ShopLayout.ExtraShelfPrice==9000&&ShopActivities.CoffeeMachinePrice==16000,"Development prices use the new economy");
        var ledger=new ShopLedger(6000);ledger.RecordStockLoss(150);Check(ledger.Cash==6000&&ledger.StockLoss==150&&ledger.Profit==-150,"Theft is inventory loss without inventing a cash expense");
        foreach(int price in new[]{350,450,650})
        {
            var offer=new ShopCoinOffer(price,price);int rest=price;
            foreach(int index in Enumerable.Range(0,offer.Coins.Count).OrderByDescending(i=>offer.Coins[i]))if(offer.Coins[index]<=rest){offer.Toggle(index);rest-=offer.Coins[index];}
            Check(rest==0&&offer.Total==price,"Solvable finite coin offer "+price);
            var drawer=new ShopCashDrawer();drawer.OfferTenderPieces(offer.Pieces());Check(drawer.PendingTender==price&&drawer.Total==6000,"Actual selected denominations remain pending");
            drawer.CancelTender();Check(drawer.Total==6000&&drawer.PendingTender==0,"Cancelled coins do not enter cash");offer.Clear();Check(offer.Total==0,"Coin choices are reversible");
        }
        var snake=new ShopSnake(123);var head=snake.Body[0];snake.Turn(Vector2Int.left);snake.Step();Check(snake.Body[0]==head+Vector2Int.right,"Snake cannot reverse into itself");
        for(int i=0;i<30&&!snake.Ended;i++)snake.Step();Check(snake.Ended,"Snake ends at board boundary");
        var memory=new ShopMemoryGame(37);Check(memory.Cards.GroupBy(x=>x).Count()==8&&memory.Cards.GroupBy(x=>x).All(g=>g.Count()==2),"Memory has eight real pairs");
        int first=0,second=Array.FindIndex(memory.Cards,x=>x!=memory.Cards[first]);memory.Choose(first);memory.Choose(second);
        Check(!memory.Choose(3)&&memory.Second>=0,"Wrong pair cannot be changed during reveal timer");memory.Tick(1);Check(memory.First==-1&&memory.Second==-1,"Wrong pair turns back over");
        foreach(var pair in Enumerable.Range(0,16).GroupBy(i=>memory.Cards[i])){foreach(int index in pair)memory.Choose(index);}
        Check(memory.Won&&memory.Pairs==8&&memory.Moves==9,"All actual memory pairs can be completed");return new {passed,failed=0};
    }
    public static object WorldFeatures()
    {
        passed=0;var game=MvpShopController.Instance;var activities=game.Activities;var player=Object.FindAnyObjectByType<PlayerInteractor>();
        var camera=Camera.main;var movement=player.GetComponent<FirstPersonController>();bool enabled=movement.enabled;
        var position=player.transform.position;var rotation=player.transform.rotation;var localCamera=camera.transform.localPosition;var cameraRotation=camera.transform.localRotation;
        var body=player.GetComponent<CharacterController>();movement.enabled=false;game.ManualTick=true;
        try
        {
            game.ResetDemo();game.ClosePanel();
            var product=Object.FindObjectsByType<ShopProduct>().First();var source=product.CurrentSlot;int type=ShopCatalog.Index(product.ProductId),before=game.StockCount(type);
            Check(player.TryHold(product)&&player.DropHeld(),"G drop releases held product");
            Check(!product.IsHeld&&product.CurrentSlot==null&&source.Occupant==null&&game.StockCount(type)==before,"Dropped product frees shelf and preserves inventory");
            Check(product.GetComponent<Rigidbody>()!=null&&!product.GetComponent<Rigidbody>().isKinematic,"Dropped product has real physics");
            Check(player.TryHold(product),"Dropped product can be picked up again");player.CancelHeld();
            Check(game.OrderSupply(0,1),"Purchase a finite carton");game.Tick(8.1f);var carton=game.Warehouse.Carton(0);
            Check(player.TryHoldCarton(carton)&&player.DropHeld()&&!carton.IsHeld&&!carton.IsDocked&&carton.Quantity==1,"Filled carton drops with contents intact");
            Check(game.WarehouseCount(0)==1&&game.Warehouse.Carton(0)==null,"Dropped carton remains in total inventory without occupying delivery dock");
            Check(player.TryHoldCarton(carton),"Dropped carton can be picked up again");player.ReturnCarton();
            Check(player.TryHoldTool(activities.Bat)&&player.DropHeld()&&!activities.Bat.IsHeld,"Bat can be carried and dropped");
            Check(player.TryHoldTool(activities.Mop)&&player.DropHeld()&&!activities.Mop.IsHeld,"Mop can be carried and dropped");
            var spill=activities.SpawnSpill();Check(spill!=null&&activities.DirtCount==1,"Walkable floor receives a visible spill");
            Check(player.TryHoldTool(activities.Mop),"Pick mop for actual floor targeting");
            var cleaningEye=spill.transform.position+Vector3.up*1.45f+Vector3.forward*.7f;Physics.SyncTransforms();player.RefreshTarget(new Ray(cleaningEye,(spill.transform.position-cleaningEye).normalized));
            Check(player.TargetFeature==spill,"Actual player ray targets the floor stain while holding mop");
            for(int i=0;i<16;i++)spill.Scrub(.1f);Check(activities.DirtCount==0,"Sustained mopping clears the spill");player.CancelHeld();
            Fund(game);Check(game.TryExpand(),"Expanded room can be purchased at new price");
            Check(game.Activities.BuyCoffeeMachine()&&!game.Activities.BuyCoffeeMachine(),"Coffee machine is a single paid upgrade");
            Check(activities.OrderCoffeeSupplies()&&activities.PendingCoffee==10&&activities.CoffeeSupplies==0,"Coffee supplies are ordered, not free");
            game.Tick(7.9f);Check(activities.CoffeeSupplies==0,"Coffee supply does not arrive early");game.Tick(.2f);
            Check(activities.CoffeeSupplies==10&&activities.PendingCoffee==0,"Paid coffee supplies arrive");
            Check(activities.BrewCoffee()&&!activities.BrewCoffee()&&activities.CoffeeSupplies==9,"One brew consumes one serving and prevents duplicate brewing");game.Tick(3.6f);
            Check(activities.CoffeeOutput.Occupant!=null&&activities.CoffeeOutput.Occupant.ProductId=="coffee","Brew produces a real carryable coffee cup");
            Check(!activities.BrewCoffee(),"Occupied output prevents coffee duplication");
            activities.UseCoffeeMachine();Check(player.HeldProduct!=null&&player.HeldProduct.ProductId=="coffee","E on coffee machine picks the finished cup");player.DropHeld();
            Check(activities.CameraCount==2&&activities.RenderCamera(0)&&activities.RenderCamera(1),"Both installed CCTV cameras render live images");
            Check(activities.CameraImage(0).IsCreated()&&activities.CameraImage(0).width==480,"Camera render targets are allocated at bounded size");
            game.OpenShop();game.Devices.Focus(ShopDeviceKind.Monitor);game.Devices.OpenApp(ShopComputerApp.Cameras);float elapsed=game.Elapsed;game.Tick(1);
            Check(game.Elapsed>elapsed&&game.Spawned>0,"Shop keeps running during CCTV monitoring");
            game.Devices.OpenApp(ShopComputerApp.Games);activities.StartSnake();var head=activities.Snake.Body[0];elapsed=game.Elapsed;game.Tick(.18f);
            Check(activities.Snake.Body[0]!=head&&game.Elapsed>elapsed,"Arcade and shop both advance while playing");
            activities.StartMemory();Check(activities.Memory!=null&&activities.Memory.Cards.Length==16,"Second computer minigame is playable");
            game.Devices.Release();game.RequestClose();
            for(int i=0;i<100&&game.Phase!=ShopPhase.Summary;i++){game.Tick(1);if(game.ActiveCustomer!=null)game.SkipCurrent();}
            int supplies=activities.CoffeeSupplies;game.NextDay();
            Check(game.Day==2&&activities.CoffeeOwned&&activities.CoffeeSupplies==supplies,"Day change preserves coffee upgrade and supply");
            Check(product!=null&&product.gameObject.activeSelf&&carton!=null&&carton.Quantity==1,"Loose goods and carton survive day change");
            Check(game.Drawer.Total==game.Ledger.Cash,"Activities preserve ledger and drawer equality");
            game.ResetDemo();Check(!activities.CoffeeOwned&&activities.CoffeeSupplies==0&&activities.DirtCount==0,"New game resets purchased coffee and spills");
            return new {passed,failed=0};
        }
        finally
        {
            game.Devices.Release();player.CancelHeld();body.enabled=false;player.transform.SetPositionAndRotation(position,rotation);body.enabled=true;
            camera.transform.localPosition=localCamera;camera.transform.localRotation=cameraRotation;movement.enabled=enabled;game.ResetDemo();game.ManualTick=false;
        }
    }
    public static object CatchDay()=>FullDay(true);
    public static object EscapeDay()=>FullDay(false);
    private static object FullDay(bool stopThief)
    {
        passed=0;var game=MvpShopController.Instance;var player=Object.FindAnyObjectByType<PlayerInteractor>();var camera=Camera.main;
        var body=player.GetComponent<CharacterController>();var movement=player.GetComponent<FirstPersonController>();bool enabled=movement.enabled;movement.enabled=false;
        var playerPosition=player.transform.position;var playerRotation=player.transform.rotation;var cameraPosition=camera.transform.localPosition;var cameraRotation=camera.transform.localRotation;
        var hand=new GameObject("Release check hand").transform;hand.position=Vector3.one*1000;game.ManualTick=true;
        try
        {
            game.ResetDemo();game.ClosePanel();Fund(game);
            Check(game.TryExpand()&&game.Activities.BuyCoffeeMachine()&&game.BuyPanicButton()&&game.BuyExtraShelf(),"All paid developments coexist");
            for(int type=4;type<7;type++)Check(game.OrderSupply(type),"Order self-service stock "+type);
            Check(game.Activities.OrderCoffeeSupplies(),"Order coffee ingredients");game.Tick(8.1f);
            for(int type=4;type<7;type++)for(int i=0;i<3;i++)
            {
                Check(game.TryCreateSupply(type,out var product),"Finite warehouse unit for shelf");
                var slot=Object.FindObjectsByType<ProductSlot>().First(s=>s.CanAcceptStock(ShopCatalog.Ids[type]));
                Check(product.AssignInitialSlot(slot),"Place received unit onto public shelf");
            }
            game.OpenShop();int coinsPaid=0,coffeePaid=0,selfServed=0;bool caught=false;var served=new System.Collections.Generic.HashSet<MvpCustomerActor>();var sales=new int[8];int stolenType=-1;
            for(int tick=0;tick<20000&&game.Phase!=ShopPhase.Summary;tick++)
            {
                game.Tick(.1f);
                var thief=Object.FindObjectsByType<MvpCustomerActor>().FirstOrDefault(c=>c.IsStealing&&!c.Gone);
                if(thief!=null&&stopThief&&!caught)
                {
                    Check(game.Security.Toggle()&&game.Security.Locked,"Purchased panic button traps a running thief");
                    Check(player.TryHoldTool(game.Activities.Bat),"Pick real bat to stop thief");
                    body.enabled=false;player.transform.position=thief.transform.position-thief.transform.forward*.9f;body.enabled=true;
                    camera.transform.LookAt(thief.transform.position+Vector3.up*1.1f);Physics.SyncTransforms();
                    Check(game.Activities.SwingBat(camera),"Actual short-range bat hit stops the thief");
                    Check(game.CaughtToday==1&&game.FleeingCount==0&&game.Ledger.StockLoss==0,"Caught thief creates no stock loss");player.DropHeld();game.Security.Toggle();caught=true;
                    body.enabled=false;player.transform.SetPositionAndRotation(playerPosition,playerRotation);body.enabled=true;camera.transform.localPosition=cameraPosition;camera.transform.localRotation=cameraRotation;
                }
                if(thief!=null&&!stopThief)stolenType=thief.Product;
                var customer=game.ActiveCustomer;if(customer==null||game.Stage==ServiceStage.Thanks)continue;
                if(game.Stage==ServiceStage.Order)
                {
                    if(customer.Product==ShopCatalog.Coffee)
                    {
                        var cup=game.Activities.CoffeeOutput.Occupant;
                        if(cup==null){if(game.Activities.BrewRemaining==0)game.Activities.BrewCoffee();continue;}
                        Check(cup.PickUp(hand),"Pick brewed coffee for customer");Physics.SyncTransforms();Check(cup.TryPlace(game.DeliverySlot),"Put real coffee on service counter");
                    }
                    else if(customer.Product<4)
                    {
                        var item=Object.FindObjectsByType<ShopProduct>().First(p=>p.ProductId==ShopCatalog.Ids[customer.Product]&&!p.IsHeld);
                        Check(item.PickUp(hand),"Pick requested cigarette");Physics.SyncTransforms();Check(item.TryPlace(game.DeliverySlot),"Put cigarette on counter");
                    }
                }
                if(game.Stage!=ServiceStage.Payment)continue;
                if(customer.BasketItem!=null){Check(!customer.BasketItem.IsHeld&&customer.BasketItem.CurrentSlot==game.Activities.BasketSlot,"Customer sets actual self-service item on the counter");selfServed++;}
                int cash=game.Ledger.Cash;int typeSold=customer.Product;
                if(game.CoinPayment)
                {
                    Check(!game.ConfirmCustomerCoins()&&game.Ledger.Cash==cash,"Wrong palm selection cannot charge customer");
                    game.OpenCash();Check(game.Devices.FocusKind==ShopDeviceKind.Coins,"Cash interaction focuses actual customer palm");
                    Physics.SyncTransforms();
                    foreach(var control in Object.FindObjectsByType<ShopWorldControl>().Where(c=>c.Kind==WorldControlKind.CustomerCoin))
                    {
                        var point=camera.WorldToScreenPoint(control.transform.position);
                        Check(point.z>0&&point.x>0&&point.x<camera.pixelWidth&&point.y>0&&point.y<camera.pixelHeight,"Offered coin is visible in palm view");
                        Check(Physics.Raycast(camera.ScreenPointToRay(point),out var hit,4,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<ShopWorldControl>()==control,"Actual click ray reaches the customer coin");
                    }
                    SelectCoins(game);Check(game.ConfirmCustomerCoins(),"Exact selected customer coins settle once");coinsPaid++;
                }
                else {game.OpenCash();SelectChange(game,customer.Tender-ShopCatalog.Prices[typeSold]);Check(game.SubmitChange(game.ChangeSelected),"Register settles conventional payment");}
                if(typeSold==ShopCatalog.Coffee)coffeePaid++;
                sales[typeSold]++;Check(game.Ledger.Cash==cash+ShopCatalog.Prices[typeSold]&&game.Drawer.Total==game.Ledger.Cash,"Each sale conserves cash and physical denominations");
                Check(!game.ConfirmCustomerCoins()&&!game.SubmitChange(0),"Completed payment cannot be repeated");served.Add(customer);
            }
            Check(game.Phase==ShopPhase.Summary&&game.Spawned==8&&game.Missed==0,"Complete mixed-feature eight-visitor shift");
            Check(game.Ledger.Sales==7&&coinsPaid>=2&&coffeePaid==2&&selfServed==3,"Shift includes coffee, coin payment and public shelf service");
            Check(stopThief?caught&&game.CaughtToday==1&&game.StolenToday==0:game.StolenToday==1&&game.Ledger.StockLoss==ShopCatalog.Costs[stolenType],"Theft has the expected caught or escaped result");
            for(int type=0;type<7;type++)Check(game.StockCount(type)+game.WarehouseCount(type)==(type<4?8:3)-sales[type]-(!stopThief&&type==stolenType?1:0),"All units conserved for product "+type);
            Check(game.Ledger.Cash==game.Ledger.OpeningCash+game.Ledger.Revenue-game.Ledger.Purchases-game.Ledger.Improvements-game.Ledger.OperatingCosts&&game.Drawer.Total==game.Ledger.Cash,"Whole-shift cash-flow invariant");
            Check(game.Ledger.Profit==game.Ledger.Revenue-game.Ledger.CostOfGoods-game.Ledger.StockLoss-game.Ledger.OperatingCosts,"Profit accounts for actual theft losses");
            Check(!game.Security.Locked,"Closing releases panic lock");
            var quantities=Enumerable.Range(0,7).Select(game.StockCount).ToArray();game.NextDay();
            Check(game.Day==2&&game.Activities.CoffeeOwned&&game.Security.Owned&&game.Layout.Expanded&&game.Layout.ExtraShelfOwned,"All paid developments survive next day");
            Check(Enumerable.Range(0,7).All(i=>game.StockCount(i)==quantities[i]),"Next day preserves all remaining units");
            return new {passed,failed=0,stopThief,coinsPaid,coffeePaid,selfServed};
        }
        finally
        {
            foreach(var product in Object.FindObjectsByType<ShopProduct>().Where(p=>p.IsHeld))product.ReturnToPickupPosition();
            Object.Destroy(hand.gameObject);game.Devices.Release();player.CancelHeld();body.enabled=false;player.transform.SetPositionAndRotation(playerPosition,playerRotation);body.enabled=true;
            camera.transform.localPosition=cameraPosition;camera.transform.localRotation=cameraRotation;movement.enabled=enabled;game.ResetDemo();game.ManualTick=false;
        }
    }
}
