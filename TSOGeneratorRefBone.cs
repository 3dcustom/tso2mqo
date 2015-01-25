using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tso2MqoGui
{
    public unsafe class TSOGeneratorRefBone : TSOGenerator
    {
        private List<Vertex> vlst;
        private PointCluster pc;

        public TSOGeneratorRefBone(TSOGeneratorConfig config)
            : base(config)
        {
        }

        private void CreatePointCluster(TSOFile tso)
        {
            vlst = new List<Vertex>();

            foreach (TSOMesh i in tso.meshes)
                foreach (TSOSubMesh j in i.sub_meshes)
                    vlst.AddRange(j.vertices);

            pc = new PointCluster(vlst.Count);

            foreach (Vertex i in vlst)
                pc.Add(i.Pos.x, i.Pos.y, i.Pos.z);

            pc.Clustering();
        }

        protected override bool DoLoadRefTSO(string path)
        {
            tsoref = LoadTSO(path);
            tsoref.SwitchBoneIndicesOnMesh();
            CreatePointCluster(tsoref);
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

                foreach (UVertex j in obj.vertices)
                    vref.Add(pc.NearestIndex(j.Pos.x, j.Pos.y, j.Pos.z));

                obj.CreateNormal();

                // フェイスの組成
                List<int> faces1 = new List<int>();
                List<int> faces2 = new List<int>();
                //int[]                   bonecnv = new int[tsor.nodes.Length];   // ボーン変換テーブル
                VertexHeap<Vertex> vh = new VertexHeap<Vertex>();
                Vertex[] v = new Vertex[3];
                List<int> bones = new List<int>(16);
                List<ushort> indices = new List<ushort>();
                Dictionary<int, int> selected = new Dictionary<int, int>();
                Dictionary<int, int> work = new Dictionary<int, int>();
                List<TSOSubMesh> subs = new List<TSOSubMesh>();

                for (int j = 0, n = obj.faces.Count; j < n; ++j)
                    faces1.Add(j);

                #region ボーンパーティション
                Console.WriteLine("  vertices bone_indices");
                Console.WriteLine("  -------- ------------");

                while (faces1.Count > 0)
                {
                    int mtl = obj.faces[faces1[0]].mtl;
                    selected.Clear();
                    indices.Clear();
                    vh.Clear();
                    bones.Clear();

                    foreach (int j in faces1)
                    {
                        MqoFace f = obj.faces[j];

                        if (f.mtl != mtl)
                        {
                            faces2.Add(j);
                            continue;
                        }

                        v[0] = vlst[vref[f.a]];
                        v[1] = vlst[vref[f.b]];
                        v[2] = vlst[vref[f.c]];

                        work.Clear();

                        for (int k = 0; k < 3; ++k)
                        {
                            Vertex vv = v[k];
                            UInt32 idx0 = vv.Idx;
                            Point4 wgt0 = vv.Wgt;
                            byte* idx = (byte*)(&idx0);
                            float* wgt = (float*)(&wgt0);

                            for (int l = 0; l < 4; ++l)
                            {
                                if (wgt[l] <= float.Epsilon) continue;
                                if (selected.ContainsKey(idx[l])) continue;

                                if (!work.ContainsKey(idx[l]))
                                    work.Add(idx[l], 0);
                            }
                        }

                        if (selected.Count + work.Count > 16)
                        {
                            faces2.Add(j);
                            continue;
                        }

                        // ボーンリストに足してvalid
                        foreach (KeyValuePair<int, int> l in work)
                        {
                            selected.Add(l.Key, selected.Count);    // ボーンテーブルに追加
                            bones.Add(l.Key);
                        }

                        // \todo 点の追加
                        Vertex va = new Vertex(obj.vertices[f.a].Pos, v[0].Wgt, v[0].Idx, obj.vertices[f.a].Nrm, new Point2(f.ta.x, 1 - f.ta.y));
                        Vertex vb = new Vertex(obj.vertices[f.b].Pos, v[1].Wgt, v[1].Idx, obj.vertices[f.b].Nrm, new Point2(f.tb.x, 1 - f.tb.y));
                        Vertex vc = new Vertex(obj.vertices[f.c].Pos, v[2].Wgt, v[2].Idx, obj.vertices[f.c].Nrm, new Point2(f.tc.x, 1 - f.tc.y));

                        indices.Add(vh.Add(va));
                        indices.Add(vh.Add(vc));
                        indices.Add(vh.Add(vb));
                    }

                    // フェイス最適化
                    ushort[] nidx = NvTriStrip.Optimize(indices.ToArray());

                    // 頂点のボーン参照ローカルに変換
                    Vertex[] verts = vh.verts.ToArray();

                    for (int j = 0; j < verts.Length; ++j)
                    {
                        uint idx0 = verts[j].Idx;
                        byte* idx = (byte*)(&idx0);
                        Point4 wgt0 = verts[j].Wgt;
                        float* wgt = (float*)(&wgt0);

                        for (int k = 0; k < 4; ++k)
                            if (wgt[k] > float.Epsilon)
                                idx[k] = (byte)selected[idx[k]];

                        verts[j].Idx = idx0;
                    }

                    // サブメッシュ生成
                    TSOSubMesh sub = new TSOSubMesh();
                    sub.spec = mtl;
                    sub.numbones = bones.Count;
                    sub.bones = bones.ToArray();
                    sub.numvertices = nidx.Length;
                    sub.vertices = new Vertex[nidx.Length];

                    for (int j = 0; j < nidx.Length; ++j)
                        sub.vertices[j] = verts[nidx[j]];

                    Console.WriteLine("  {0,8} {1,12}", sub.vertices.Length, sub.bones.Length);

                    subs.Add(sub);

                    // 次の周回
                    List<int> t = faces1;
                    faces1 = faces2;
                    faces2 = t;
                    t.Clear();
                }
                #endregion
                // \todo TSOMesh生成
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
            vlst = null;
            return base.DoCleanup();
        }
    }
}
