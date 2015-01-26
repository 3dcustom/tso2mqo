using System;
using System.Collections.Generic;
using System.Drawing.Design;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows.Forms.Design;

namespace Tso2MqoGui
{
    public abstract class TSOGenerator
    {
        string dir;
        TSOGeneratorConfig config;
        protected MqoReader mqo;
        protected MqxReader mqx;
        protected TSOFile tsoref;
        protected List<TSOMesh> meshes;
        ImportInfo ii;
        BinaryWriter bw;
        Dictionary<string, MaterialInfo> materials;
        protected int nummaterials { get { return materials.Count; } }
        Dictionary<string, TextureInfo> textures;

        public TSOGenerator(TSOGeneratorConfig config)
        {
            this.config = config;
        }

        public TSOFile LoadTSO(string file)
        {
            TSOFile tso = new TSOFile(file);
            tso.ReadAll();
            return tso;
        }

        bool SetCurrentDirectory(string dir)
        {
            this.dir = dir;
            Environment.CurrentDirectory = dir;
            return true;
        }

        bool DoLoadMQO(string mqo_file)
        {
            // MQO読み込み
            mqo = new MqoReader();
            mqo.Load(mqo_file);
            return true;
        }

        bool DoLoadMqx(string mqo_file)
        {
            // Mqx読み込み
            mqx = new MqxReader();
            if (mqx.Load(mqo_file))
            {
                mqx.CreateWeits();
                mqx.CreateWeitMap();
            }
            return true;
        }

        bool DoLoadXml(string importinfo_file)
        {
            // XML読み込み
            ii = ImportInfo.Load(importinfo_file);

            // 使用マテリアル一覧取得
            materials = new Dictionary<string, MaterialInfo>();
            bool validmap = true;

            foreach (MqoMaterial i in mqo.Materials)
            {
                MaterialInfo mi = new MaterialInfo(dir, i, ii.GetMaterial(i.name));
                validmap &= mi.Valid;
                materials.Add(i.name, mi);
            }

            if (!validmap || config.ShowMaterials)
            {
                if (config.cui)
                    throw new Exception("マテリアルの設定が無効です");

                FormMaterial dlg = new FormMaterial();
                dlg.materials = materials;

                if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return false;
            }

            // 使用テクスチャ一覧の取得
            textures = new Dictionary<string, TextureInfo>();

            foreach (MaterialInfo i in materials.Values)
            {
                string color_tex_name = Path.GetFileNameWithoutExtension(i.ColorTexture);

                if (color_tex_name != null && !textures.ContainsKey(color_tex_name))
                        textures.Add(color_tex_name, new TextureInfo(color_tex_name, i.ColorTexture));

                string shade_tex_name = Path.GetFileNameWithoutExtension(i.ShadeTexture);

                if (shade_tex_name != null && !textures.ContainsKey(shade_tex_name))
                        textures.Add(shade_tex_name, new TextureInfo(shade_tex_name, i.ShadeTexture));
            }

            return true;
        }

        bool DoWriteHeader()
        {
            bw.Write(0x314F5354);
            return true;
        }

        bool DoWriteNodeNames()
        {
            if (tsoref != null)
            {
                bw.Write(tsoref.nodes.Length);

                foreach (TSONode i in tsoref.nodes)
                    WriteString(bw, i.Name);
            }
            else if (mqx != null)
            {
                bw.Write(mqx.bones.Length);

                foreach (MqoBone i in mqx.bones)
                    WriteString(bw, i.path);
            }
            else
                return false;

            return true;
        }

        bool DoWriteNodeMatrices()
        {
            if (tsoref != null)
            {
                bw.Write(tsoref.nodes.Length);

                foreach (TSONode i in tsoref.nodes)
                    WriteMatrix(bw, i.Matrix);
            }
            else if (mqx != null)
            {
                bw.Write(mqx.bones.Length);

                foreach (MqoBone i in mqx.bones)
                    WriteMatrix(bw, i.matrix);
            }
            else
                return false;

            return true;
        }

