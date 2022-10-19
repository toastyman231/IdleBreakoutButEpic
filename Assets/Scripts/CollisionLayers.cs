using System;

[Flags]
public enum CollisionLayers
{
    Ball = 1 << 0,
    Wall = 1 << 1,
    Brick = 1 << 2,
    Click = 1 << 3
}
