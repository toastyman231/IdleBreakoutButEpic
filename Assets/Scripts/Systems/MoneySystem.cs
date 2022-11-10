using System;
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
    public event EventHandler UpdateGoldStepEvent;

    public NativeQueue<int> GoldStepEventQueue;

    public BigInteger Money;
    public BigInteger Gold;
    public BigInteger GoldStep;
    public BigInteger NextGoldIncrease;
    public BigInteger GoldToClaim;

    protected override void OnCreate()
    {
        base.OnCreate();
        GoldStepEventQueue = new NativeQueue<int>(Allocator.Persistent);
        UpdateGoldStepEvent += UpdateGoldStep;
        //TODO: This is for testing, remove this
        //9999999999990000000000000000
        Money = BigInteger.Parse("50");
        Money += new BigInteger(PlayerPrefs.GetInt("prestiges", 0) * 50);
        GoldStep = 1;
        NextGoldIncrease = 30;
        Gold = BigInteger.Parse("100");//BigInteger.Parse(PlayerPrefs.GetString("gold", "0"));
        GoldToClaim = new BigInteger(20); //BigInteger.Zero;
    }

    protected override void OnUpdate()
    {
        while (GoldStepEventQueue.TryDequeue(out var item))
        {
            UpdateGoldStepEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        GoldStepEventQueue.Dispose();
        UpdateGoldStepEvent -= UpdateGoldStep;
    }

    private void UpdateGoldStep(object sender, EventArgs args)
    {
        GoldToClaim += GoldStep;
    }
}
