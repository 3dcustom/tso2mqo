using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tso2MqoGui
{
    public class TSOGeneratorOneBone : TSOGenerator
    {
        public Dictionary<string, string> ObjectBoneNames = new Dictionary<string, string>();

        public TSOGeneratorOneBone(TSOGeneratorConfig config)
            : base(config)
        {
        }

        protected override bool DoLoadRefTSO(string tsoref_file)
        {
            // 参照TSOロード
            tsoref = LoadTSO(tsoref_file);
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

                // ボーン情報作成
                uint idx = 0x00000000;
                Point4 wgt = new Point4(1, 0, 0, 0);
                int[] bones = new int[1];
                string bone;
                try
                {
                    bone = ObjectBoneNames[obj.name];
                }
                catch (KeyNotFoundException)
                {
                    throw new KeyNotFoundException(string.Format("ボーン指定に誤りがあります。オブジェクト {0} にボーンを割り当てる必要があります。", obj.name));
                }
                bones[0] = nodes[bone].ID;

                // マテリアル別に処理を実行
                List<ushort> indices = new List<ushort>();
                VertexHeap<Vertex> vh = new VertexHeap<Vertex>();
                List<TSOSubMesh> subs = new List<TSOSubMesh>();

                Console.WriteLine("  vertices bone_indices");
                Console.WriteLine("  -------- ------------");

                for (int mtl = 0; mtl < materials.Count; ++mtl)
                {
                    indices.Clear();

                    foreach (MqoFace face in obj.faces)
                    {
                        if (face.mtl != mtl)
                            continue;

                        Vertex va = new Vertex(obj.vertices[face.a].Pos, wgt, idx, obj.vertices[face.a].Nrm, new Point2(face.ta.x, 1 - face.ta.y));
                        Vertex vb = new Vertex(obj.vertices[face.b].Pos, wgt, idx, obj.vertices[face.b].Nrm, new Point2(face.tb.x, 1 - face.tb.y));
                        Vertex vc = new Vertex(obj.vertices[face.c].Pos, wgt, idx, obj.vertices[face.c].Nrm, new Point2(face.tc.x, 1 - face.tc.y));

                        indices.Add(vh.Add(va));
                        indices.Add(vh.Add(vc));
                        indices.Add(vh.Add(vb));
                    }

                    if (indices.Count == 0)
                        continue;

                    // フェイス最適化
                    ushort[] nidx = NvTriStrip.Optimize(indices.ToArray());

                    // サブメッシュ生成
                    Vertex[] verts = vh.verts.ToArray();
                    TSOSubMesh sub = new TSOSubMesh();
                    sub.spec = mtl;
                    sub.numbones = bones.Length;
                    sub.bones = bones;
                    sub.numvertices = nidx.Length;
                    sub.vertices = new Vertex[nidx.Length];

                    for (int i = 0; i < nidx.Length; ++i)
                        sub.vertices[i] = verts[nidx[i]];

                    Console.WriteLine("  {0,8} {1,12}", sub.vertices.Length, sub.bones.Length);

                    subs.Add(sub);
                }

                // メッシュ生成
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
