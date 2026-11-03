using System;
using System.Collections.Generic;
using UnityEngine;

// Pure rules shared by the TobaccOS games and their deterministic checks.
public sealed class ShopSnake
{
    public const int Width=18, Height=10;
    private readonly System.Random random;
    public readonly List<Vector2Int> Body=new List<Vector2Int>();
    public Vector2Int Food { get; private set; }
    public Vector2Int Direction { get; private set; }=Vector2Int.right;
    private Vector2Int pending=Vector2Int.right;
    public bool Ended { get; private set; }
    public int Score { get; private set; }
    public ShopSnake(int seed)
    { random=new System.Random(seed);Body.Add(new Vector2Int(4,5));Body.Add(new Vector2Int(3,5));Body.Add(new Vector2Int(2,5));PlaceFood(); }
    public void Turn(Vector2Int direction)
    { if(Math.Abs(direction.x)+Math.Abs(direction.y)==1 && direction!=-Direction)pending=direction; }
    public void Step()
    {
        if(Ended)return;Direction=pending;var head=Body[0]+Direction;
        bool eating=head==Food;
        if(head.x<0||head.x>=Width||head.y<0||head.y>=Height){Ended=true;return;}
        for(int i=0;i<Body.Count-(eating?0:1);i++)if(Body[i]==head){Ended=true;return;}
        Body.Insert(0,head);
        if(eating){Score+=10;if(Body.Count==Width*Height){Ended=true;return;}PlaceFood();}
        else Body.RemoveAt(Body.Count-1);
    }
    private void PlaceFood()
    {
        var free=new List<Vector2Int>();
        for(int y=0;y<Height;y++)for(int x=0;x<Width;x++){var cell=new Vector2Int(x,y);if(!Body.Contains(cell))free.Add(cell);}
        Food=free[random.Next(free.Count)];
    }
}
public sealed class ShopMemoryGame
{
    public readonly int[] Cards=new int[16];
    public readonly bool[] Matched=new bool[16];
    public int First { get; private set; }=-1;
    public int Second { get; private set; }=-1;
    public int Moves { get; private set; }
    public int Pairs { get; private set; }
    public bool Won=>Pairs==8;
    private float hideTimer;
    public ShopMemoryGame(int seed)
    {
        for(int i=0;i<Cards.Length;i++)Cards[i]=i/2;
        var random=new System.Random(seed);
        for(int i=Cards.Length-1;i>0;i--){int j=random.Next(i+1);int swap=Cards[i];Cards[i]=Cards[j];Cards[j]=swap;}
    }
    public bool Choose(int index)
    {
        if(index<0||index>=Cards.Length||Matched[index]||index==First||Second>=0||Won)return false;
        if(First<0){First=index;return true;}
        Second=index;Moves++;
        if(Cards[First]==Cards[Second]){Matched[First]=Matched[Second]=true;Pairs++;First=Second=-1;}
        else hideTimer=.9f;
        return true;
    }
    public void Tick(float dt){if(Second<0)return;hideTimer-=Mathf.Max(0,dt);if(hideTimer<=0)First=Second=-1;}
    public bool Visible(int index)=>Matched[index]||First==index||Second==index;
}
public sealed class ShopCoinOffer
{
    public readonly List<int> Coins=new List<int>();
    public readonly List<bool> Selected=new List<bool>();
    private readonly List<int> selectionOrder=new List<int>();
    public int Price { get; }
    public int Total { get {int value=0;for(int i=0;i<Coins.Count;i++)if(Selected[i])value+=Coins[i];return value;} }
    public ShopCoinOffer(int price,int seed)
    {
        if(price<=0||price%5!=0)throw new ArgumentOutOfRangeException(nameof(price));Price=price;
        int rest=price;foreach(int value in new[]{200,100,50,20,10,5})while(rest>=value){Coins.Add(value);rest-=value;}
        Coins.AddRange(new[]{100,50,20,10});var random=new System.Random(seed);
        for(int i=Coins.Count-1;i>0;i--){int j=random.Next(i+1);int swap=Coins[i];Coins[i]=Coins[j];Coins[j]=swap;}
        foreach(int value in Coins)Selected.Add(false);
    }
    public bool Toggle(int index){if(index<0||index>=Coins.Count)return false;Selected[index]=!Selected[index];selectionOrder.Remove(index);if(Selected[index])selectionOrder.Add(index);return true;}
    public bool Undo(){if(selectionOrder.Count==0)return false;return Toggle(selectionOrder[selectionOrder.Count-1]);}
    public void Clear(){for(int i=0;i<Selected.Count;i++)Selected[i]=false;selectionOrder.Clear();}
    public int[] Pieces()
    {
        var pieces=new int[ShopCatalog.Denominations.Length];
        for(int i=0;i<Coins.Count;i++)if(Selected[i])pieces[Array.IndexOf(ShopCatalog.Denominations,Coins[i])]++;
        return pieces;
    }
}