        bool DoWriteTextures()
        {
            bw.Write(textures.Count);

            foreach (TextureInfo tex_info in textures.Values)
            {
                string file = tex_info.file;
                string name = tex_info.name;

                string file_directory_name = Path.GetDirectoryName(file);
                string file_name = Path.GetFileName(file);

                WriteString(bw, name);
                WriteString(bw, "\"" + file_name + "\"");

                // テクスチャの読み込み
                TSOTex tex = LoadTex(file);
                tex.name = name;
                bw.Write(tex.Width);
                bw.Write(tex.Height);
                bw.Write(tex.Depth);
                bw.Write(tex.data, 0, tex.data.Length);

                ImportTextureInfo import_tex_info = new ImportTextureInfo(tex);
                ii.textures.Add(import_tex_info);

                // テクスチャが同じフォルダにない場合、コピーしておく
                if (file_directory_name != "" && file_directory_name.ToUpper() != dir.ToUpper())
                {
                    import_tex_info.File = Path.Combine(dir, file_name);
                    File.Copy(file, import_tex_info.File, true);
                }
            }

            return true;
        }

        bool DoWriteEffects()
        {
            bw.Write(ii.effects.Count);

            foreach (ImportEffectInfo import_effect_info in ii.effects)
            {
                string file = Path.Combine(dir, import_effect_info.Name);
                string[] code = File.ReadAllLines(file, Encoding.Default);

                WriteString(bw, import_effect_info.Name);
                bw.Write(code.Length);

                foreach (string line in code)
                    WriteString(bw, line.Trim('\r', '\n'));
            }

            return true;
        }

        bool DoWriteMaterials()
        {
            bw.Write(mqo.Materials.Count);

            foreach (MqoMaterial mat in mqo.Materials)
            {
                MaterialInfo mat_info = materials[mat.name];
                string[] code = mat_info.GetCode();

                WriteString(bw, mat.name);
                WriteString(bw, "cgfxShader");
                bw.Write(code.Length);

                foreach (string line in code)
                    WriteString(bw, line.Trim('\r', '\n'));

                ImportMaterialInfo import_mat_info = new ImportMaterialInfo();
                import_mat_info.Name = mat.name;
                import_mat_info.File = "cgfxShader";
                ii.materials.Add(import_mat_info);

                // コードを保存する
                File.WriteAllLines(Path.Combine(dir, mat.name), code);
            }

            return true;
        }

        bool DoWriteMeshes()
        {
            bw.Write(meshes.Count);

            foreach (TSOMesh mesh in meshes)
            {
                WriteString(bw, mesh.Name);
                WriteMatrix(bw, mesh.Matrix);
                bw.Write(1);
                bw.Write(mesh.numsubs);

                foreach (TSOSubMesh sub in mesh.sub_meshes)
                {
                    bw.Write(sub.spec);
                    bw.Write(sub.numbones);

                    foreach (int i in sub.bones)
                        bw.Write(i);

                    bw.Write(sub.numvertices);

                    foreach (Vertex v in sub.vertices)
                        WriteVertex(bw, v);
                }
            }

            return true;
        }

        bool DoOutput(string path)
        {
            //----- 出力処理 -----------------------------------------------
            ii.materials.Clear();
            ii.textures.Clear();

            using (FileStream fs = File.OpenWrite(path))
            {
                fs.SetLength(0);
                bw = new BinaryWriter(fs);

                DoWriteHeader();
                DoWriteNodeNames();
                DoWriteNodeMatrices();
                DoWriteTextures();
                DoWriteEffects();
                DoWriteMaterials();
                DoGenerateMeshes();
                DoWriteMeshes();
            }

            return true;
        }

        //メッシュリストを生成する。
        //メッシュリストはthis.meshesに保持する。
        protected abstract bool DoGenerateMeshes();

        bool DoSaveXml(string importinfo_file)
        {
            // 結果を保存しておく
            ImportInfo.Save(importinfo_file, ii);
            return true;
        }

