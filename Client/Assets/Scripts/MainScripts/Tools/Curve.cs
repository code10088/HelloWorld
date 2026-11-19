using UnityEngine;

public enum CurveType
{
    None,
    Line,
    InSine,
    OutSine,
}
public class Curve
{
    private float min;
    private float max;
    private float time;
    private float timer;
    private CurveType curve;

    public Curve(float min, float max, float time, CurveType curve)
    {
        this.min = min;
        this.max = max;
        this.time = time;
        this.curve = curve;
    }
    public float Update(float t)
    {
        timer += t;
        float y = 0;
        float x = timer / time;
        switch (curve)
        {
            case CurveType.Line:
                y = x;
                break;
            case CurveType.InSine:
                y = 1 - Mathf.Cos(x * Mathf.PI / 2);
                break;
            case CurveType.OutSine:
                y = Mathf.Sin(x * Mathf.PI / 2);
                break;
        }
        return min + (max - min) * y;
    }
}
