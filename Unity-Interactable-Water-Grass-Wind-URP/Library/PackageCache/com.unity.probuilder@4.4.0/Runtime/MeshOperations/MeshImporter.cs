using UnityEngine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace UnityEngine.ProBuilder.MeshOperations
{
    /// <summary>
    /// A collection of settings used when importing models to the ProBuilderMesh component.
    /// </summary>
    [Serializable]
    public sealed class MeshImportSettings
    {
        [SerializeField]
        bool m_Quads = true;

        [SerializeField]
        bool m_Smoothing = true;

        [SerializeField]
        float m_SmoothingThreshold = 1f;

        /// <value>
        /// Try to quadrangilize triangle meshes.
        /// </value>
        public bool quads
        {
            get { return m_Quads; }
            set { m_Quads = value; }
        }

        // Allow ngons when importing meshes. @todo
        // public bool ngons = false;

        /// <value>
        /// Generate smoothing groups based on mesh normals.
        /// </value>
        public bool smoothing
        {
            get { return m_Smoothing; }
            set { m_Smoothing = value; }
        }

        /// <value>
        /// Degree of difference between face normals to allow when determining smoothing groups.
        /// </value>
        public float smoothingAngle
        {
            get { return m_SmoothingThreshold; }
            set { m_SmoothingThreshold = value; }
        }

        public override string ToString()
        {
            return string.Format("quads: {0}\nsmoothing: {1}\nthreshold: {2}",
                quads,
                smoothing,
                smoothingAngle);
        }
    }

    /// <summary>
    /// Responsible for importing UnityEngine.Mesh data to a ProBuilderMesh component.
    /// </summary>
    public sealed class MeshImporter
    {
        static readonly MeshImportSettings k_DefaultImportSettings = new MeshImportSettings()
        {
            quads = true,
            smoothing = true,
            smoothingAngle = 1f
        };

        Mesh m_SourceMesh;
        Material[] m_SourceMaterials;
        ProBuilderMesh m_Destination;
        Vertex[] m_Vertices;

        public MeshImporter(GameObject gameObject)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            m_SourceMesh = meshFilter.sharedMesh;
            if(m_SourceMesh == null)
                throw new ArgumentNullException("gameObject", "GameObject does not contain a valid MeshFilter.sharedMesh.");
            m_Destination = gameObject.DemandComponent<ProBuilderMesh>();
            m_SourceMaterials = gameObject.GetComponent<MeshRenderer>()?.sharedMaterials;
        }

        /// <summary>
        /// Create a new ProBuilderMesh importer instance.
        /// </summary>
        /// <param name="sourceMesh">The Mesh asset to import vertex data from.</param>
        /// <param name="sourceMaterials">The materials to assign to the ProBuilderMesh renderer.</param>
        /// <param name="destination">The ProBuilderMesh asset to write vertex data to.</param>
        public MeshImporter(Mesh sourceMesh, Material[] sourceMaterials, ProBuilderMesh destination)
        {
            if(sourceMesh == null)
                throw new ArgumentNullException("sourceMesh");
            if(destination == null)
                throw new ArgumentNullException("destination");
            m_SourceMesh = sourceMesh;
            m_SourceMaterials = sourceMaterials;
            m_Destination = destination;
        }

        [Obsolete, EditorBrowsable(EditorBrowsableState.Never)]
        public MeshImporter(ProBuilderMesh destination)
        {
            m_Destination = destination;
        }

        [Obsolete, EditorBrowsable(EditorBrowsableState.Never)]
        public bool Import(GameObject go, MeshImportSettings importSettings = null)
        {
            try
            {
                m_SourceMesh = go.GetComponent<MeshFilter>().sharedMesh;
                m_SourceMaterials = go.GetComponent<MeshRenderer>()?.sharedMaterials;
                Import(importSettings);
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                return false;
            }

            return true;
        }

        /// <summary>
        /// Import mesh data from a GameObject's MeshFilter.sharedMesh and MeshRenderer.sharedMaterials.
        /// </summary>
        /// <param name="originalMesh">The UnityEngine.Mesh to extract attributes from.</param>
        /// <param name="materials">The materials array corresponding to the originalMesh submeshes.</param>
        /// <param name="importSettings">Optional settings parameter defines import customization properties.</param>
        /// <exception cref="NotSupportedException">Import only supports triangle and quad mesh topologies.</exception>
        public void Import(MeshImportSettings importSettings = null)
        {
            if (importSettings == null)
                importSettings = k_DefaultImportSettings;

            // When importing the mesh is always split into triangles with no vertices shared
            // between faces. In a later step co-incident vertices are collapsed (eg, before
            // leaving the Import function).
            Vertex[] sourceVertices = m_SourceMesh.GetVertices();
            List<Vertex> splitVertices = new List<Vertex>();
            List<Face> faces = new List<Face>();

            // Fill in Faces array with just the position indexes. In the next step we'll
            // figure out smoothing groups & merging
            int vertexIndex = 0;
            int materialCount = m_SourceMaterials != null ? m_SourceMaterials.Length : 0;

            for (int submeshIndex = 0; submeshIndex < m_SourceMesh.subMeshCount; submeshIndex++)
            {
                switch (m_SourceMesh.GetTopology(submeshIndex))
                {
                    case MeshTopology.Triangles:
                    {
                        int[] indexes = m_SourceMesh.GetIndices(submeshIndex);

                        for (int tri = 0; tri < indexes.Length; tri += 3)
                        {
                            faces.Add(new Face(
                                    new int[] { vertexIndex, vertexIndex + 1, vertexIndex + 2 },
                                    Math.Clamp(submeshIndex, 0, materialCount - 1),
                                    AutoUnwrapSettings.tile,
                                    Smoothing.smoothingGroupNone,
                                    -1,
                                    -1,
                                    true));

                            splitVertices.Add(sourceVertices[indexes[tri]]);
                            splitVertices.Add(sourceVertices[indexes[tri + 1]]);
                            splitVertices.Add(sourceVertices[indexes[tri + 2]]);

                            vertexIndex += 3;
                        }
                    }
                    break;

                    case MeshTopology.Quads:
                    {
                        int[] indexes = m_SourceMesh.GetIndices(submeshIndex);

                        for (int quad = 0; quad < indexes.Length; quad += 4)
                        {
                            faces.Add(new Face(new int[]
                                {
                                    vertexIndex, vertexIndex + 1, vertexIndex + 2,
                                    vertexIndex + 2, vertexIndex + 3, vertexIndex + 0
                                },
                                Math.Clamp(submeshIndex, 0, materialCount - 1),
                                AutoUnwrapSettings.tile,
                                Smoothing.smoothingGroupNone,
                                -1,
                                -1,
                                true));

                            splitVertices.Add(sourceVertices[indexes[quad]]);
                            splitVertices.Add(sourceVertices[indexes[quad + 1]]);
                            splitVertices.Add(sourceVertices[indexes[quad + 2]]);
                            splitVertices.Add(sourceVertices[indexes[quad + 3]]);

                            vertexIndex += 4;
                        }
                    }
                    break;

                    default:
                        throw new NotSupportedException("ProBuilder only supports importing triangle and quad meshes.");
                }
            }

            m_Vertices = splitVertices.ToArray();

            m_Destination.Clear();
            m_Destination.SetVertices(m_Vertices);
            m_Destination.faces = faces;
            m_Destination.sharedVertices = SharedVertex.GetSharedVerticesWithPositions(m_Destination.positionsInternal);
            m_Destination.sharedTextures = new SharedVertex[0];

            HashSet<Face> processed = new HashSet<Face>();

            if (importSettings.quads)
            {
                List<WingedEdge> wings = WingedEdge.GetWingedEdges(m_Destination, m_Destination.facesInternal, true);

                // build a lookup of the strength of edge connections between triangle faces
                Dictionary<EdgeLookup, float> connections = new Dictionary<EdgeLookup, float>();

                for (int i = 0; i < wings.Count; i++)
                {
                    using (var it = new WingedEdgeEnumerator(wings[i]))
                    {
                        while (it.MoveNext())
                        {
                            var border = it.Current;

                            if (border.opposite != null && !connections.ContainsKey(border.edge))
                            {
                                float score = GetQuadScore(border, border.opposite);
                                connections.Add(border.edge, score);
                            }
                        }
                    }
                }

                List<SimpleTuple<Face, Face>> quads = new List<SimpleTuple<Face, Face>>();

                // move through each face and find it's best quad neighbor
                foreach (WingedEdge face in wings)
                {
                    if (!processed.Add(face.face))
                        continue;

                    float bestScore = 0f;
                    Face buddy = null;

                    using (var it = new WingedEdgeEnumerator(face))
                    {
                        while (it.MoveNext())
                        {
                            var border = it.Current;

                            if (border.opposite != null && processed.Contains(border.opposite.face))
                                continue;

                            float borderScore;

                            // only add it if the opposite face's best score is also this face
                            if (connections.TryGetValue(border.edge, out borderScore) &&
                                borderScore > bestScore &&
                                face.face == GetBestQuadConnection(border.opposite, connections))
                            {
                                bestScore = borderScore;
                                buddy = border.opposite.face;
                            }
                        }
                    }

                    if (buddy != null)
                    {
                        processed.Add(buddy);
                        quads.Add(new SimpleTuple<Face, Face>(face.face, buddy));
                    }
                }

                // don't collapse coincident vertices if smoothing is enabled, we need the original normals intact
                MergeElements.MergePairs(m_Destination, quads, !importSettings.smoothing);
            }

            if (importSettings.smoothing)
            {
                Smoothing.ApplySmoothingGroups(m_Destination, m_Destination.facesInternal, importSettings.smoothingAngle, m_Vertices.Select(x => x.normal).ToArray());
                // After smoothing has been applied go back and weld coincident vertices created by MergePairs.
                MergeElements.CollapseCoincidentVertices(m_Destination, m_Destination.facesInternal);
            }
        }

        static Face GetBestQuadConnection(WingedEdge wing, Dictionary<EdgeLookup, float> connections)
        {
            float score = 0f;
            Face face = null;

            using (var it = new WingedEdgeEnumerator(wing))
            {
                while (it.MoveNext())
                {
                    var border = it.Current;

                    float s = 0f;

                    if (connections.TryGetValue(border.edge, out s) && s > score)
                    {
                        score = connections[border.edge];
                        face = border.opposite.face;
                    }
                }
            }

            return face;
        }

        /**
         * Get a weighted value for the quality of a quad composed of two triangles. 0 is terrible, 1 is perfect.
         * normalThreshold will discard any quads where the dot product of their normals is less than the threshold.
         * @todo Abstract the quad detection to a separate class so it can be applied to pb_Objects.
         */
        float GetQuadScore(WingedEdge left, WingedEdge right, float normalThreshold = .9f)
        {
            int[] quad = WingedEdge.MakeQuad(left, right);

            if (quad == null)
                return 0f;

            // first check normals
            Vector3 leftNormal = Math.Normal(m_Vertices[quad[0]].position, m_Vertices[quad[1]].position, m_Vertices[quad[2]].position);
            Vector3 rightNormal = Math.Normal(m_Vertices[quad[2]].position, m_Vertices[quad[3]].position, m_Vertices[quad[0]].position);

            float score = Vector3.Dot(leftNormal, rightNormal);

            if (score < normalThreshold)
                return 0f;

            // next is right-angle-ness check
            Vector3 a = (m_Vertices[quad[1]].position - m_Vertices[quad[0]].position);
            Vector3 b = (m_Vertices[quad[2]].position - m_Vertices[quad[1]].position);
            Vector3 c = (m_Vertices[quad[3]].position - m_Vertices[quad[2]].position);
            Vector3 d = (m_Vertices[quad[0]].position - m_Vertices[quad[3]].position);

            a.Normalize();
            b.Normalize();
            c.Normalize();
            d.Normalize();

            float da = Mathf.Abs(Vector3.Dot(a, b));
            float db = Mathf.Abs(Vector3.Dot(b, c));
            float dc = Mathf.Abs(Vector3.Dot(c, d));
            float dd = Mathf.Abs(Vector3.Dot(d, a));

            score += 1f - ((da + db + dc + dd) * .25f);

            // and how close to parallel the opposite sides area
            score += Mathf.Abs(Vector3.Dot(a, c)) * .5f;
            score += Mathf.Abs(Vector3.Dot(b, d)) * .5f;

            // the three tests each contribute 1
            return score * .33f;
        }
    }
}
