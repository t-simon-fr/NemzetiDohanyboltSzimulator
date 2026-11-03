using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using DeviceKey=UnityEngine.InputSystem.Key;

public sealed class ShopKeyboardTarget
{
    public string Id, Label;
    public Rect Bounds;
    public Action Activate;
    public bool Enabled=true;
    public ShopKeyboardTarget(string id,string label,Rect bounds,Action activate,bool enabled=true)
    {Id=id;Label=label;Bounds=bounds;Activate=activate;Enabled=enabled;}
}

public sealed partial class ShopDevices
{
    private string keyboardTargetId;
    private bool keyboardOutline;
    public string KeyboardTargetId=>keyboardTargetId;
    private void ResetKeyboardFocus(){keyboardTargetId=null;keyboardOutline=false;}
    public List<ShopKeyboardTarget> KeyboardTargets()
    {
        var targets=new List<ShopKeyboardTarget>();if(!Focused||shop==null||view==null)return targets;
        Transform root=FocusKind==ShopDeviceKind.Monitor?monitor:FocusKind==ShopDeviceKind.Coins?shop.Activities.CustomerHand:tillAssembly;
        if(root!=null)
        {
            foreach(var control in root.GetComponentsInChildren<ShopWorldControl>())
            {
                if(!control.isActiveAndEnabled)continue;
                if(control.Kind==WorldControlKind.Denomination&&(!registerDrawer.IsOpen||shop.Drawer.Count(control.Value)<=shop.SelectedPieces(control.Value)))continue;
                string id=control.Kind+":"+control.Value;if(targets.Any(t=>t.Id==id))continue;
                var collider=control.GetComponent<Collider>();if(collider==null||!collider.enabled)continue;
                var b=collider.bounds;Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);
                for(int i=0;i<8;i++)
                {
                    var p=view.WorldToScreenPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                    var point=new Vector2(p.x,Screen.height-p.y);min=Vector2.Min(min,point);max=Vector2.Max(max,point);
                }
                string label=control.transform.parent.name;
                targets.Add(new ShopKeyboardTarget(id,label,Rect.MinMaxRect(min.x,min.y,max.x,max.y),()=>control.Activate(shop)));
            }
        }
        if(FocusKind==ShopDeviceKind.Monitor)shop.Activities.AddKeyboardTargets(targets,AppRect);
        if(FocusKind==ShopDeviceKind.Coins)
        {
            var panel=new Rect(Screen.width*.18f,Screen.height-170,Screen.width*.64f,150);
            targets.Add(new ShopKeyboardTarget("Coins:Clear","Újrakezdem",new Rect(panel.x+12,panel.y+86,panel.width*.3f,45),()=>{shop.Activities.CoinOffer?.Clear();shop.Activities.RefreshHandCoins();}));
            targets.Add(new ShopKeyboardTarget("Coins:Pay","Átveszem",new Rect(panel.x+panel.width*.35f,panel.y+86,panel.width*.62f,45),()=>shop.ConfirmCustomerCoins()));
        }
        return targets.OrderBy(t=>Mathf.Round(t.Bounds.center.y/12)).ThenBy(t=>t.Bounds.center.x).ToList();
    }
    private void PollDeviceKeyboard()
    {
        if(!Focused||!Application.isFocused||Keyboard.current==null)return;
        var keyboard=Keyboard.current;
        foreach(var key in keyboard.allKeys)if(key.wasPressedThisFrame)HandleDeviceKey(key.keyCode,keyboard.shiftKey.isPressed);
    }
    // The same entry point is used by real keyboard events and scene integration checks.
    public bool HandleDeviceKey(DeviceKey key,bool shift=false)
    {
        if(!Focused||shop==null)return false;
        bool enter=key==DeviceKey.Enter||key==DeviceKey.NumpadEnter;
        if(FocusKind==ShopDeviceKind.Till&&shop.CardPayment)
        {
            if(enter||key==DeviceKey.Space)shop.ConfirmCardPayment();
            return true;
        }
        if(FocusKind==ShopDeviceKind.Till)
        {
            if(enter){shop.SubmitChange(shop.ChangeSelected);return true;}
            if(key==DeviceKey.Backspace){shop.UndoChange();return true;}
            if(key==DeviceKey.Delete){shop.ClearChange();return true;}
            int digit=Digit(key);if(digit>=1&&digit<=6){shop.AddChange(ShopCatalog.Denominations[(shift?6:0)+digit-1]);return true;}
        }
        else if(FocusKind==ShopDeviceKind.Coins)
        {
            if(enter){shop.ConfirmCustomerCoins();return true;}
            if(key==DeviceKey.Backspace){shop.UndoCustomerCoin();return true;}
            if(key==DeviceKey.Delete){shop.Activities.CoinOffer?.Clear();shop.Activities.RefreshHandCoins();return true;}
        }
        else if(FocusKind==ShopDeviceKind.Monitor)
        {
            if(key==DeviceKey.Backspace)
            {
                if(ComputerApp==ShopComputerApp.Games&&shop.Activities.BackFromGame()){ResetKeyboardFocus();return true;}
                if(ComputerApp==ShopComputerApp.Desktop)shop.ClosePanel();else OpenApp(ShopComputerApp.Desktop);return true;
            }
            int digit=Digit(key);
            if(ComputerApp==ShopComputerApp.Desktop&&digit>=1&&digit<=5){OpenApp((ShopComputerApp)digit);return true;}
            if(ComputerApp==ShopComputerApp.Orders)
            {
                if(key==DeviceKey.Minus||key==DeviceKey.NumpadMinus){ChangeOrderQuantity(-1);return true;}
                if(key==DeviceKey.Equals||key==DeviceKey.NumpadPlus){ChangeOrderQuantity(1);return true;}
                if(digit>=1&&digit<=shop.AvailableProducts){keyboardTargetId=WorldControlKind.Order+":"+(digit-1);keyboardOutline=true;return true;}
            }
            if(ComputerApp==ShopComputerApp.Games&&shop.Activities.SnakeRunning&&(key==DeviceKey.UpArrow||key==DeviceKey.DownArrow||key==DeviceKey.LeftArrow||key==DeviceKey.RightArrow))return false;
        }
        var targets=KeyboardTargets();var enabled=targets.Where(t=>t.Enabled).ToList();if(enabled.Count==0)return false;
        var current=targets.FirstOrDefault(t=>t.Id==keyboardTargetId);
        if(key==DeviceKey.Tab)
        {
            int index=enabled.FindIndex(t=>t.Id==keyboardTargetId);
            keyboardTargetId=enabled[(index+(shift?-1:1)+enabled.Count)%enabled.Count].Id;keyboardOutline=true;return true;
        }
        if(key==DeviceKey.LeftArrow||key==DeviceKey.RightArrow||key==DeviceKey.UpArrow||key==DeviceKey.DownArrow)
        {
            var direction=key==DeviceKey.LeftArrow?Vector2.left:key==DeviceKey.RightArrow?Vector2.right:key==DeviceKey.UpArrow?Vector2.down:Vector2.up;
            var origin=current?.Bounds.center??enabled[0].Bounds.center;
            var next=enabled.Where(t=>Vector2.Dot(t.Bounds.center-origin,direction)>3)
                .OrderBy(t=>{var d=t.Bounds.center-origin;float forward=Vector2.Dot(d,direction);return forward+Mathf.Abs(d.x*direction.y-d.y*direction.x)*3;}).FirstOrDefault();
            keyboardTargetId=(current==null?enabled[0]:next??current).Id;keyboardOutline=true;return true;
        }
        if(enter||key==DeviceKey.Space)
        {
            if(current==null){current=enabled.FirstOrDefault(t=>!t.Id.StartsWith("Exit:")&&!t.Id.StartsWith("Desktop:"))??enabled[0];keyboardTargetId=current.Id;}
            keyboardOutline=true;if(current.Enabled)current.Activate();return true;
        }
        return false;
    }
    private static int Digit(DeviceKey key)
    {
        switch(key){case DeviceKey.Digit1:case DeviceKey.Numpad1:return 1;case DeviceKey.Digit2:case DeviceKey.Numpad2:return 2;case DeviceKey.Digit3:case DeviceKey.Numpad3:return 3;case DeviceKey.Digit4:case DeviceKey.Numpad4:return 4;case DeviceKey.Digit5:case DeviceKey.Numpad5:return 5;case DeviceKey.Digit6:case DeviceKey.Numpad6:return 6;case DeviceKey.Digit7:case DeviceKey.Numpad7:return 7;case DeviceKey.Digit8:case DeviceKey.Numpad8:return 8;case DeviceKey.Digit9:case DeviceKey.Numpad9:return 9;default:return -1;}
    }
    private void OnGUI()
    {
        if(!Focused)return;GUI.depth=-100;
        if(keyboardOutline&&keyboardTargetId!=null)
        {
            var target=KeyboardTargets().FirstOrDefault(t=>t.Id==keyboardTargetId);
            if(target!=null)
            {
                var old=GUI.color;GUI.color=target.Enabled?new Color(1,.77f,.18f):Color.gray;var r=target.Bounds;
                GUI.DrawTexture(new Rect(r.x-3,r.y-3,r.width+6,3),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(r.x-3,r.yMax,r.width+6,3),Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(r.x-3,r.y,3,r.height),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(r.xMax,r.y,3,r.height),Texture2D.whiteTexture);GUI.color=old;
            }
        }
        string hint=FocusKind==ShopDeviceKind.Till?"1–6: bankjegy • Shift+1–6: érme • Space: kivesz • Enter: fizetés • Backspace: vissza • Del: törlés":FocusKind==ShopDeviceKind.Coins?"Tab / nyilak: választás • Space: érme • Enter: átvétel • Backspace: vissza • Del: törlés":"Tab / nyilak: választás • Enter / Space: jóváhagyás • Backspace: vissza • Esc: kilépés";
        if(FocusKind==ShopDeviceKind.Monitor&&ComputerApp==ShopComputerApp.Desktop)hint="1: árurendelés • 2: fejlesztés • 3: kávé • 4: kamerák • 5: játékok • "+hint;
        if(FocusKind==ShopDeviceKind.Monitor&&ComputerApp==ShopComputerApp.Orders)hint="1–9: termék • + / −: mennyiség • "+hint;
        var style=new GUIStyle(GUI.skin.box){fontSize=Mathf.Clamp(Screen.width/90,12,18),wordWrap=true,alignment=TextAnchor.MiddleCenter};
        GUI.Box(new Rect(12,FocusKind==ShopDeviceKind.Coins?12:Screen.height-47,Screen.width-24,39),hint,style);
    }
}
