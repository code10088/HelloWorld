
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Luban;


namespace cfg
{
public partial struct vec3
{
    public vec3(ByteBuf _buf) 
    {
        X = _buf.ReadFloat();
        Y = _buf.ReadFloat();
        Z = _buf.ReadFloat();
    }

    public static vec3 Deserializevec3(ByteBuf _buf)
    {
        return new vec3(_buf);
    }

    public readonly float X;
    public readonly float Y;
    public readonly float Z;
   

    public override string ToString()
    {
        return "{ "
        + "x:" + X + ","
        + "y:" + Y + ","
        + "z:" + Z + ","
        + "}";
    }
}
}

