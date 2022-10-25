using System;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Systems
{
    [BurstCompile]
    public partial class BallSharedDataUpdateSystem : SystemBase
    {
        [BurstCompile]
        protected override void OnUpdate()
        {
            return;
        }
        
        [BurstCompile]
        public void SetBallData(BallType type, bool update, params int[] data)
        {
            JobHandle ballDataJob;
            
            switch (type)
            {
                default:
                    ballDataJob = new SetBasicBallDataJob
                    {
                        Update = update,
                        NewPower = data[0],
                        NewSpeed = data[1],
                        NewCost = data[2],
                        NewCount = data[3]
                    }.ScheduleParallel();
                    break;
                case BallType.PlasmaBall:
                    ballDataJob = new SetPlasmaBallDataJob
                    {
                        Update = update,
                        NewPower = data[0],
                        NewSpeed = data[1],
                        NewCost = data[2],
                        NewCount = data[3],
                        NewRange = data[4]
                    }.ScheduleParallel();
                    break;
                case BallType.SniperBall:
                    ballDataJob = new SetSniperBallDataJob
                    {
                        Update = update,
                        NewPower = data[0],
                        NewSpeed = data[1],
                        NewCost = data[2],
                        NewCount = data[3]
                    }.ScheduleParallel();
                    break;
            }

            ballDataJob.Complete();
            //Debug.Log(GetSingleton<BasicBallSharedData>().Count);
        }

        [BurstCompile]
        private partial struct SetBasicBallDataJob : IJobEntity
        {
            public int NewPower;
            public int NewSpeed;
            public int NewCost;
            public int NewCount;
            public bool Update;
            
            [BurstCompile]
            void Execute(ref BasicBallSharedData ballSharedData, in BallTag tag)
            {
                if (Update)
                {
                    if (NewPower >= 0) ballSharedData.Power += NewPower;
                    if (NewSpeed >= 0) ballSharedData.Speed += NewSpeed;
                    if (NewCost >= 0) ballSharedData.Cost += NewCost;
                    if (NewCount >= 0) ballSharedData.Count += NewCount;
                }
                else
                {
                    if (NewPower >= 0) ballSharedData.Power = NewPower;
                    if (NewSpeed >= 0) ballSharedData.Speed = NewSpeed;
                    if (NewCost >= 0) ballSharedData.Cost = NewCost;
                    if (NewCount >= 0) ballSharedData.Count = NewCount;
                }
            }
        }

        [BurstCompile]
        private partial struct SetPlasmaBallDataJob : IJobEntity
        {
            public int NewPower;
            public int NewSpeed;
            public int NewCost;
            public int NewCount;
            public int NewRange;
            public bool Update;

            [BurstCompile]
            void Execute(ref BasicBallSharedData ballSharedData, ref PlasmaBallSharedData plasmaSharedData, in PlasmaTag tag)
            {
                if (Update)
                {
                    if (NewPower >= 0) ballSharedData.Power += NewPower;
                    if (NewSpeed >= 0) ballSharedData.Speed += NewSpeed;
                    if (NewCost >= 0) ballSharedData.Cost += NewCost;
                    if (NewCount >= 0) ballSharedData.Count += NewCount;
                    if (NewRange >= 0) plasmaSharedData.Range += NewRange;
                }
                else
                {
                    if (NewPower >= 0) ballSharedData.Power = NewPower;
                    if (NewSpeed >= 0) ballSharedData.Speed = NewSpeed;
                    if (NewCost >= 0) ballSharedData.Cost = NewCost;
                    if (NewCount >= 0) ballSharedData.Count = NewCount;
                    if (NewRange >= 0) plasmaSharedData.Range = NewRange;
                }
            }
        }
        
        [BurstCompile]
        private partial struct SetSniperBallDataJob : IJobEntity
        {
            public int NewPower;
            public int NewSpeed;
            public int NewCost;
            public int NewCount;
            public bool Update;
            
            [BurstCompile]
            void Execute(ref BasicBallSharedData ballSharedData, in SniperTag tag)
            {
                if (Update)
                {
                    if (NewPower >= 0) ballSharedData.Power += NewPower;
                    if (NewSpeed >= 0) ballSharedData.Speed += NewSpeed;
                    if (NewCost >= 0) ballSharedData.Cost += NewCost;
                    if (NewCount >= 0) ballSharedData.Count += NewCount;
                }
                else
                {
                    if (NewPower >= 0) ballSharedData.Power = NewPower;
                    if (NewSpeed >= 0) ballSharedData.Speed = NewSpeed;
                    if (NewCost >= 0) ballSharedData.Cost = NewCost;
                    if (NewCount >= 0) ballSharedData.Count = NewCount;
                }
            }
        }
    }
}