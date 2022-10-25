using System;
using Unity.Burst;
using Unity.Entities;

namespace Tags
{
    [Serializable, BurstCompile, GenerateAuthoringComponent]
    public struct SniperTag : IComponentData
    {
    }
}