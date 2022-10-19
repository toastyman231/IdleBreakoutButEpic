using System;
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
    public event EventHandler<GlobalDataEventClass> UpdateMoneyEvent;
    public event EventHandler<GlobalDataEventClass> UpdateBricksEvent;
    public NativeQueue<GlobalDataEventArgs> EventQueue;

    protected override void OnCreate()
    {
        EventQueue = new NativeQueue<GlobalDataEventArgs>(Allocator.Persistent);
        UpdateLevelEvent += UpdateGlobalLevel;
        UpdateMoneyEvent += UpdateGlobalMoney;
        UpdateBricksEvent += UpdateGlobalBricks;
    }

    protected override void OnDestroy()
    {
        EventQueue.Dispose();
        UpdateLevelEvent -= UpdateGlobalLevel;
        UpdateMoneyEvent -= UpdateGlobalMoney;
        UpdateBricksEvent -= UpdateGlobalBricks;
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
            }
        }
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
        Entities.ForEach((ref GlobalData globalData) =>
        {
            globalData.Money += args.NewData;
        }).WithoutBurst().Run();
    }

    private void UpdateGlobalBricks(object sender, GlobalDataEventClass args)
    {
        Entities.ForEach((ref GlobalData globalData) =>
        {
            globalData.Bricks = args.NewData;
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
    BRICKS
}
