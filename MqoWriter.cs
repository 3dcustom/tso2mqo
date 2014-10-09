using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tso2MqoGui
{
    public enum MqoBoneMode
    {
        None,
        RokDeBone,
        Mikoto,
    }

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
        public MqoBoneMode BoneMode = MqoBoneMode.None;

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

        public void Write(TSOFile file)
        {
            tw.WriteLine("Metasequoia Document");
            tw.WriteLine("Format Text Ver 1.0");
            tw.WriteLine("");
            tw.WriteLine("Scene {");
            tw.WriteLine("\tpos -7.0446 4.1793 1541.1764");
            tw.WriteLine("\tlookat 11.8726 193.8590 0.4676");
            tw.WriteLine("\thead 0.8564");
            tw.WriteLine("\tpich 0.1708");
            tw.WriteLine("\tortho 0");
            tw.WriteLine("\tzoom2 31.8925");
            tw.WriteLine("\tamb 0.250 0.250 0.250");
            tw.WriteLine("}");

            VertexHeap<UVertex> vh = new VertexHeap<UVertex>();
            List<MqoFace> face = new List<MqoFace>(2048);

            foreach (TSOTex tex in file.textures)
                CreateTextureFile(tex);

            tw.WriteLine("Material {0} {{", file.materials.Length);

            foreach (TSOMaterial mat in file.materials)
            {
                TSOTex tex = null;
                if (file.texturemap.TryGetValue(mat.ColorTex, out tex))
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

            foreach (TSOMesh i in file.meshes)
            {
                vh.Clear();
                face.Clear();

                foreach (TSOSubMesh j in i.sub_meshes)
                {
                    int cnt = 0;
                    ushort a = 0, b = 0, c = 0;
                    Vertex va = new Vertex(), vb = new Vertex(), vc = new Vertex();

                    foreach (Vertex k in j.vertices)
                    {
                        ++cnt;
                        va = vb; a = b;
                        vb = vc; b = c;
                        vc = k; c = vh.Add(new UVertex(k.Pos, k.Nrm, k.Tex));

                        if (cnt < 3) continue;
                        if (a == b || b == c || c == a) continue;

                        if ((cnt & 1) == 0)
                        {
                            MqoFace f = new MqoFace(a, b, c, (ushort)j.spec,
                                    new Point2(va.Tex.x, 1 - va.Tex.y),
                                    new Point2(vb.Tex.x, 1 - vb.Tex.y),
                                    new Point2(vc.Tex.x, 1 - vc.Tex.y));
                            face.Add(f);
                        }
                        else
                        {
                            MqoFace f = new MqoFace(a, c, b, (ushort)j.spec,
                                    new Point2(va.Tex.x, 1 - va.Tex.y),
                                    new Point2(vc.Tex.x, 1 - vc.Tex.y),
                                    new Point2(vb.Tex.x, 1 - vb.Tex.y));
                            face.Add(f);
                        }
                    }
                }

                tw.WriteLine("Object \"{0}\" {{", i.Name);
                tw.WriteLine("\tvisible {0}", 15);
                tw.WriteLine("\tlocking {0}", 0);
                tw.WriteLine("\tshading {0}", 1);
                tw.WriteLine("\tfacet {0}", 59.5);
                tw.WriteLine("\tcolor {0:F3} {1:F3} {2:F3}", 0.898f, 0.498f, 0.698f);
                tw.WriteLine("\tcolor_type {0}", 0);

                //
                tw.WriteLine("\tvertex {0} {{", vh.Count);

                foreach (UVertex j in vh.verts)
                    WriteVertex(j.Pos.x, j.Pos.y, j.Pos.z);

                tw.WriteLine("\t}");

                //
                tw.WriteLine("\tface {0} {{", face.Count);

                for (int j = 0, n = face.Count; j < n; j++)
                    WriteFace(face[j]);
                tw.WriteLine("\t}");
                tw.WriteLine("}");
            }

            // ボーンを出す
            switch (BoneMode)
            {
                case MqoBoneMode.None:
                    break;
                case MqoBoneMode.RokDeBone:
                    {
                        // マトリクス計算
                        foreach (TSONode i in file.nodes)
                        {
                            if (i.parent == null)
                                i.world = i.Matrix;
                            else i.world = Matrix44.Mul(i.Matrix, i.parent.World);
                        }

                        List<Point3> points = new List<Point3>();
                        List<int> bones = new List<int>();

                        tw.WriteLine("Object \"{0}\" {{", "Bone");
                        tw.WriteLine("\tvisible {0}", 15);
                        tw.WriteLine("\tlocking {0}", 0);
                        tw.WriteLine("\tshading {0}", 1);
                        tw.WriteLine("\tfacet {0}", 59.5);
                        tw.WriteLine("\tcolor {0} {1} {2}", 1, 0, 0);
                        tw.WriteLine("\tcolor_type {0}", 0);

                        foreach (TSONode i in file.nodes)
                        {
                            if (i.children.Count == 0)
                                continue;

                            Point3 q = new Point3(i.world.M41, i.world.M42, i.world.M43);
                            Point3 p = new Point3();

                            foreach (TSONode j in i.children)
                            {
                                p.x += j.world.M41;
                                p.y += j.world.M42;
                                p.z += j.world.M43;
                            }

                            p.x /= i.children.Count;
                            p.y /= i.children.Count;
                            p.z /= i.children.Count;

                            bones.Add(points.Count); points.Add(q);
                            bones.Add(points.Count); points.Add(p);
                        }

                        tw.WriteLine("\tvertex {0} {{", points.Count);

                        foreach (Point3 j in points)
                            WriteVertex(j.x, j.y, j.z);

                        tw.WriteLine("\t}");

                        //
                        tw.WriteLine("\tface {0} {{", bones.Count / 2);

                        for (int j = 0, n = bones.Count; j < n; j += 2)
                            tw.WriteLine(string.Format("\t\t2 V({0} {1})", bones[j + 0], bones[j + 1]));

                        tw.WriteLine("\t}");
                        tw.WriteLine("}");
                    }
                    break;

                case MqoBoneMode.Mikoto:
                    break;
            }

            tw.WriteLine("Eof");
        }

        public void WriteFace(MqoFace f)
        {
            tw.WriteLine("\t\t{0} V({1} {2} {3}) M({10}) UV({4:F5} {5:F5} {6:F5} {7:F5} {8:F5} {9:F5})",
                3, f.a, f.b, f.c, f.ta.x, f.ta.y, f.tb.x, f.tb.y, f.tc.x, f.tc.y, f.mtl);
        }

        public void WriteVertex(float x, float y, float z)
        {
            tw.WriteLine("\t\t{0:F4} {1:F4} {2:F4}", x, y, z);
        }
    }

    public class UVertex : IComparable<UVertex>
    {
        public Point3 Pos;
        public Point3 Nrm;
        public Point2 Tex;

        public UVertex()
        {
        }

        public UVertex(Point3 pos, Point3 nrm, Point2 tex)
        {
            Pos = pos;
            Nrm = nrm;
            Tex = tex;
        }

        public int CompareTo(UVertex o)
        {
            int cmp;
            cmp = Pos.CompareTo(o.Pos); if (cmp != 0) return cmp;
            cmp = Nrm.CompareTo(o.Nrm);
            return cmp;
        }

        public override int GetHashCode()
        {
            return Pos.GetHashCode() ^ Nrm.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is UVertex)
            {
                UVertex v = (UVertex)obj;
                return Pos.Equals(v.Pos) && Nrm.Equals(v.Nrm);
            }
            return false;
        }

        public bool Equals(UVertex v)
        {
            if ((object)v == null)
            {
                return false;
            }

            return Pos.Equals(v.Pos) && Nrm.Equals(v.Nrm);
        }
    }
}