        protected virtual bool DoCleanup()
        {
            dir = null;
            tsoref = null;
            meshes = null;
            mqo = null;
            ii = null;
            bw = null;
            materials = null;
            textures = null;

            System.GC.Collect();
            return true;
        }

        public void Generate(string mqo_file, string tsoref_file, string tsoout_file)
        {
            string dir = Path.GetDirectoryName(mqo_file);
            string importinfo_file = Path.ChangeExtension(mqo_file, ".xml");

            try
            {
                if (!SetCurrentDirectory(dir)) return;
                if (!DoLoadMQO(mqo_file)) return;
                if (!DoLoadMqx(mqo_file)) return;
                if (!DoLoadRefTSO(tsoref_file)) return;
                if (!DoLoadXml(importinfo_file)) return;
                if (!DoOutput(tsoout_file)) return;
                if (!DoSaveXml(importinfo_file)) return;
            }
            finally
            {
                DoCleanup();
            }
        }

        // 参照tsoを読み込む。
        // 参照tsoはthis.tsorefに保持する。
        protected abstract bool DoLoadRefTSO(string path);

        #region ユーティリティ
        public void WriteString(BinaryWriter bw, string s)
        {
            byte[] b = Encoding.Default.GetBytes(s);
            bw.Write(b);
            bw.Write((byte)0);
        }

        public void WriteMatrix(BinaryWriter bw, Matrix44 m)
        {
            bw.Write(m.M11); bw.Write(m.M12); bw.Write(m.M13); bw.Write(m.M14);
            bw.Write(m.M21); bw.Write(m.M22); bw.Write(m.M23); bw.Write(m.M24);
            bw.Write(m.M31); bw.Write(m.M32); bw.Write(m.M33); bw.Write(m.M34);
            bw.Write(m.M41); bw.Write(m.M42); bw.Write(m.M43); bw.Write(m.M44);
        }

        public unsafe void WriteVertex(BinaryWriter bw, Vertex v)
        {
            uint idx0 = v.Idx;
            byte* idx = (byte*)(&idx0);
            List<int> idxs = new List<int>(4);
            List<float> wgts = new List<float>(4);

            if (v.Wgt.x > 0) { idxs.Add(idx[0]); wgts.Add(v.Wgt.x); }
            if (v.Wgt.y > 0) { idxs.Add(idx[1]); wgts.Add(v.Wgt.y); }
            if (v.Wgt.z > 0) { idxs.Add(idx[2]); wgts.Add(v.Wgt.z); }
            if (v.Wgt.w > 0) { idxs.Add(idx[3]); wgts.Add(v.Wgt.w); }

            bw.Write(v.Pos.X); bw.Write(v.Pos.Y); bw.Write(v.Pos.Z);
            bw.Write(v.Nrm.X); bw.Write(v.Nrm.Y); bw.Write(v.Nrm.Z);
            bw.Write(v.Tex.X); bw.Write(v.Tex.Y);

            bw.Write(wgts.Count);

            for (int i = 0, n = idxs.Count; i < n; ++i)
            {
                bw.Write(idxs[i]);
                bw.Write(wgts[i]);
            }
        }
        #endregion
        #region テクスチャ処理
        public TSOTex LoadTex(string file)
        {
            string ext = Path.GetExtension(file).ToUpper();
            TSOTex tex;

            switch (ext)
            {
                case ".TGA": tex = LoadTarga(file); break;
                case ".BMP": tex = LoadBitmap(file); break;
                default: throw new Exception("Unsupported texture file: " + file);
            }

            for (int i = 0, n = tex.data.Length; i < n; i += tex.Depth)
            {
                byte b = tex.data[i + 0];
                tex.data[i + 0] = tex.data[i + 2];
                tex.data[i + 2] = b;
            }

            return tex;
        }

