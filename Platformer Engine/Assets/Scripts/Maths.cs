using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Maths
{

    public static int Sign(int value)
    {
        return value < 0 ? -1 : (value > 0 ? 1 : 0);
    }

    public static int Sign(float value)
    {
        return value < 0 ? -1 : (value > 0 ? 1 : 0);
    }

    public static int Sign(double value)
    {
        return value < 0 ? -1 : (value > 0 ? 1 : 0);
    }

    public static float Lerp(float val, float target, float step)
    {
        if (val < target)
            return Mathf.Min(val + step, target);
        else
            return Mathf.Max(val - step, target);

    }

    public static T Choose<T>(params T[] values)
    {
        int size = values.Length;
        return values[Random.Range(0, size)];
    }

    public static bool Chance(float chance)
    {
        return Random.Range(0f, 1f) < chance;
    }

    public static float SmoothLerp(float value, float start, float end, float CurrentTime, float LerpTime)
    {
        if (CurrentTime <= LerpTime)
        {
            float tl = CurrentTime / LerpTime;
            tl = tl * tl * tl * (tl * (6f * tl - 15f) + 10f);

            CurrentTime += Time.deltaTime;

            float tc = CurrentTime / LerpTime;
            tc = tc * tc * tc * (tc * (6f * tc - 15f) + 10f);

            value += (tc - tl) * (end - start);
        }

        value = Mathf.Clamp(value, start < end ? start : end, start < end ? end : start);

        return value;
    }

    public static bool floatEquals(float first, float second, float epsilon = 0.001f)
    {
        return (first < second + epsilon) && (first > second - epsilon);
    }
}

