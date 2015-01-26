using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tso2MqoGui
{
    public unsafe class TSOGeneratorMqxBone : TSOGenerator
    {
        public TSOGeneratorMqxBone(TSOGeneratorConfig config)
            : base(config)
        {
        }

        protected override bool DoLoadRefTSO(string path)
        {
            tsoref = LoadTSO(path);

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

                int object_id = obj.id;

                obj.CreateNormal();

                List<int> faces_1 = new List<int>();
                List<int> faces_2 = new List<int>();
                Heap<int> bh = new Heap<int>();
                Heap<Vertex> vh = new Heap<Vertex>();
                Vertex[] refvs = new Vertex[3];
                List<ushort> vert_indices = new List<ushort>();
                Dictionary<int, bool> adding_bone_indices = new Dictionary<int, bool>();
                List<TSOSubMesh> subs = new List<TSOSubMesh>();

                for (int i = 0, n = obj.faces.Count; i < n; ++i)
                    faces_1.Add(i);

                #region ボーンパーティション
                Console.WriteLine("  vertices bone_indices");
                Console.WriteLine("  -------- ------------");

                while (faces_1.Count != 0)
                {
                    int spec = obj.faces[faces_1[0]].spec;
                    bh.Clear();
                    vh.Clear();
                    vert_indices.Clear();

                    foreach (int f in faces_1)
                    {
                        MqoFace face = obj.faces[f];

                        if (face.spec != spec)
                        {
                            faces_2.Add(f);
                            continue;
                        }

                        for (int k = 0; k < 3; ++k)
                        {
                            refvs[k] = new Vertex();
                        }

                        adding_bone_indices.Clear();

                        for (int k = 0; k < 3; ++k)
                        {
                            UInt32 idx0 = refvs[k].Idx;
                            Point4 wgt0 = refvs[k].Wgt;
                            byte* idx = (byte*)(&idx0);
                            float* wgt = (float*)(&wgt0);

                            int vertex_id = obj.vertices[face.vert_indices[k]].id;
                            mqx.UpdateWeits(object_id, vertex_id);
                            for (int l = 0; l < 4; ++l)
                            {
                                idx[l] = (byte)(mqx.weits[l].bone_id-1);
                                wgt[l] = mqx.weits[l].weit;
                            }
                            refvs[k].Idx = idx0;
                            refvs[k].Wgt = wgt0;

                            for (int l = 0; l < 4; ++l)
                            {
                                if (wgt[l] <= float.Epsilon)
                                    continue;
                                if (bh.map.ContainsKey(idx[l]))
                                    continue;

                                adding_bone_indices[idx[l]] = true;
                            }
                        }

                        if (bh.Count + adding_bone_indices.Count > 16)
                        {
                            faces_2.Add(f);
                            continue;
                        }

                        foreach (int i in adding_bone_indices.Keys)
                        {
                            bh.Add(i);
                        }

                        for (int k = 0; k < 3; ++k)
                        {
                            UInt32 idx0 = refvs[k].Idx;
                            Point4 wgt0 = refvs[k].Wgt;
                            byte* idx = (byte*)(&idx0);
                            float* wgt = (float*)(&wgt0);

                            for (int l = 0; l < 4; ++l)
                            {
                                if (wgt[l] <= float.Epsilon)
                                    continue;

                                idx[l] = (byte)bh[idx[l]];
                            }

                            refvs[k].Idx = idx0;
                        }

                        Vertex va = new Vertex(obj.vertices[face.a].Pos, refvs[0].Wgt, refvs[0].Idx, obj.vertices[face.a].Nrm, new Point2(face.ta.x, 1 - face.ta.y));
                        Vertex vb = new Vertex(obj.vertices[face.b].Pos, refvs[1].Wgt, refvs[1].Idx, obj.vertices[face.b].Nrm, new Point2(face.tb.x, 1 - face.tb.y));
                        Vertex vc = new Vertex(obj.vertices[face.c].Pos, refvs[2].Wgt, refvs[2].Idx, obj.vertices[face.c].Nrm, new Point2(face.tc.x, 1 - face.tc.y));

                        vert_indices.Add(vh.Add(va));
                        vert_indices.Add(vh.Add(vc));
                        vert_indices.Add(vh.Add(vb));
                    }

                    ushort[] optimized_indices = NvTriStrip.Optimize(vert_indices.ToArray());

                    TSOSubMesh sub = new TSOSubMesh();
                    sub.spec = spec;
                    sub.numbones = bh.Count;
                    sub.bones = bh.ary.ToArray();

                    sub.numvertices = optimized_indices.Length;
                    Vertex[] vertices = new Vertex[optimized_indices.Length];
                    for (int i = 0; i < optimized_indices.Length; ++i)
                    {
                        vertices[i] = vh.ary[optimized_indices[i]];
                    }
                    sub.vertices = vertices;

                    Console.WriteLine("  {0,8} {1,12}", sub.vertices.Length, sub.bones.Length);

                    subs.Add(sub);

                    List<int> faces_tmp = faces_1;
                    faces_1 = faces_2;
                    faces_2 = faces_tmp;
                    faces_tmp.Clear();
                }
                #endregion
                TSOMesh mesh = new TSOMesh();
                mesh.name = obj.name;
                mesh.numsubs = subs.Count;
                mesh.sub_meshes = subs.ToArray();
                mesh.matrix = Matrix44.Identity;
                mesh.effect = 0;
                meshes.Add(mesh);
            }

            return true;
        }
    }
}
