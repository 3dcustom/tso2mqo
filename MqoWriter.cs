using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Tso2MqoGui
{
    public class Pair<T, U>
    {
        public T First;
        public U Second;

        public Pair()
        {
        }

        public Pair(T first, U second)
        {
            First = first;
            Second = second;
        }
    }

    public class MqoWriter : IDisposable
    {
        public TextWriter tw;
        public string OutPath;
        public string OutFile;
        public bool MqxEnabled;

        public MqoWriter(string file)
        {
            FileStream fs = File.OpenWrite(file);
            fs.SetLength(0);
            tw = new StreamWriter(fs, Encoding.Default);
            OutFile = file;
            OutPath = Path.GetDirectoryName(file);
        }

        void IDisposable.Dispose()
        {
            Close();
        }

        public void Close()
        {
            if (tw != null)
                tw.Close();
            tw = null;
        }

        string GetTextureFileName(TSOTex tex)
        {
            string filename = Path.GetFileName(tex.File.Trim('"'));
            if (filename == "")
                filename = "none";
            return filename;
        }

        string GetTexturePath(TSOTex tex)
        {
            return Path.Combine(OutPath, GetTextureFileName(tex));
        }

        public void CreateTextureFile(TSOTex tex)
        {
            string file = GetTexturePath(tex);
            byte[] data = tex.data;

            //TODO: .bmpのはずが.psdになってるものがある

            using (FileStream fs = File.OpenWrite(file))
            {
                BinaryWriter bw = new BinaryWriter(fs);

                switch (Path.GetExtension(file).ToUpper())
                {
                    case ".TGA":
                        bw.Write((byte)0);              // id
                        bw.Write((byte)0);              // colormap
                        bw.Write((byte)2);              // imagetype
                        bw.Write((byte)0);              // unknown0
                        bw.Write((byte)0);              // unknown1
                        bw.Write((byte)0);              // unknown2
                        bw.Write((byte)0);              // unknown3
                        bw.Write((byte)0);              // unknown4
                        bw.Write((short)0);             // width
                        bw.Write((short)0);             // height
                        bw.Write((short)tex.Width);     // width
                        bw.Write((short)tex.Height);    // height
                        bw.Write((byte)(tex.depth * 8));// depth
                        bw.Write((byte)0);              // depth
                        break;

                    default:
                        bw.Write((byte)'B');
                        bw.Write((byte)'M');
                        bw.Write((int)(54 + data.Length));
                        bw.Write((int)0);
                        bw.Write((int)54);
                        bw.Write((int)40);
                        bw.Write((int)tex.Width);
                        bw.Write((int)tex.Height);
                        bw.Write((short)1);
                        bw.Write((short)(tex.Depth * 8));
                        bw.Write((int)0);
                        bw.Write((int)data.Length);
                        bw.Write((int)0);
                        bw.Write((int)0);
                        bw.Write((int)0);
                        bw.Write((int)0);
                        break;
                }

                bw.Write(data, 0, data.Length);
                bw.Flush();
            }
        }

        public void Write(TSOFile tso)
        {
            tw.WriteLine("Metasequoia Document");
            tw.WriteLine("Format Text Ver 1.0");
            tw.WriteLine("");
            if (MqxEnabled)
            {
                tw.WriteLine("IncludeXml \"{0}\"", Path.GetFileName(Path.ChangeExtension(OutFile, ".mqx")));
                tw.WriteLine("");
            }
            tw.WriteLine("Scene {");
            tw.WriteLine("\tpos -7.0446 4.1793 1541.1764");
            tw.WriteLine("\tlookat 11.8726 193.8590 0.4676");
            tw.WriteLine("\thead 0.8564");
            tw.WriteLine("\tpich 0.1708");
            tw.WriteLine("\tortho 0");
            tw.WriteLine("\tzoom2 31.8925");
            tw.WriteLine("\tamb 0.250 0.250 0.250");
            tw.WriteLine("}");

            foreach (TSOTex tex in tso.textures)
                CreateTextureFile(tex);

            tw.WriteLine("Material {0} {{", tso.materials.Length);

            foreach (TSOMaterial mat in tso.materials)
            {
                TSOTex tex = null;
                if (tso.texturemap.TryGetValue(mat.ColorTex, out tex))
                {
                    tw.WriteLine(
                        "\t\"{0}\" col(1.000 1.000 1.000 1.000) dif(0.800) amb(0.600) emi(0.000) spc(0.000) power(5.00) tex(\"{1}\")",
                        mat.name, GetTextureFileName(tex));
                }
                else
                {
                    tw.WriteLine(
                        "\t\"{0}\" col(1.000 1.000 1.000 1.000) dif(0.800) amb(0.600) emi(0.000) spc(0.000) power(5.00))",
                        mat.name);
                }
            }

            tw.WriteLine("}");

            MqoBone[] bones = null;

            if (MqxEnabled)
                bones = CreateBones(tso);

            MqoObjectGen.uid_enabled = MqxEnabled;
            MqoObjectGen obj = new MqoObjectGen();

            ushort object_id = 0;
            foreach (TSOMesh mesh in tso.meshes)
            {
                obj.id = ++object_id;
                obj.name = mesh.Name;
                obj.Update(mesh);
                obj.Write(tw);

                if (MqxEnabled)
                    obj.AddWeits(bones);
            }

            if (MqxEnabled)
            {
                MqxWriter writer = new MqxWriter();
                writer.MqoFile = OutFile;
                writer.Write(bones, object_id /* eq numobjects */);
            }

            tw.WriteLine("Eof");
        }

        MqoBone[] CreateBones(TSOFile tso)
        {
            MqoBone[] bones = new MqoBone[tso.nodes.Length];

            tso.UpdateNodesWorld();

            foreach (TSONode node in tso.nodes)
            {
                MqoBone bone = new MqoBone();
                bone.id = node.id;
                bone.name = node.ShortName;
                bone.tail = node.children.Count == 0;

                if (node.parent == null)
                {
                    bone.pid = -1;
                }
                else
                {
                    bone.pid = node.parent.id;
                    bones[bone.pid].cids.Add(bone.id);
                }

                //根本
                bone.q = node.world.Translation;
                //先端
                if (! bone.tail)
                    bone.p = node.children[0].world.Translation;
                else
                    bone.p = node.world.Translation;

                bones[node.id] = bone;
            }
            return bones;
        }
    }

    public class MqoObjectGen
    {
        public static bool uid_enabled;

        public int id; //object_id
        public string name;
        VertexHeap<UVertex> vh = new VertexHeap<UVertex>();
        public List<MqoFace> faces;

        public int numvertices { get { return vh.Count; } }
        public List<UVertex> vertices { get { return vh.verts; } }
        public int numfaces { get { return faces.Count; } }

        public MqoObjectGen()
        {
            faces = new List<MqoFace>(2048);
        }

        public void Update(TSOMesh mesh)
        {
            vh.Clear();
            faces.Clear();

            foreach (TSOSubMesh sub_mesh in mesh.sub_meshes)
            {
                int cnt = 0;
                ushort a = 0, b = 0, c = 0;
                Vertex va = new Vertex(), vb = new Vertex(), vc = new Vertex();

                foreach (Vertex v in sub_mesh.vertices)
                {
                    ++cnt;
                    va = vb; a = b;
                    vb = vc; b = c;
                    vc = v; c = vh.Add(new UVertex(v.Pos, v.Wgt, v.Idx, v.Nrm));

                    if (cnt < 3) continue;
                    if (a == b || b == c || c == a) continue;

                    if ((cnt & 1) == 0)
                    {
                        MqoFace f = new MqoFace(a, b, c, (ushort)sub_mesh.spec,
                                new Point2(va.Tex.x, 1 - va.Tex.y),
                                new Point2(vb.Tex.x, 1 - vb.Tex.y),
                                new Point2(vc.Tex.x, 1 - vc.Tex.y));
                        faces.Add(f);
                    }
                    else
                    {
                        MqoFace f = new MqoFace(a, c, b, (ushort)sub_mesh.spec,
                                new Point2(va.Tex.x, 1 - va.Tex.y),
                                new Point2(vc.Tex.x, 1 - vc.Tex.y),
                                new Point2(vb.Tex.x, 1 - vb.Tex.y));
                        faces.Add(f);
                    }
                }
            }
        }

        public void Write(TextWriter tw)
        {
            tw.WriteLine("Object \"{0}\" {{", name);
            if (uid_enabled)
                tw.WriteLine("\tuid {0}", id);
            tw.WriteLine("\tvisible {0}", 15);
            tw.WriteLine("\tlocking {0}", 0);
            tw.WriteLine("\tshading {0}", 1);
            tw.WriteLine("\tfacet {0}", 59.5);
            tw.WriteLine("\tcolor {0:F3} {1:F3} {2:F3}", 0.898f, 0.498f, 0.698f);
            tw.WriteLine("\tcolor_type {0}", 0);

            //
            tw.WriteLine("\tvertex {0} {{", numvertices);

            foreach (UVertex v in vertices)
                v.Write(tw);

            tw.WriteLine("\t}");

            if (uid_enabled)
            {
                tw.WriteLine("\tvertexattr {");
                tw.WriteLine("\t\tuid {");

                ushort vertex_id = 0;
                foreach (UVertex v in vertices)
                    tw.WriteLine("\t\t\t{0}", ++vertex_id);

                tw.WriteLine("\t\t}");
                tw.WriteLine("\t}");
            }

            //
            tw.WriteLine("\tface {0} {{", numfaces);

            for (int i = 0, n = numfaces; i < n; i++)
                faces[i].Write(tw);
            tw.WriteLine("\t}");
            tw.WriteLine("}");
        }

        public unsafe void AddWeits(MqoBone[] bones)
        {
            ushort vertex_id = 0;
            foreach (UVertex v in vertices)
            {
                ++vertex_id;

                uint idx0 = v.Idx;
                byte* idx = (byte*)(&idx0);
                Point4 wgt0 = v.Wgt;
                float* wgt = (float*)(&wgt0);

                for (int k = 0; k < 4; ++k)
                    if (wgt[k] > float.Epsilon)
                    {
                        MqoWeit weit = new MqoWeit();
                        weit.object_id = id;
                        weit.vertex_id = vertex_id;
                        weit.weit = wgt[k];
                        bones[idx[k]].weits.Add(weit);
                    }
            }
        }
    }
}
