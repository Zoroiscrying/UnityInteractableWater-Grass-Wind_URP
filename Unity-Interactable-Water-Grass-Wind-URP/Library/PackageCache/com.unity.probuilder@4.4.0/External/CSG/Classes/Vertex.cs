using System;
using UnityEngine;

namespace UnityEngine.ProBuilder.Csg
{
    /// <summary>
    /// Holds information about a single vertex, and provides methods for averaging between many.
    /// <remarks>All values are optional. Where not present a default value will be substituted if necessary.</remarks>
    /// </summary>
    struct Vertex
    {
        Vector3 m_Position;
        Color m_Color;
        Vector3 m_Normal;
        Vector4 m_Tangent;
        Vector2 m_UV0;
        Vector2 m_UV2;
        Vector4 m_UV3;
        Vector4 m_UV4;
        VertexAttributes m_Attributes;

        /// <value>
        /// The position in model space.
        /// </value>
        public Vector3 position
        {
            get { return m_Position; }
            set
            {
                hasPosition = true;
                m_Position = value;
            }
        }

        /// <value>
        /// Vertex color.
        /// </value>
        public Color color
        {
            get { return m_Color; }
            set
            {
                hasColor = true;
                m_Color = value;
            }
        }

        /// <value>
        /// Unit vector normal.
        /// </value>
        public Vector3 normal
        {
            get { return m_Normal; }
            set
            {
                hasNormal = true;
                m_Normal = value;
            }
        }

        /// <value>
        /// Vertex tangent (sometimes called binormal).
        /// </value>
        public Vector4 tangent
        {
            get { return m_Tangent; }
            set
            {
                hasTangent = true;
                m_Tangent = value;
            }
        }

        /// <value>
        /// UV 0 channel. Also called textures.
        /// </value>
        public Vector2 uv0
        {
            get { return m_UV0; }
            set
            {
                hasUV0 = true;
                m_UV0 = value;
            }
        }

        /// <value>
        /// UV 2 channel.
        /// </value>
        public Vector2 uv2
        {
            get { return m_UV2; }
            set
            {
                hasUV2 = true;
                m_UV2 = value;
            }
        }

        /// <value>
        /// UV 3 channel.
        /// </value>
        public Vector4 uv3
        {
            get { return m_UV3; }
            set
            {
                hasUV3 = true;
                m_UV3 = value;
            }
        }

        /// <value>
        /// UV 4 channel.
        /// </value>
        public Vector4 uv4
        {
            get { return m_UV4; }
            set
            {
                hasUV4 = true;
                m_UV4 = value;
            }
        }

        /// <summary>
        /// Find if a vertex attribute has been set.
        /// </summary>
        /// <param name="attribute">The attribute or attributes to test for.</param>
        /// <returns>True if this vertex has the specified attributes set, false if they are default values.</returns>
        public bool HasArrays(VertexAttributes attribute)
        {
            return (m_Attributes & attribute) == attribute;
        }

        public bool hasPosition
        {
            get { return (m_Attributes & VertexAttributes.Position) == VertexAttributes.Position; }
            private set { m_Attributes = value ? (m_Attributes | VertexAttributes.Position) : (m_Attributes & ~(VertexAttributes.Position)); }
        }

        public bool hasColor
        {
            get { return (m_Attributes & VertexAttributes.Color) == VertexAttributes.Color; }
            private set { m_Attributes = value ? (m_Attributes | VertexAttributes.Color) : (m_Attributes & ~(VertexAttributes.Color)); }
        }

        public bool hasNormal
        {
            get { return (m_Attributes & VertexAttributes.Normal) == VertexAttributes.Normal; }
            private set { m_Attributes = value ? (m_Attributes | VertexAttributes.Normal) : (m_Attributes & ~(VertexAttributes.Normal)); }
        }

        public bool hasTangent
        {
            get { return (m_Attributes & VertexAttributes.Tangent) == VertexAttributes.Tangent; }
            private set { m_Attributes = value ? (m_Attributes | VertexAttributes.Tangent) : (m_Attributes & ~(VertexAttributes.Tangent)); }
        }

        public bool hasUV0
        {
            get { return (m_Attributes & VertexAttributes.Texture0) == VertexAttributes.Texture0; }
            private set { m_Attributes = value ? (m_Attributes | VertexAttributes.Texture0) : (m_Attributes & ~(VertexAttributes.Texture0)); }
        }

        public bool hasUV2
        {
            get { return (m_Attributes & VertexAttributes.Texture1) == VertexAttributes.Texture1; }
            private set { m_Attributes = value ? (m_Attributes | VertexAttributes.Texture1) : (m_Attributes & ~(VertexAttributes.Texture1)); }
        }

        public bool hasUV3
        {
            get { return (m_Attributes & VertexAttributes.Texture2) == VertexAttributes.Texture2; }
            private set { m_Attributes = value ? (m_Attributes | VertexAttributes.Texture2) : (m_Attributes & ~(VertexAttributes.Texture2)); }
        }

        public bool hasUV4
        {
            get { return (m_Attributes & VertexAttributes.Texture3) == VertexAttributes.Texture3; }
            private set { m_Attributes = value ? (m_Attributes | VertexAttributes.Texture3) : (m_Attributes & ~(VertexAttributes.Texture3)); }
        }

        public void Flip()
        {
            if(hasNormal)
                m_Normal *= -1f;

            if (hasTangent)
                m_Tangent *= -1f;
        }
    }
}
