using System;
using Unity.Burst;
using Unity.Entities;

namespace Tags
{
    [Serializable, GenerateAuthoringComponent, BurstCompile]
    public struct PlasmaTag : IComponentData
    {
    }
}