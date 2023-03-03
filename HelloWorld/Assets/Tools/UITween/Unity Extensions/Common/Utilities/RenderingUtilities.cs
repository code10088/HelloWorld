using UnityEngine;

namespace UnityExtensions
{
    /// <summary>
    /// RenderingUtilities
    /// </summary>
    public struct RenderingUtilities
    {
        static Mesh _quadMesh;

        public static Mesh quadMesh
        {
            get
            {
                if (!_quadMesh)
                {
                    var vertices = new[]
                    {
                        new Vector3(-0.5f, -0.5f, 0f),
                        new Vector3(0.5f,  0.5f, 0f),
                        new Vector3(0.5f, -0.5f, 0f),
                        new Vector3(-0.5f,  0.5f, 0f)
                    };

                    var uvs = new[]
                    {
                        new Vector2(0f, 0f),
                        new Vector2(1f, 1f),
                        new Vector2(1f, 0f),
                        new Vector2(0f, 1f)
                    };

                    var indices = new[] { 0, 1, 2, 1, 0, 3 };

                    _quadMesh = new Mesh
                    {
                        vertices = vertices,
                        uv = uvs,
                        triangles = indices
                    };

                    _quadMesh.RecalculateNormals();
                    _quadMesh.RecalculateBounds();
                }

                return _quadMesh;
            }
        }

    } // struct RenderingUtilities

} // namespace UnityExtensions