using System.Numerics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public partial class MoneySystem : SystemBase
{
    public BigInteger Money;

    protected override void OnCreate()
    {
        base.OnCreate();
        //TODO: This is for testing, remove this
        //9999999999990000000000000000
        Money = BigInteger.Parse("9999999999990000000000000000");
    }

    protected override void OnUpdate()
    {
    }
}
