using System;

public enum ShopPartnerKind { Hamster, LocaCola, Muszakkezdo }

public static class ShopPartnerCatalog
{
    public const int PackagePrice = 20000;
    public const int FirstVariant = 16;
    public const int Zero = 16, Mango = 17, Fantom = 18, Spritz = 19;
    public const int Plum = 20, Pear = 21, Mixed = 22;
    public const int FirstFurnitureKind = 7;
    public static readonly string[] Names = { "Hamster Energy", "Loca Cola", "Kevert / Műszakkezdő" };
    public static readonly string[] Details = {
        "Zero • Pacific Mango • Hamster hűtő\n10% beszerzési kedvezmény",
        "Fantom • Spritz • Loca Cola hűtő\n10% beszerzési kedvezmény",
        "Szilva • Körte • Vegyes • Pulti display\n10% beszerzési kedvezmény"
    };
    public static bool Valid(ShopPartnerKind kind) => (int)kind >= 0 && (int)kind < Names.Length;
    public static bool IsVariant(int type) => type >= FirstVariant && type <= Mixed;
    public static bool IsSpirit(int type) => type >= Plum && type <= Mixed;
    public static int PartnerFor(int type)
    {
        if (type == 6 || type == Zero || type == Mango) return 0;
        if (type == 5 || type == Fantom || type == Spritz) return 1;
        if (type == 7 || IsSpirit(type)) return 2;
        return -1;
    }
    public static int DiscountedCost(int normalCost)
    {
        if (normalCost < 0) throw new ArgumentOutOfRangeException(nameof(normalCost));
        return (int)((long)normalCost * 90 / 100);
    }
}

// Contracts contain no money or Unity references. Purchase orchestration owns the transaction.
public sealed class ShopPartnerState
{
    private readonly bool[] owned = new bool[3];
    public bool AnyOwned => owned[0] || owned[1] || owned[2];
    public bool Owned(ShopPartnerKind kind) => ShopPartnerCatalog.Valid(kind) && owned[(int)kind];
    public bool Unlocked(int type)
    {
        int partner = ShopPartnerCatalog.PartnerFor(type);
        return !ShopPartnerCatalog.IsVariant(type) || partner >= 0 && owned[partner];
    }
    public int UnitCost(int type)
    {
        int normal = ShopCatalog.Costs[type];
        int partner = ShopPartnerCatalog.PartnerFor(type);
        return partner >= 0 && owned[partner] ? ShopPartnerCatalog.DiscountedCost(normal) : normal;
    }
    public bool Activate(ShopPartnerKind kind)
    {
        if (!ShopPartnerCatalog.Valid(kind) || Owned(kind)) return false;
        owned[(int)kind] = true;
        return true;
    }
    public void Reset() => Array.Clear(owned, 0, owned.Length);
}
