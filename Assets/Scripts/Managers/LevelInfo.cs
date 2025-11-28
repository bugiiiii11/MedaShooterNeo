using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelInfo : Singleton<LevelInfo>
{
    public Transform BoundLowerLeft, BoundUpperRight, BoundBottom, BoundTop;
    public List<Transform> SpawnPositions;
    public Vector3 StageDimensions;

    public float LeftDespawnCoordinateX;

    public override void Awake()
    {
        base.Awake();
        StageDimensions = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, 0, 0));
    }

    public Vector3 GetRandomPointInLevel()
    {
        var randomLLUR = Vector2.Lerp(BoundLowerLeft.position, BoundUpperRight.position, Random.value);

        return randomLLUR;
    }

    public Vector2 GetRandomPointToLeftSide(Vector2 from)
    {
/*        var value = Vector2.Lerp(BoundLowerLeft.position, from, Random.value);
        return value;*/
        var randomX = BoundLowerLeft.position.x;
        var randomY = Random.Range(BoundLowerLeft.position.y, from.y);
        var randomY2 = Random.Range(BoundUpperRight.position.y, from.y); 

        return new Vector2(randomX, Random.Range(randomY, randomY2));
    }

    public bool IsInVerticalBoundaries(Vector3 pos)
    {
        return IsUnderTopBoundary(pos) && IsAboveBottomBoundary(pos);
    }

    public bool IsUnderTopBoundary(Vector3 pos)
    {
        return pos.y < BoundTop.position.y;
    }

    public bool IsAboveBottomBoundary(Vector3 pos)
    {
        return pos.y > BoundBottom.position.y;
    }
}
