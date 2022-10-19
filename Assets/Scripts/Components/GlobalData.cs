using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile, GenerateAuthoringComponent, Serializable]
public struct GlobalData : IComponentData
{
    public int CurrentLevel;
    public int CashBonus;
    public int SpeedScale;
    public int GlobalSpeed;
    public int PowerMultiplier;
    public int Bricks;
    public int Money;
    public int MaxBalls;
    public int ClickX;
}
