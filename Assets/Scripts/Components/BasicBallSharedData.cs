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
    public int Power;
    public int Speed;
    public int Cost;
    public int Count;
}
