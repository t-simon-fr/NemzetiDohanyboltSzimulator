using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public static class BindingAcceptanceChecks
{
    public static object Run()
    {
        var saved = ShopBindings.Actions.ToDictionary(a => a.Key, a => PlayerPrefs.HasKey("Shop.Key." + a.Key) ? PlayerPrefs.GetString("Shop.Key." + a.Key) : null);
        int passed = 0;
        Action<bool, string> check = (ok, name) => { if (!ok) throw new Exception(name); passed++; };
        try
        {
            ShopBindings.Reset();
            check(ShopBindings.Bind(Key.E, Key.W), "Accept conflicting gameplay key");
            check(ShopBindings.Resolve(Key.E) == Key.W && ShopBindings.Resolve(Key.W) == Key.E, "Swap both conflicting actions");
            check(!ShopBindings.Bind(Key.E, Key.Enter) && ShopBindings.Resolve(Key.E) == Key.W, "Reserved terminal key rejected without changing binding");
            ShopBindings.Reset();
            check(ShopBindings.Actions.All(a => ShopBindings.Resolve(a.Key) == a.Key), "Restore every gameplay default");
            return new { passed, failed = 0 };
        }
        finally
        {
            foreach (var item in saved)
            {
                string key = "Shop.Key." + item.Key;
                if (item.Value == null) PlayerPrefs.DeleteKey(key); else PlayerPrefs.SetString(key, item.Value);
            }
            PlayerPrefs.Save();
        }
    }
}
