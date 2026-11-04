using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public static class ShopBindings
{
    public static Key Resolve(Key original){string saved=PlayerPrefs.GetString("Shop.Key."+original,original.ToString());return Enum.TryParse(saved,out Key key)?key:original;}
    public static bool Pressed(Key key)=>Keyboard.current!=null&&Keyboard.current[Resolve(key)].wasPressedThisFrame;
    public static bool Held(Key key)=>Keyboard.current!=null&&Keyboard.current[Resolve(key)].isPressed;
    public static bool Bind(Key original,Key value){if(value==Key.None||value==Key.Escape||value==Key.F1||value==Key.Enter||value==Key.NumpadEnter||value==Key.Backspace||value==Key.Delete||value==Key.Tab||value==Key.Space&&original!=Key.Space||(int)value>=(int)Key.Digit1&&(int)value<=(int)Key.Digit0)return false;foreach(var item in Actions)if(item.Key!=original&&Resolve(item.Key)==value){Key previous=Resolve(original);PlayerPrefs.SetString("Shop.Key."+item.Key,previous.ToString());break;}PlayerPrefs.SetString("Shop.Key."+original,value.ToString());PlayerPrefs.Save();return true;}
    public static readonly KeyValuePair<Key,string>[] Actions={new(Key.W,"Előre"),new(Key.S,"Hátra"),new(Key.A,"Balra"),new(Key.D,"Jobbra"),new(Key.E,"Használat / felvétel / lerakás"),new(Key.G,"Eldobás"),new(Key.Q,"Váltás"),new(Key.B,"Számítógép"),new(Key.F,"Kassza"),new(Key.X,"Vevő elküldése"),new(Key.T,"Másik áru ajánlása"),new(Key.H,"Súgó"),new(Key.LeftShift,"Sprint"),new(Key.Space,"Ugrás")};
    public static void Reset(){foreach(var item in Actions)PlayerPrefs.DeleteKey("Shop.Key."+item.Key);PlayerPrefs.Save();}
}

[DefaultExecutionOrder(-1000)]
public sealed class ShopMenu : MonoBehaviour
{
    public static bool Active { get; private set; }
    public static Rect StartRect => new Rect(364, 220, 552, 52);
    public static Rect SettingsRect => new Rect(364, 344, 552, 52);
    public static Rect DefaultsRect => new Rect(364, 562, 266, 44);
    public static Rect BackRect => new Rect(642, 562, 274, 44);
    public Rect InteractionBindingRect => new Rect(744, 158 + 4 * 40 - scroll.y, 148, 34);
    public bool SettingsOpen => settings;
    private MvpShopController shop;
    private bool settings, started, oldGate, oldVisible;
    private float previousScale = 1;
    private Key waiting;
    private Vector2 scroll;
    private CursorLockMode oldLock;
    private string feedback = "";

