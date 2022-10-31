using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable, BurstCompile, GenerateAuthoringComponent]
public struct ScatterBallSharedData : IComponentData
{
    public int ExtraBalls;
}
