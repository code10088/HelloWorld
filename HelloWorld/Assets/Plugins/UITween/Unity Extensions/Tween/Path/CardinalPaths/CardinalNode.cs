using System;
using UnityEngine;

namespace UnityExtensions.Paths
{
    /// <summary>
    /// Cardinal 路径节点
    /// </summary>
    [Serializable]
    public class CardinalNode : Path.Node, ICopyable<CardinalNode>
    {
        public Vector3 position;
        public float tension = 0.5f;


        public void Copy(CardinalNode target)
        {
            base.Copy(target);
            position = target.position;
            tension = target.tension;
        }

    } // class CardinalNode

} // namespace UnityExtensions.Paths