using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
[BurstCompile]
public struct BasicBallSharedData : IComponentData
{
    public FixedString64Bytes BallName;
    public FixedString64Bytes BallDesc;
    public int Power;
    public int Speed;
    public FixedString64Bytes Cost;
    public int Count;
}
