using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tso2MqoGui
{
    public unsafe class TSOGeneratorRefBone : TSOGenerator
    {
        public TSOGeneratorRefBone(TSOGeneratorConfig config)
            : base(config)
        {
        }

        // 参照tso上の全頂点を保持する
        List<Vertex> refverts;
        // 最近傍探索
        PointCluster pc;

        void CreateRefVerts(TSOFile tso)
        {
            refverts = new List<Vertex>();

            foreach (TSOMesh i in tso.meshes)
                foreach (TSOSubMesh j in i.sub_meshes)
                    refverts.AddRange(j.vertices);
        }

        void CreatePointCluster()
        {
            pc = new PointCluster(refverts.Count);

            foreach (Vertex i in refverts)
                pc.Add(i.Pos);

            pc.Clustering();
        }

        protected override bool DoLoadRefTSO(string path)
        {
            tsoref = LoadTSO(path);
            tsoref.SwitchBoneIndicesOnMesh();
            CreateRefVerts(tsoref);
            CreatePointCluster();
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

                // 一番近い頂点への参照
                List<int> vref = new List<int>(obj.vertices.Count);

                foreach (UVertex i in obj.vertices)
                    vref.Add(pc.NearestIndex(i.Pos.x, i.Pos.y, i.Pos.z));

                obj.CreateNormal();

                List<int> faces_1 = new List<int>();
                List<int> faces_2 = new List<int>();
                VertexHeap<Vertex> vh = new VertexHeap<Vertex>();
                Vertex[] v = new Vertex[3];
                List<int> bone_indices = new List<int>(16);
                List<ushort> vert_indices = new List<ushort>();
                Dictionary<int, int> bone_indices_map = new Dictionary<int, int>();
                Dictionary<int, bool> adding_bone_indices = new Dictionary<int, bool>();
                List<TSOSubMesh> subs = new List<TSOSubMesh>();

                for (int i = 0, n = obj.faces.Count; i < n; ++i)
                    faces_1.Add(i);

                #region ボーンパーティション
                Console.WriteLine("  vertices bone_indices");
                Console.WriteLine("  -------- ------------");

                while (faces_1.Count != 0)
                {
                    int mtl = obj.faces[faces_1[0]].mtl;
                    bone_indices_map.Clear();
                    vert_indices.Clear();
                    vh.Clear();
                    bone_indices.Clear();

                    foreach (int f in faces_1)
                    {
                        MqoFace face = obj.faces[f];

                        if (face.mtl != mtl)
                        {
                            faces_2.Add(f);
                            continue;
                        }

                        v[0] = refverts[vref[face.a]];
                        v[1] = refverts[vref[face.b]];
                        v[2] = refverts[vref[face.c]];

                        adding_bone_indices.Clear();

                        for (int k = 0; k < 3; ++k)
                        {
                            UInt32 idx0 = v[k].Idx;
                            Point4 wgt0 = v[k].Wgt;
                            byte* idx = (byte*)(&idx0);
                            float* wgt = (float*)(&wgt0);

                            for (int l = 0; l < 4; ++l)
                            {
                                if (wgt[l] <= float.Epsilon)
                                    continue;
                                if (bone_indices_map.ContainsKey(idx[l]))
                                    continue;

                                adding_bone_indices[idx[l]] = true;
                            }
                        }

                        if (bone_indices_map.Count + adding_bone_indices.Count > 16)
                        {
                            faces_2.Add(f);
                            continue;
                        }

                        foreach (int i in adding_bone_indices.Keys)
                        {
                            bone_indices_map.Add(i, bone_indices_map.Count);
                            bone_indices.Add(i);
                        }

                        for (int k = 0; k < 3; ++k)
                        {
                            UInt32 idx0 = v[k].Idx;
                            Point4 wgt0 = v[k].Wgt;
                            byte* idx = (byte*)(&idx0);
                            float* wgt = (float*)(&wgt0);

                            for (int l = 0; l < 4; ++l)
                                if (wgt[l] > float.Epsilon)
                                    idx[l] = (byte)bone_indices_map[idx[l]];

                            //v[k]は値型なのでrefvertsに影響しない。
                            v[k].Idx = idx0;
                        }

                        Vertex va = new Vertex(obj.vertices[face.a].Pos, v[0].Wgt, v[0].Idx, obj.vertices[face.a].Nrm, new Point2(face.ta.x, 1 - face.ta.y));
                        Vertex vb = new Vertex(obj.vertices[face.b].Pos, v[1].Wgt, v[1].Idx, obj.vertices[face.b].Nrm, new Point2(face.tb.x, 1 - face.tb.y));
                        Vertex vc = new Vertex(obj.vertices[face.c].Pos, v[2].Wgt, v[2].Idx, obj.vertices[face.c].Nrm, new Point2(face.tc.x, 1 - face.tc.y));

                        vert_indices.Add(vh.Add(va));
                        vert_indices.Add(vh.Add(vc));
                        vert_indices.Add(vh.Add(vb));
                    }

                    ushort[] optimized_indices = NvTriStrip.Optimize(vert_indices.ToArray());

                    TSOSubMesh sub = new TSOSubMesh();
                    sub.spec = mtl;
                    sub.numbones = bone_indices.Count;
                    sub.bones = bone_indices.ToArray();

                    sub.numvertices = optimized_indices.Length;
                    Vertex[] vertices = new Vertex[optimized_indices.Length];
                    for (int i = 0; i < optimized_indices.Length; ++i)
                    {
                        vertices[i] = vh.verts[optimized_indices[i]];
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

        protected override bool DoCleanup()
        {
            pc = null;
            refverts = null;
            return base.DoCleanup();
        }
    }
}
