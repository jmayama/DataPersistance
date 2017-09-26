



using UnityEngine;
using System;

public class MathUtils
{
    public static float CompareEpsilon = 0.00001f;

    public static float ExponentialEase(float easeSpeed, float start, float end, float dt)
    {
        float diff = end - start;

        diff *= Mathf.Clamp(dt * easeSpeed, 0.0f, 1.0f);

        return diff + start;
    }

    public static Vector3 ExponentialEase(float easeSpeed, Vector3 start, Vector3 end, float dt)
    {
        Vector3 diff = end - start;

        diff *= Mathf.Clamp(dt * easeSpeed, 0.0f, 1.0f);

        return diff + start;
    }

    public static float CalcRotationDegs(float x, float y)
    {
        return Mathf.Atan2(y, x) * Mathf.Rad2Deg;
    }

    public static bool AlmostEquals(float v1, float v2, float epsilon)
    {
        return Mathf.Abs(v2 - v1) <= epsilon;
    }

    public static bool AlmostEquals(float v1, float v2)
    {
        return AlmostEquals(v1, v2, CompareEpsilon);
    }

    public static Vector2 RandomUnitVector2()
    {
        float angleRadians = UnityEngine.Random.Range(0.0f, 2.0f * Mathf.PI);

        Vector2 unitVector = new Vector2(
            Mathf.Cos(angleRadians),
            Mathf.Sin(angleRadians)
            );

        return unitVector;
    }

    public static Vector3 ReflectIfAgainstNormal(Vector3 vec, Vector3 normal)
    {
        //If the move direction is going back into the wall reflect the movement away from the wall
        float amountAlongNormal = Vector3.Dot(vec, normal);

        //If this value is negative it means it's going in the opposite direction of the normal.  This means we
        //need to reflect it.
        if (amountAlongNormal < 0.0f)
        {
            //Calculate the projection onto the normal
            Vector3 directionAlongNormal = normal * amountAlongNormal / normal.sqrMagnitude;

            //Subtract the projection once to remove the movement into the wall, and another time to make it move
            //away from the wall the same amount.  (this adds up to subtracting twice the projection)
            vec -= directionAlongNormal * 2.0f;
        }

        return vec;
    }

    //This will get the closest point on a sphere. 
    public static Vector3 GetClosestPtOnSphere(
        Vector3 samplePt,
        Vector3 sphereCenter,
        float sphereRadius
        )
    {
        //Calculate the projection direction to the sample point
        Vector3 displacementDir = samplePt - sphereCenter;

        float distToSphere = displacementDir.magnitude;
        if (distToSphere > 0.0f)
        {
            displacementDir /= distToSphere;
        }
        else
        {
            displacementDir.Set(1.0f, 0.0f, 0.0f);
        }

        //The closest point on the capsule will be from the closest line segment point, in the direction
        //of the sample point, at a distance of the capsule radius.
        return sphereCenter + displacementDir * sphereRadius;
    }

    //This will get the closest point on a capsule. 
    public static Vector3 GetClosestPtOnCapsule(
        Vector3 samplePt, 
        Vector3 capsuleCenter,
        float capsuleHeight, 
        float capsuleRadius
        )
    {
        //Calculating the length of the line segment part of the capsule
        float lineSegmentLength = capsuleHeight - 2.0f * capsuleRadius;

        //if the linesegment lenght is less than or equal to zero just treat it like a sphere
        if (lineSegmentLength <= 0.0f)
        {
            return GetClosestPtOnSphere(samplePt, capsuleCenter, capsuleRadius);
        }

        //Calculate the line segment that goes along the capsules "Height"
        Vector3 lineSegPt1 = capsuleCenter;
        Vector3 lineSegPt2 = capsuleCenter;

        lineSegPt1.y += lineSegmentLength * 0.5f;
        lineSegPt2.y -= lineSegmentLength * 0.5f;

        Vector3 lineSegPtDiff = lineSegPt2 - lineSegPt1;

        //This formula will give the projected percent along the line segment.
        //If the number is between 0 and 1 the point is on the line segment, otherwise it will be
        //a point collinear to the line segment.  Because of this we need to clamp the value bettween 0
        //and 1
        float sampleProjectedT = Vector3.Dot(lineSegPtDiff, samplePt - lineSegPt1) / lineSegPtDiff.sqrMagnitude;
        sampleProjectedT = Mathf.Clamp01(sampleProjectedT);

        //Calculate the closest pt on the line segment to our sample point.  This is based on the 
        //projected percent we calculated above.
        Vector3 closestLineSegPt = lineSegPtDiff * sampleProjectedT + lineSegPt1;

        //Calculate the projection direction to the sample point
        Vector3 displacementDir = samplePt - closestLineSegPt;

        float sampleLineSegDist = displacementDir.magnitude;
        if (sampleLineSegDist > 0.0f)
        {
            displacementDir /= sampleLineSegDist;
        }
        else
        {
            displacementDir.Set(1.0f, 0.0f, 0.0f);
        }

        //The closest point on the capsule will be from the closest line segment point, in the direction
        //of the sample point, at a distance of the capsule radius.
        return closestLineSegPt + displacementDir * capsuleRadius;
    }


    public static float CalcSpringForce(
       float currentLength,
       float restLength,
       float minLength,
       float maxLength,
       Vector3 dir,
       Vector3 velocity,
       float maxForce,
       float damping
       )
    {
       float forceAmount = maxForce * (currentLength - restLength) / (maxLength - minLength);

       float dampingAmount = damping * Vector3.Dot(dir, velocity);

       return Mathf.Max(0.0f, forceAmount - dampingAmount);
    }
}
