using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
[GenerateAuthoringComponent]
public struct DontDestroyTag : IComponentData
{
}