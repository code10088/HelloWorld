using cfg;

namespace HotAssembly
{
    public static class ExternalTypeUtil
    {
        public static UnityEngine.Vector2 NewVector2(vec2 v)
        {
            return new UnityEngine.Vector2(v.X, v.Y);
        }
        public static UnityEngine.Vector2Int NewVector2Int(vec2int v)
        {
            return new UnityEngine.Vector2Int(v.X, v.Y);
        }

        public static UnityEngine.Vector3 NewVector3(vec3 v)
        {
            return new UnityEngine.Vector3(v.X, v.Y, v.Z);
        }
        public static UnityEngine.Vector3Int NewVector3Int(vec3int v)
        {
            return new UnityEngine.Vector3Int(v.X, v.Y, v.Z);
        }

        public static UnityEngine.Vector4 NewVector4(vec4 v)
        {
            return new UnityEngine.Vector4(v.X, v.Y, v.Z, v.W);
        }

        public static UnityEngine.Color NewColor(color32 c)
        {
            return new UnityEngine.Color(c.R / 255.0f, c.G / 255.0f, c.B / 255.0f, c.A / 255.0f);
        }
    }
}