        public unsafe TSOTex LoadTarga(string file)
        {
            using (FileStream fs = File.OpenRead(file))
            {
                BinaryReader br = new BinaryReader(fs);
                TARGA_HEADER header;

                Marshal.Copy(br.ReadBytes(sizeof(TARGA_HEADER)), 0, (IntPtr)(&header), sizeof(TARGA_HEADER));

                if (header.imagetype != 0x02) throw new Exception("Invalid imagetype: " + file);
                if (header.depth != 24
                && header.depth != 32) throw new Exception("Invalid depth: " + file);

                TSOTex tex = new TSOTex();
                tex.depth = header.depth / 8;
                tex.width = header.width;
                tex.height = header.height;
                tex.File = file;
                tex.data = br.ReadBytes(tex.width * tex.height * tex.depth);

                return tex;
            }
        }

        public unsafe TSOTex LoadBitmap(string file)
        {
            using (FileStream fs = File.OpenRead(file))
            {
                BinaryReader br = new BinaryReader(fs);
                BITMAPFILEHEADER bfh;
                BITMAPINFOHEADER bih;

                Marshal.Copy(br.ReadBytes(sizeof(BITMAPFILEHEADER)), 0, (IntPtr)(&bfh), sizeof(BITMAPFILEHEADER));
                Marshal.Copy(br.ReadBytes(sizeof(BITMAPINFOHEADER)), 0, (IntPtr)(&bih), sizeof(BITMAPINFOHEADER));

                if (bfh.bfType != 0x4D42) throw new Exception("Invalid imagetype: " + file);
                if (bih.biBitCount != 24
                && bih.biBitCount != 32) throw new Exception("Invalid depth: " + file);

                TSOTex tex = new TSOTex();
                tex.depth = bih.biBitCount / 8;
                tex.width = bih.biWidth;
                tex.height = bih.biHeight;
                tex.File = file;
                tex.data = br.ReadBytes(tex.width * tex.height * tex.depth);

                return tex;
            }
        }
        #endregion
    }

    public class TextureInfo
    {
        public string name;
        public string file;

        public TextureInfo(string name, string file)
        {
            this.name = name;
            this.file = file;
        }
    }

    public class MaterialInfo
    {
        string name;
        string shader;
        string color_tex;
        string shade_tex;
        //public Dictionary<string, string>   parameters;

        public MaterialInfo(string path, MqoMaterial mat, ImportMaterialInfo import_mat_info)
        {
            name = mat.name;
            color_tex = mat.tex;

            if (import_mat_info != null)
            {
                string file = Path.Combine(path, import_mat_info.Name);

                if (File.Exists(file))
                    shader = import_mat_info.Name;

                if (import_mat_info.ShadeTex != null)
                {
                    file = Path.Combine(path, import_mat_info.ShadeTex.File);

                    if (File.Exists(file))
                        shade_tex = import_mat_info.ShadeTex.File;
                }
            }
        }

        public bool Valid
        {
            get
            {
                return File.Exists(shader);
            }
        }

        public string[] GetCode()
        {
            TSOMaterialCode code = TSOMaterialCode.GenerateFromFile(shader);
            if (color_tex != null)
                code.SetValue("ColorTex", Path.GetFileNameWithoutExtension(color_tex));
            if (shade_tex != null)
                code.SetValue("ShadeTex", Path.GetFileNameWithoutExtension(shade_tex));

            List<string> line = new List<string>();
            foreach (KeyValuePair<string, TSOParameter> i in code)
                line.Add(i.Value.ToString());

            return line.ToArray();
        }

        public string Name { get { return name; } }

        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        [DisplayNameAttribute("シェーダー設定ファイル")]
        public string ShaderFile { get { return shader; } set { shader = value; } }

        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        [DisplayNameAttribute("テクスチャ：カラー")]
        public string ColorTexture { get { return color_tex; } set { color_tex = value; } }

        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        [DisplayNameAttribute("テクスチャ：シェーティング")]
        public string ShadeTexture { get { return shade_tex; } set { shade_tex = value; } }
    }
}
