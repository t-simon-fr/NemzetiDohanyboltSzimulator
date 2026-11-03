using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public static class ShopBindings
{
    public static Key Resolve(Key original){string saved=PlayerPrefs.GetString("Shop.Key."+original,original.ToString());return Enum.TryParse(saved,out Key key)?key:original;}
    public static bool Pressed(Key key)=>Keyboard.current!=null&&Keyboard.current[Resolve(key)].wasPressedThisFrame;
    public static bool Held(Key key)=>Keyboard.current!=null&&Keyboard.current[Resolve(key)].isPressed;
    public static bool Bind(Key original,Key value){if(value==Key.None||value==Key.Escape||value==Key.F1||value==Key.Enter||value==Key.NumpadEnter||value==Key.Backspace||value==Key.Delete||value==Key.Tab||value==Key.Space||(int)value>=(int)Key.Digit1&&(int)value<=(int)Key.Digit0)return false;foreach(var item in Actions)if(item.Key!=original&&Resolve(item.Key)==value){Key previous=Resolve(original);PlayerPrefs.SetString("Shop.Key."+item.Key,previous.ToString());break;}PlayerPrefs.SetString("Shop.Key."+original,value.ToString());PlayerPrefs.Save();return true;}
    public static readonly KeyValuePair<Key,string>[] Actions={new(Key.W,"Előre"),new(Key.S,"Hátra"),new(Key.A,"Balra"),new(Key.D,"Jobbra"),new(Key.E,"Használat / felvétel / lerakás"),new(Key.G,"Eldobás"),new(Key.Q,"Váltás"),new(Key.B,"Számítógép"),new(Key.F,"Kassza"),new(Key.O,"Nyitás / zárás"),new(Key.X,"Vevő elküldése"),new(Key.T,"Párbeszéd"),new(Key.H,"Súgó")};
    public static void Reset(){foreach(var item in Actions)PlayerPrefs.DeleteKey("Shop.Key."+item.Key);PlayerPrefs.Save();}
}

[DefaultExecutionOrder(-1000)]
public sealed class ShopMenu:MonoBehaviour
{
    public static bool Active {get;private set;}
    private MvpShopController shop;private bool settings,started;private float previousScale=1;private Key waiting;private Vector2 scroll;private bool oldGate;private CursorLockMode oldLock;private bool oldVisible;
    public void Configure(MvpShopController value){shop=value;Open();}
    public void Open(){if(Active)return;oldGate=ShopInputGate.Blocked;oldLock=Cursor.lockState;oldVisible=Cursor.visible;previousScale=Time.timeScale;Active=true;Time.timeScale=0;ShopInputGate.Blocked=true;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
    public void Resume(){Active=false;settings=false;waiting=Key.None;Time.timeScale=previousScale;ShopInputGate.Blocked=oldGate;Cursor.lockState=oldLock;Cursor.visible=oldVisible;}
    private void Update(){if(!Application.isFocused)return;if(Active){ShopInputGate.Blocked=true;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;if(waiting!=Key.None&&Keyboard.current!=null){foreach(var key in Keyboard.current.allKeys)if(key.wasPressedThisFrame){if(key.keyCode==Key.Escape)waiting=Key.None;else if(ShopBindings.Bind(waiting,key.keyCode))waiting=Key.None;break;}return;}if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame){if(settings)settings=false;else if(started)Resume();}}else if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)Open();}
    private void OnGUI(){if(!Active)return;GUI.depth=-1000;float w=Mathf.Min(600,Screen.width-24),h=Mathf.Min(650,Screen.height-24);GUILayout.BeginArea(new Rect((Screen.width-w)/2,(Screen.height-h)/2,w,h),GUI.skin.box);GUILayout.Label(settings?"BEÁLLÍTÁSOK – BILLENTYŰKIOSZTÁS":"NEMZETI DOHÁNYBOLT SZIMULÁTOR");if(settings){GUILayout.Label("Kattints egy műveletre, majd nyomd meg az új gombot. Az ütköző gombok felcserélődnek. A számok és a menükezelő gombok fenntartottak. Esc: megszakítás.");scroll=GUILayout.BeginScrollView(scroll);foreach(var item in ShopBindings.Actions)if(GUILayout.Button(item.Value+": "+(waiting==item.Key?"Nyomj meg egy gombot…":ShopBindings.Resolve(item.Key).ToString()),GUILayout.Height(32)))waiting=item.Key;GUILayout.EndScrollView();if(GUILayout.Button("Alapértékek",GUILayout.Height(35)))ShopBindings.Reset();if(GUILayout.Button("Vissza",GUILayout.Height(35))){settings=false;waiting=Key.None;}}else{if(GUILayout.Button(started?"Folytatás":"Játék indítása",GUILayout.Height(48))){started=true;Resume();}if(started&&GUILayout.Button("Új játék",GUILayout.Height(48))){Resume();shop.ResetDemo();}if(GUILayout.Button("Beállítások",GUILayout.Height(48)))settings=true;if(GUILayout.Button("Kilépés",GUILayout.Height(48))){
#if UNITY_EDITOR
UnityEditor.EditorApplication.isPlaying=false;
#else
Application.Quit();
#endif
}}GUILayout.EndArea();}
    private void OnDestroy(){if(Active){Time.timeScale=previousScale;Active=false;ShopInputGate.Blocked=false;}}
}
