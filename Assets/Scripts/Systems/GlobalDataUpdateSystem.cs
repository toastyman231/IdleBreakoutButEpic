using System;
using System.Numerics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEditor.Timeline.Actions;
using UnityEngine;

public partial class GlobalDataUpdateSystem : SystemBase
{
    public event EventHandler<GlobalDataEventClass> UpdateLevelEvent;
    public event EventHandler<GlobalDataEventClass> UpdateClicksEvent;
    public event EventHandler<GlobalDataEventClass> UpdateMoneyEvent;
    public event EventHandler<GlobalDataEventClass> UpdateBricksEvent;
    public event EventHandler<GlobalDataEventClass> UpdateCashBonusEvent;
    public event EventHandler<GlobalDataEventClass> UpdateGlobalSpeedEvent;
    public event EventHandler<GlobalDataEventClass> UpdateGlobalPowerEvent;
    public event EventHandler<GlobalDataEventClass> UpdateGlobalBallsEvent;
    public NativeQueue<GlobalDataEventArgs> EventQueue;

    private MoneySystem _moneySystem;

    protected override void OnCreate()
    {
        EventQueue = new NativeQueue<GlobalDataEventArgs>(Allocator.Persistent);
        UpdateLevelEvent += UpdateGlobalLevel;
        UpdateMoneyEvent += UpdateGlobalMoney;
        UpdateBricksEvent += UpdateGlobalBricks;
        UpdateClicksEvent += UpdateGlobalClicks;
        UpdateCashBonusEvent += UpdateGlobalCashBonus;
        UpdateGlobalSpeedEvent += UpdateGlobalSpeedIncrease;
        UpdateGlobalPowerEvent += UpdateGlobalPower;
        UpdateGlobalBallsEvent += UpdateGlobalBalls;

        _moneySystem = World.GetOrCreateSystem<MoneySystem>();
    }

    protected override void OnDestroy()
    {
        EventQueue.Dispose();
        UpdateLevelEvent -= UpdateGlobalLevel;
        UpdateMoneyEvent -= UpdateGlobalMoney;
        UpdateBricksEvent -= UpdateGlobalBricks;
        UpdateClicksEvent -= UpdateGlobalClicks;
        UpdateCashBonusEvent -= UpdateGlobalCashBonus;
        UpdateGlobalSpeedEvent -= UpdateGlobalSpeedIncrease;
        UpdateGlobalPowerEvent -= UpdateGlobalPower;
        UpdateGlobalBallsEvent -= UpdateGlobalBalls;
    }

    protected override void OnUpdate()
    {
        while (EventQueue.TryDequeue(out GlobalDataEventArgs args))
        {
            switch (args.EventType)
            {
                case Field.LEVEL:
                    UpdateLevelEvent?.Invoke(this, new GlobalDataEventClass{EventType = args.EventType, NewData = args.NewData});
                    break;
                case Field.MONEY:
                    UpdateMoneyEvent?.Invoke(this, new GlobalDataEventClass{EventType = args.EventType, NewData = args.NewData});
                    break;
                case Field.BRICKS:
                    UpdateBricksEvent?.Invoke(this, new GlobalDataEventClass{EventType = args.EventType, NewData = args.NewData});
                    break;
                case Field.CLICKX:
                    UpdateClicksEvent?.Invoke(this, new GlobalDataEventClass{EventType = args.EventType, NewData = args.NewData});
                    break;
                case Field.CASHBONUS:
                    UpdateCashBonusEvent?.Invoke(this, new GlobalDataEventClass{EventType = args.EventType, NewData = args.NewData});
                    break;
                case Field.SPEED:
                    UpdateGlobalSpeedEvent?.Invoke(this, new GlobalDataEventClass{EventType = args.EventType, NewData = args.NewData});
                    break;
                case Field.POWER:
                    UpdateGlobalPowerEvent?.Invoke(this, new GlobalDataEventClass{EventType = args.EventType, NewData = args.NewData});
                    break;
                case Field.BALLS:
                    UpdateGlobalBallsEvent?.Invoke(this, new GlobalDataEventClass{EventType = args.EventType, NewData = args.NewData});
                    break;
            }
        }
    }
    
    private void UpdateGlobalClicks(object sender, GlobalDataEventClass args)
    {
        //Debug.Log("Updating level");
        Entities.ForEach((ref GlobalData globalData) =>
        {
            globalData.ClickX = args.NewData;
        }).WithoutBurst().Run();
    }

    private void UpdateGlobalLevel(object sender, GlobalDataEventClass args)
    {
        //Debug.Log("Updating level");
        Entities.ForEach((ref GlobalData globalData) =>
        {
            globalData.CurrentLevel = args.NewData;
        }).WithoutBurst().Run();
    }

    private void UpdateGlobalMoney(object sender, GlobalDataEventClass args)
    {
        //Debug.Log("Adding " + args.NewData + " money");
        _moneySystem.Money = BigInteger.Add(_moneySystem.Money, new BigInteger(args.NewData));
        /*Entities.ForEach((ref GlobalData globalData) =>
        {
            globalData.Money += args.NewData;
        }).WithoutBurst().Run();*/
        BallShopControl.InvokeIncreaseMoneyEvent();
    }

    private void UpdateGlobalBricks(object sender, GlobalDataEventClass args)
    {
        Entities.ForEach((ref GlobalData globalData) =>
        {
            Debug.Log("Bricks before " + globalData.Bricks);
            globalData.Bricks = args.NewData;
            Debug.Log("Bricks after " + globalData.Bricks);
        }).WithoutBurst().Run();
        World.GetOrCreateSystem<LevelControlSystem>().EventQueue.Enqueue(2);
    }

    private void UpdateGlobalCashBonus(object sender, GlobalDataEventClass args)
    {
        Entities.ForEach((ref GlobalData globalData) =>
        {
            globalData.CashBonus = args.NewData;
        }).WithoutBurst().Run();
    }

    private void UpdateGlobalSpeedIncrease(object sender, GlobalDataEventClass args)
    {
        Entities.ForEach((ref GlobalData globalData) =>
        {
            globalData.GlobalSpeed = args.NewData;
        }).WithoutBurst().Run();
    }
    
    private void UpdateGlobalPower(object sender, GlobalDataEventClass args)
    {
        Entities.ForEach((ref GlobalData globalData) =>
        {
            globalData.PowerMultiplier = args.NewData;
        }).WithoutBurst().Run();
    }
    
    private void UpdateGlobalBalls(object sender, GlobalDataEventClass args)
    {
        Entities.ForEach((ref GlobalData globalData) =>
        {
            globalData.MaxBalls = args.NewData;
        }).WithoutBurst().Run();
    }
}

public class GlobalDataEventClass : EventArgs
{
    public Field EventType;
    public int NewData;
}

public struct GlobalDataEventArgs
{
    public Field EventType;
    public int NewData;
}

public enum Field
{
    MONEY,
    LEVEL,
    BRICKS,
    CASHBONUS,
    CLICKX,
    POWER,
    SPEED,
    RANGE,
    EXTRA,
    BALLS
}
