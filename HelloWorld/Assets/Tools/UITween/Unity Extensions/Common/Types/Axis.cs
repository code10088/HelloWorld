
namespace UnityExtensions
{
    /// <summary>
    /// Axis
    /// Note: Use AxisUsageAttribute to custom inspector, use AxisUtilities in you scripts.
    /// </summary>
    [System.Flags]
    public enum Axis
    {
        None = 0,

        PositiveX = 1,      // 000 001
        PositiveY = 2,      // 000 010
        PositiveZ = 4,      // 000 100
        NegativeX = 8,      // 001 000
        NegativeY = 16,     // 010 000
        NegativeZ = 32,     // 100 000

        X = 9,
        Y = 18,
        Z = 36,

        XY = 27,
        YZ = 54,
        XZ = 45,

        All = 63
    }


    /// <summary>
    /// The relation between two axis
    /// </summary>
    public enum AxisRelation
    {
        Same,
        Vertical,
        Opposite,
    }

} // namespace UnityExtensions