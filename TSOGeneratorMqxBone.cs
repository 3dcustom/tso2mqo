using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tso2MqoGui
{
    public class TSOGeneratorMqxBone : TSOGenerator
    {
        public TSOGeneratorMqxBone(TSOGeneratorConfig config)
            : base(config)
        {
        }

        protected override bool DoLoadRefTSO(string path)
        {
            return true;
        }

        protected override bool DoGenerateMeshes()
        {
            meshes = new List<TSOMesh>();

            foreach (MqoObject obj in mqo.Objects)
            {
                if (obj.name.ToLower() == "bone")
                    continue;

                Console.WriteLine("object:" + obj.name);

                obj.CreateNormal();

                List<int> faces_1 = new List<int>();
                for (int i = 0, n = obj.faces.Count; i < n; ++i)
                    faces_1.Add(i);

                List<ushort> indices = new List<ushort>();

                    foreach (int f in faces_1)
                    {
                        MqoFace face = obj.faces[f];

                        Vertex va = new Vertex(obj.vertices[face.a], new Point2(face.ta.x, 1 - face.ta.y));
                        Vertex vb = new Vertex(obj.vertices[face.b], new Point2(face.tb.x, 1 - face.tb.y));
                        Vertex vc = new Vertex(obj.vertices[face.c], new Point2(face.tc.x, 1 - face.tc.y));

                        vert_indices.Add(vh.Add(va));
                        vert_indices.Add(vh.Add(vc));
                        vert_indices.Add(vh.Add(vb));
                    }

                    ushort[] optimized_indices = NvTriStrip.Optimize(vert_indices.ToArray());

                    TSOSubMesh sub = new TSOSubMesh();

                    sub.numvertices = optimized_indices.Length;
                    Vertex[] vertices = new Vertex[optimized_indices.Length];
                    for (int i = 0; i < optimized_indices.Length; ++i)
                    {
                        vertices[i] = vh.verts[optimized_indices[i]];
                    }
                    sub.vertices = vertices;

            }

            return true;
        }
    }
}
