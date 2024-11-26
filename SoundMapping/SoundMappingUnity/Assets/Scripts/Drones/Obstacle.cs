using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class Obstacle
{
    public abstract Vector3 ClosestPoint(Vector3 point);
}

public class SphereObstacle : Obstacle
{
    public Vector3 Center;
    public float Radius;

    public SphereObstacle(Vector3 center, float radius)
    {
        Center = center;
        Radius = radius;
    }

    public override Vector3 ClosestPoint(Vector3 point)
    {
        Vector3 direction = point - Center;
        if (direction.sqrMagnitude <= Radius * Radius)
        {
            // Point is inside the sphere
            return point;
        }
        else
        {
            // Point is outside the sphere
            return Center + direction.normalized * Radius;
        }
    }
}
public class BoxObstacle : Obstacle
{
    public Vector3 Center;
    public Vector3 Size;
    public Quaternion Rotation;

    public BoxObstacle(Vector3 center, Vector3 size, Quaternion rotation)
    {
        Center = center;
        Size = size;
        Rotation = rotation;
    }

    public override Vector3 ClosestPoint(Vector3 point)
    {
        // Transform point into the box's local space
        Vector3 localPoint = Quaternion.Inverse(Rotation) * (point - Center);

        // Compute closest point in the axis-aligned bounding box
        Vector3 halfSize = Size * 0.5f;
        Vector3 clampedPoint = new Vector3(
            Mathf.Clamp(localPoint.x, -halfSize.x, halfSize.x),
            Mathf.Clamp(localPoint.y, -halfSize.y, halfSize.y),
            Mathf.Clamp(localPoint.z, -halfSize.z, halfSize.z)
        );

        // Transform back to world space
        return Center + Rotation * clampedPoint;
    }
}
public class CylinderObstacle : Obstacle
{
    public Vector3 Center;
    public float Radius;
    public float Height;
    public Quaternion Rotation;

    public CylinderObstacle(Vector3 center, float radius, float height, Quaternion rotation)
    {
        Center = center;
        Radius = radius/2;
        Height = 2*height;
        Rotation = rotation;
    }

    public override Vector3 ClosestPoint(Vector3 point)
    {
        // Transform the point into the cylinder's local space
        Quaternion inverseRotation = Quaternion.Inverse(Rotation);
        Vector3 localPoint = inverseRotation * (point - Center);

        // Cylinder's axis is along the local Y-axis
        float halfHeight = Height * 0.5f;

        // Clamp the local point's Y coordinate to the cylinder's height
        float clampedY = Mathf.Clamp(localPoint.y, -halfHeight, halfHeight);

        // Compute the distance from the point to the cylinder's axis in the XZ plane
        Vector2 localPointXZ = new Vector2(localPoint.x, localPoint.z);
        float distanceXZ = localPointXZ.magnitude;

        Vector3 closestLocalPoint;

        if (distanceXZ <= Radius)
        {
            if (localPoint.y >= -halfHeight && localPoint.y <= halfHeight)
            {
                // The point is inside the cylinder
                closestLocalPoint = localPoint;
            }
            else
            {
                // The point is inside the side projection but outside along Y
                closestLocalPoint = new Vector3(localPoint.x, clampedY, localPoint.z);
            }
        }
        else
        {
            // Point is outside the cylinder's side projection
            Vector2 projectedPointXZ = localPointXZ.normalized * Radius;
            closestLocalPoint = new Vector3(projectedPointXZ.x, clampedY, projectedPointXZ.y);
        }

        // Transform back to world space
        return Center + Rotation * closestLocalPoint;
    }
}



public static class ClosestPointCalculator
{
    public static List<Obstacle> obstacles;

    public static Vector3 ClosestPoint(Vector3 point)
    {
        Vector3 closestPoint = point;
        float minSqrDistance = float.MaxValue;
        string name = "";

        foreach (var obstacle in obstacles)
        {
            Vector3 cp = obstacle.ClosestPoint(point);
            float sqrDist = (cp - point).sqrMagnitude;
            if (sqrDist < minSqrDistance)
            {
                minSqrDistance = sqrDist;
                closestPoint = cp;
                name = obstacle.GetType().Name;
            }
        }

        Debug.Log($"Closest point to {point} is {closestPoint} on {name}");
        return closestPoint;
    }

    public static List<Vector3> ClosestPointsWithinRadius(Vector3 refPoint, float radius)
    {
        List<Vector3> closestPoints = new List<Vector3>();

        foreach (var obstacle in obstacles)
        {
            Vector3 cp = obstacle.ClosestPoint(refPoint);
            if ((cp - refPoint).sqrMagnitude <= radius * radius)
            {
                closestPoints.Add(cp);
            }
        }

        return closestPoints;
    }
}
