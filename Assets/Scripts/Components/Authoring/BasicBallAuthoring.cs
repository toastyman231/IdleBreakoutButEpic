using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public class BasicBallAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public string BallNameAuth;
    public string BallDescAuth;
    public int PowerAuth;
    public int SpeedAuth;
    public string CostAuth;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new BasicBallSharedData
        {
            BallName = BallNameAuth,
            BallDesc = BallDescAuth,
            Power = PowerAuth,
            Speed = SpeedAuth,
            Cost = CostAuth,
            Count = 0
        });
    }
}