    public void Configure(MvpShopController value) { shop = value; Open(); }
    public void Open()
    {
        if (Active) return;
        oldGate = ShopInputGate.Blocked;
        oldLock = Cursor.lockState;
        oldVisible = Cursor.visible;
        previousScale = Time.timeScale;
        Active = true;
        Time.timeScale = 0;
        ShopInputGate.Blocked = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void Resume()
    {
        Active = false;
        settings = false;
        waiting = Key.None;
        Time.timeScale = previousScale;
        ShopInputGate.Blocked = oldGate;
        Cursor.lockState = oldGate ? oldLock : CursorLockMode.Locked;
        Cursor.visible = oldGate && oldVisible;
    }
    private void Update()
    {
        if (!Application.isFocused || shop.Arrangement != null && shop.Arrangement.Active) return;
        if (!Active)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && !ShopInputGate.Blocked) Open();
            return;
        }
        ShopInputGate.Blocked = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (Keyboard.current == null) return;
        if (waiting != Key.None)
        {
            foreach (var key in Keyboard.current.allKeys)
            {
                if (!key.wasPressedThisFrame) continue;
                if (key.keyCode == Key.Escape) { waiting = Key.None; feedback = "Átkötés megszakítva."; }
                else if (ShopBindings.Bind(waiting, key.keyCode))
                {
                    waiting = Key.None;
                    feedback = "Billentyű mentve. Az ütköző műveletek gombjai felcserélődnek.";
                }
                else feedback = "Ez a gomb a menükhöz vagy a kasszához van fenntartva. Válassz másikat.";
                break;
            }
            return;
        }
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (settings) settings = false;
            else if (started) Resume();
        }
    }
    private void OnGUI()
    {
        if (!Active) return;
        GUI.depth = -1000;
        using (ShopUi.Frame())
        {
            ShopUi.Fill(new Rect(0, 0, 1280, 720), new Color(.04f, .09f, .06f, .83f));
            ShopUi.Panel(new Rect(340, 44, 600, 632));
            ShopUi.Label(new Rect(364, 66, 552, 24), "NEMZETI  /  DOHÁNYBOLT SZIMULÁTOR", 16, true);
            ShopUi.Label(new Rect(364, 105, 552, 50), settings ? "Beállítások" : started ? "Műszakszünet" : "Jó reggelt, trafikos!", 32, true);
            if (settings) DrawSettings();
            else DrawMenu();
        }
    }
    private void DrawMenu()
    {
        ShopUi.Label(new Rect(364, 164, 552, 38), started ? "A bolt vár. A vevők most egy pillanatra nem." : "Kis bolt. Nagy történetek. A kulcs nálad van.", 18);
        if (ShopUi.Button(StartRect, started ? "Folytatás" : "Játék indítása", true)) { started = true; Resume(); }
        bool enabled = GUI.enabled;
        GUI.enabled = started;
        if (ShopUi.Button(new Rect(364, 282, 552, 52), "Új játék")) { Resume(); shop.ResetDemo(); }
        GUI.enabled = enabled;
        if (ShopUi.Button(SettingsRect, "Beállítások")) { settings = true; scroll = Vector2.zero; feedback = ""; }
        if (ShopUi.Button(new Rect(364, 406, 552, 52), "Kilépés"))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            feedback = "A böngészős játékot a lap bezárásával hagyhatod el.";
            Application.Quit();
#endif
        }
        ShopUi.Fill(new Rect(364, 500, 552, 1), ShopUi.Rule);
        ShopUi.Label(new Rect(364, 522, 552, 62), "Nyiss ki a táblával. Szolgáld ki a vevőket.\nZárás után az ajtónál fejezd be a napot.", 18);
        ShopUi.Label(new Rect(364, 622, 552, 40), feedback, 15);
    }
    private void DrawSettings()
    {
        Rect view = new Rect(364, 158, 552, 384);
        scroll = GUI.BeginScrollView(view, scroll, new Rect(0, 0, 532, ShopBindings.Actions.Length * 40));
        for (int i = 0; i < ShopBindings.Actions.Length; i++)
        {
            var item = ShopBindings.Actions[i];
            ShopUi.Label(new Rect(0, i * 40 + 6, 366, 30), item.Value, 17);
            if (ShopUi.Button(new Rect(380, i * 40, 148, 34), waiting == item.Key ? "Nyomj gombot…" : ShopUi.Key(item.Key))) waiting = item.Key;
        }
        GUI.EndScrollView();
        if (ShopUi.Button(DefaultsRect, "Alapértékek")) { ShopBindings.Reset(); waiting = Key.None; feedback = "Alapértelmezett billentyűk visszaállítva."; }
        if (ShopUi.Button(BackRect, "Vissza")) { settings = false; waiting = Key.None; }
        ShopUi.Label(new Rect(364, 620, 552, 44), string.IsNullOrEmpty(feedback) ? "Kattints egy gombra, majd nyomd meg az új billentyűt. Esc: megszakítás." : feedback, 15);
    }
    private void OnDestroy()
    {
        if (!Active) return;
        Time.timeScale = previousScale;
        Active = false;
        ShopInputGate.Blocked = false;
    }
}
