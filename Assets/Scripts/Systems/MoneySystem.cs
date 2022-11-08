using System.Numerics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class MoneySystem : SystemBase
{
    public BigInteger Money;
    public BigInteger Gold;
    public BigInteger GoldToClaim;

    protected override void OnCreate()
    {
        base.OnCreate();
        //TODO: This is for testing, remove this
        //9999999999990000000000000000
        Money = BigInteger.Parse("9999999999990000000000000000");
        Money += new BigInteger(PlayerPrefs.GetInt("prestiges", 0) * 50);
        Gold = BigInteger.Parse("100");//BigInteger.Parse(PlayerPrefs.GetString("gold", "0"));
        GoldToClaim = new BigInteger(20); //BigInteger.Zero;
    }

    protected override void OnUpdate()
    {
    }
}
