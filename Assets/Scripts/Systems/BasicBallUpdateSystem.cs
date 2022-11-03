using System;
using System.Numerics;
using Tags;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
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
        public void SetBallData(BallType type, bool update, params object[] data)
        {
            JobHandle ballDataJob;
            
            switch (type)
            {
                default:
                    ballDataJob = new SetBasicBallDataJob
                    {
                        Update = update,
                        NewPower = (int) data[0],
                        NewSpeed = (int) data[1],
                        NewCost = (string) data[2],
                        NewCount = (int) data[3],
                        DeltaTime = Time.DeltaTime,
                        GlobalData = GetSingleton<GlobalData>()
                    }.ScheduleParallel();
                    break;
                case BallType.PlasmaBall:
                    ballDataJob = new SetPlasmaBallDataJob
                    {
                        Update = update,
                        NewPower = (int) data[0],
                        NewSpeed = (int) data[1],
                        NewCost = (string) data[2],
                        NewCount = (int) data[3],
                        NewRange = (int) data[4],
                        DeltaTime = Time.DeltaTime,
                        GlobalData = GetSingleton<GlobalData>()
                    }.ScheduleParallel();
                    break;
                case BallType.SniperBall:
                    ballDataJob = new SetSniperBallDataJob
                    {
                        Update = update,
                        NewPower = (int) data[0],
                        NewSpeed = (int) data[1],
                        NewCost = (string) data[2],
                        NewCount = (int) data[3],
                        DeltaTime = Time.DeltaTime,
                        GlobalData = GetSingleton<GlobalData>()
                    }.ScheduleParallel();
                    break;
                case BallType.ScatterBall:
                    ballDataJob = new SetScatterBallDataJob
                    {
                        Update = update,
                        NewPower = (int) data[0],
                        NewSpeed = (int) data[1],
                        NewCost = (string) data[2],
                        NewCount = (int) data[3],
                        NewBalls = (int) data[4],
                        DeltaTime = Time.DeltaTime,
                        GlobalData = GetSingleton<GlobalData>()
                    }.ScheduleParallel();
                    break;
                case BallType.Cannonball:
                    ballDataJob = new SetCannonballDataJob
                    {
                        Update = update,
                        NewPower = (int) data[0],
                        NewSpeed = (int) data[1],
                        NewCost = (string) data[2],
                        NewCount = (int) data[3],
                        DeltaTime = Time.DeltaTime,
                        GlobalData = GetSingleton<GlobalData>()
                    }.ScheduleParallel();
                    break;
                case BallType.PoisonBall:
                    ballDataJob = new SetPoisonBallDataJob
                    {
                        Update = update,
                        NewPower = (int) data[0],
                        NewSpeed = (int) data[1],
                        NewCost = (string) data[2],
                        NewCount = (int) data[3],
                        DeltaTime = Time.DeltaTime,
                        GlobalData = GetSingleton<GlobalData>()
                    }.ScheduleParallel();
                    break;
            }

            ballDataJob.Complete();
            //Debug.Log(GetSingleton<BasicBallSharedData>().Count);
        }

        //[BurstCompile]
        private partial struct SetBasicBallDataJob : IJobEntity
        {
            public float DeltaTime;
            public GlobalData GlobalData;
            public int NewPower;
            public int NewSpeed;
            public FixedString64Bytes NewCost;
            public int NewCount;
            public bool Update;
            
            //[BurstCompile]
            void Execute(ref BasicBallSharedData ballSharedData, ref PhysicsVelocity pv, in BallTag tag)
            {
                if (Update)
                {
                    if (NewPower >= 0) ballSharedData.Power += NewPower;
                    if (NewSpeed >= 0)
                    {
                        float3 direction = math.normalize(pv.Linear);
                        ballSharedData.Speed += NewSpeed;
                        pv.Linear = direction * ballSharedData.Speed * GlobalData.SpeedScale * GlobalData.GlobalSpeed * DeltaTime;
                    }
                    if (BigInteger.Compare(BigInteger.Parse(NewCost.ToString()), BigInteger.Zero) >= 0) 
                        ballSharedData.Cost = BigInteger.Add(BigInteger.Parse(ballSharedData.Cost.ToString()), 
                            BigInteger.Parse(NewCost.ToString())).ToString();
                    if (NewCount >= 0) ballSharedData.Count += NewCount;
                }
                else
                {
                    if (NewPower >= 0) ballSharedData.Power = NewPower;
                    if (NewSpeed >= 0)
                    {
                        float3 direction = math.normalize(pv.Linear);
                        ballSharedData.Speed = NewSpeed;
                        pv.Linear = direction * ballSharedData.Speed * GlobalData.SpeedScale * GlobalData.GlobalSpeed * DeltaTime;
                    }
                    if (BigInteger.Compare(BigInteger.Parse(NewCost.ToString()), BigInteger.Zero) >= 0)
                        ballSharedData.Cost = NewCost;
                    if (NewCount >= 0) ballSharedData.Count = NewCount;
                }
            }
        }

        //[BurstCompile]
        private partial struct SetPlasmaBallDataJob : IJobEntity
        {
            public float DeltaTime;
            public GlobalData GlobalData;
            public int NewPower;
            public int NewSpeed;
            public FixedString64Bytes NewCost;
            public int NewCount;
            public int NewRange;
            public bool Update;

            //[BurstCompile]
            void Execute(ref BasicBallSharedData ballSharedData, ref PlasmaBallSharedData plasmaSharedData, ref PhysicsVelocity pv, in PlasmaTag tag)
            {
                if (Update)
                {
                    if (NewPower >= 0) ballSharedData.Power += NewPower;
                    if (NewSpeed >= 0)
                    {
                        float3 direction = math.normalize(pv.Linear);
                        ballSharedData.Speed += NewSpeed;
                        pv.Linear = direction * ballSharedData.Speed * GlobalData.SpeedScale * GlobalData.GlobalSpeed * DeltaTime;
                    }
                    if (BigInteger.Compare(BigInteger.Parse(NewCost.ToString()), BigInteger.Zero) >= 0) 
                        ballSharedData.Cost = BigInteger.Add(BigInteger.Parse(ballSharedData.Cost.ToString()), 
                            BigInteger.Parse(NewCost.ToString())).ToString();
                    if (NewCount >= 0) ballSharedData.Count += NewCount;
                    if (NewRange >= 0) plasmaSharedData.Range += NewRange;
                }
                else
                {
                    if (NewPower >= 0) ballSharedData.Power = NewPower;
                    if (NewSpeed >= 0)
                    {
                        float3 direction = math.normalize(pv.Linear);
                        ballSharedData.Speed = NewSpeed;
                        pv.Linear = direction * ballSharedData.Speed * GlobalData.SpeedScale * GlobalData.GlobalSpeed * DeltaTime;
                    }
                    if (BigInteger.Compare(BigInteger.Parse(NewCost.ToString()), BigInteger.Zero) >= 0)
                        ballSharedData.Cost = NewCost;
                    if (NewCount >= 0) ballSharedData.Count = NewCount;
                    if (NewRange >= 0) plasmaSharedData.Range = NewRange;
                }
            }
        }
        
        //[BurstCompile]
        private partial struct SetSniperBallDataJob : IJobEntity
        {
            public float DeltaTime;
            public GlobalData GlobalData;
            public int NewPower;
            public int NewSpeed;
            public FixedString64Bytes NewCost;
            public int NewCount;
            public bool Update;
            
            //[BurstCompile]
            void Execute(ref BasicBallSharedData ballSharedData, ref PhysicsVelocity pv, in SniperTag tag)
            {
                if (Update)
                {
                    if (NewPower >= 0) ballSharedData.Power += NewPower;
                    if (NewSpeed >= 0)
                    {
                        float3 direction = math.normalize(pv.Linear);
                        ballSharedData.Speed += NewSpeed;
                        pv.Linear = direction * ballSharedData.Speed * GlobalData.SpeedScale * GlobalData.GlobalSpeed * DeltaTime;
                    }
                    if (BigInteger.Compare(BigInteger.Parse(NewCost.ToString()), BigInteger.Zero) >= 0) 
                        ballSharedData.Cost = BigInteger.Add(BigInteger.Parse(ballSharedData.Cost.ToString()), 
                            BigInteger.Parse(NewCost.ToString())).ToString();
                    if (NewCount >= 0) ballSharedData.Count += NewCount;
                }
                else
                {
                    if (NewPower >= 0) ballSharedData.Power = NewPower;
                    if (NewSpeed >= 0)
                    {
                        float3 direction = math.normalize(pv.Linear);
                        ballSharedData.Speed = NewSpeed;
                        pv.Linear = direction * ballSharedData.Speed * GlobalData.SpeedScale * GlobalData.GlobalSpeed * DeltaTime;
                    }
                    if (BigInteger.Compare(BigInteger.Parse(NewCost.ToString()), BigInteger.Zero) >= 0)
                        ballSharedData.Cost = NewCost;
                    if (NewCount >= 0) ballSharedData.Count = NewCount;
                }
            }
        }
        
        private partial struct SetScatterBallDataJob : IJobEntity
        {
            public float DeltaTime;
            public GlobalData GlobalData;
            public int NewPower;
            public int NewSpeed;
            public FixedString64Bytes NewCost;
            public int NewCount;
            public int NewBalls;
            public bool Update;

            //[BurstCompile]
            void Execute(ref BasicBallSharedData ballSharedData, ref ScatterBallSharedData scatterSharedData, ref PhysicsVelocity pv, in ScatterTag tag)
            {
                if (Update)
                {
                    if (NewPower >= 0) ballSharedData.Power += NewPower;
                    if (NewSpeed >= 0)
                    {
                        float3 direction = math.normalize(pv.Linear);
                        ballSharedData.Speed += NewSpeed;
                        pv.Linear = direction * ballSharedData.Speed * GlobalData.SpeedScale * GlobalData.GlobalSpeed * DeltaTime;
                    }
                    if (BigInteger.Compare(BigInteger.Parse(NewCost.ToString()), BigInteger.Zero) >= 0) 
                        ballSharedData.Cost = BigInteger.Add(BigInteger.Parse(ballSharedData.Cost.ToString()), 
                            BigInteger.Parse(NewCost.ToString())).ToString();
                    if (NewCount >= 0) ballSharedData.Count += NewCount;
                    if (NewBalls >= 0) scatterSharedData.ExtraBalls += NewBalls;
                }
                else
                {
                    if (NewPower >= 0) ballSharedData.Power = NewPower;
                    if (NewSpeed >= 0)
                    {
                        float3 direction = math.normalize(pv.Linear);
                        ballSharedData.Speed = NewSpeed;
                        pv.Linear = direction * ballSharedData.Speed * GlobalData.SpeedScale * GlobalData.GlobalSpeed * DeltaTime;
                    }
                    if (BigInteger.Compare(BigInteger.Parse(NewCost.ToString()), BigInteger.Zero) >= 0)
                        ballSharedData.Cost = NewCost;
                    if (NewCount >= 0) ballSharedData.Count = NewCount;
                    if (NewBalls >= 0) scatterSharedData.ExtraBalls = NewBalls;
                }
            }
        }
        
        private partial struct SetCannonballDataJob : IJobEntity
        {
            public float DeltaTime;
            public GlobalData GlobalData;
            public int NewPower;
            public int NewSpeed;
            public FixedString64Bytes NewCost;
            public int NewCount;
            public bool Update;

            //[BurstCompile]
            void Execute(ref BasicBallSharedData ballSharedData, ref PhysicsVelocity pv, in CannonballTag tag)
            {
                if (Update)
                {
                    if (NewPower >= 0) ballSharedData.Power += NewPower;
                    if (NewSpeed >= 0)
                    {
                        float3 direction = math.normalize(pv.Linear);
                        ballSharedData.Speed += NewSpeed;
                        pv.Linear = direction * ballSharedData.Speed * GlobalData.SpeedScale * GlobalData.GlobalSpeed * DeltaTime;
                    }
                    if (BigInteger.Compare(BigInteger.Parse(NewCost.ToString()), BigInteger.Zero) >= 0) 
                        ballSharedData.Cost = BigInteger.Add(BigInteger.Parse(ballSharedData.Cost.ToString()), 
                            BigInteger.Parse(NewCost.ToString())).ToString();
                    if (NewCount >= 0) ballSharedData.Count += NewCount;
                }
                else
                {
                    if (NewPower >= 0) ballSharedData.Power = NewPower;
                    if (NewSpeed >= 0)
                    {
                        float3 direction = math.normalize(pv.Linear);
                        ballSharedData.Speed = NewSpeed;
                        pv.Linear = direction * ballSharedData.Speed * GlobalData.SpeedScale * GlobalData.GlobalSpeed * DeltaTime;
                    }
                    if (BigInteger.Compare(BigInteger.Parse(NewCost.ToString()), BigInteger.Zero) >= 0)
                        ballSharedData.Cost = NewCost;
                    if (NewCount >= 0) ballSharedData.Count = NewCount;
                }
            }
        }
        
        private partial struct SetPoisonBallDataJob : IJobEntity
        {
            public float DeltaTime;
            public GlobalData GlobalData;
            public int NewPower;
            public int NewSpeed;
            public FixedString64Bytes NewCost;
            public int NewCount;
            public bool Update;

            //[BurstCompile]
            void Execute(ref BasicBallSharedData ballSharedData, ref PhysicsVelocity pv, in PoisonTag tag)
            {
                if (Update)
                {
                    if (NewPower >= 0) ballSharedData.Power += NewPower;
                    if (NewSpeed >= 0)
                    {
                        float3 direction = math.normalize(pv.Linear);
                        ballSharedData.Speed += NewSpeed;
                        pv.Linear = direction * ballSharedData.Speed * GlobalData.SpeedScale * GlobalData.GlobalSpeed * DeltaTime;
                    }
                    if (BigInteger.Compare(BigInteger.Parse(NewCost.ToString()), BigInteger.Zero) >= 0) 
                        ballSharedData.Cost = BigInteger.Add(BigInteger.Parse(ballSharedData.Cost.ToString()), 
                            BigInteger.Parse(NewCost.ToString())).ToString();
                    if (NewCount >= 0) ballSharedData.Count += NewCount;
                }
                else
                {
                    if (NewPower >= 0) ballSharedData.Power = NewPower;
                    if (NewSpeed >= 0)
                    {
                        float3 direction = math.normalize(pv.Linear);
                        ballSharedData.Speed = NewSpeed;
                        pv.Linear = direction * ballSharedData.Speed * GlobalData.SpeedScale * GlobalData.GlobalSpeed * DeltaTime;
                    }
                    if (BigInteger.Compare(BigInteger.Parse(NewCost.ToString()), BigInteger.Zero) >= 0)
                        ballSharedData.Cost = NewCost;
                    if (NewCount >= 0) ballSharedData.Count = NewCount;
                }
            }
        }
    }
}