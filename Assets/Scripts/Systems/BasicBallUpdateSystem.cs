using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Systems
{
    [BurstCompile]
    public partial class BallSharedDataUpdateSystem : SystemBase
    {
        private event EventHandler<SharedDataArgs> UpdateSharedDataEvent;
        
        [BurstCompile]
        protected override void OnCreate()
        {
            base.OnCreate();
            UpdateSharedDataEvent += UpdateBasicBallData;
        }
        
        [BurstCompile]
        protected override void OnDestroy()
        {
            UpdateSharedDataEvent -= UpdateBasicBallData;
        }
        
        [BurstCompile]
        protected override void OnUpdate()
        {
            return;
        }

        public NativeArray<int> GetBallDataByType(BallType ballType)
        {
            switch (ballType)
            {
                case BallType.BasicBall:
                    BasicBallSharedData ballData = GetSingleton<BasicBallSharedData>();
                    return new NativeArray<int>(new []{ballData.Speed, ballData.Power, ballData.Cost, ballData.Count}, Allocator.Temp);
                default:
                    return new NativeArray<int>(new int[] { }, Allocator.Temp);
            }
        }

        [BurstCompile]
        public void InvokeUpdateSharedDataEvent(int power = 0, int speed = 0, int cost = 0, int count = 0)
        {
            UpdateSharedDataEvent?.Invoke(this, new SharedDataArgs
            {
                NewPower = power,
                NewSpeed = speed,
                NewCost = cost,
                NewCount = count
            });
        }
    
        [BurstCompile]
        private void UpdateBasicBallData(object sender, SharedDataArgs args)
        {
            var ballDataJob = new UpdateBallDataJob
            {
                NewPower = args.NewPower,
                NewSpeed = args.NewSpeed,
                NewCost = args.NewCost,
                NewCount = args.NewCount
            }.ScheduleParallel();
            
            ballDataJob.Complete();
            //Debug.Log(GetSingleton<BasicBallSharedData>().Count);
        }
        
        [BurstCompile]
        private partial struct UpdateBallDataJob : IJobEntity
        {
            public int NewPower;
            public int NewSpeed;
            public int NewCost;
            public int NewCount;
            
            [BurstCompile]
            void Execute(ref BasicBallSharedData ballSharedData)
            {
                ballSharedData.Power = NewPower;
                ballSharedData.Speed = NewSpeed;
                ballSharedData.Cost = NewCost;
                ballSharedData.Count = NewCount;
            }
        }
    }
    
    public class SharedDataArgs : EventArgs
    {
        public int NewPower;
        public int NewSpeed;
        public int NewCost;
        public int NewCount;
    }
}