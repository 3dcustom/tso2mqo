using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Tso2MqoGui
{
    public class MqoGenerator
    {
        public TSOFile LoadTSO(string file)
        {
            TSOFile tso = new TSOFile(file);
            tso.ReadAll();
            return tso;
        }

        public void Generate(string tso_file, string out_path, MqoBoneMode bone_mode)
        {
            string tso_filename = Path.GetFileName(tso_file);
            string mqo_file = Path.Combine(out_path, Path.ChangeExtension(tso_filename, ".mqo"));
            string xml_file = Path.Combine(out_path, Path.ChangeExtension(tso_filename, ".xml"));

            // モデル、テクスチャの作成
            using (MqoWriter mqo = new MqoWriter(mqo_file))
            {
                TSOFile tso = LoadTSO(tso_file);
                tso.SwitchBoneIndicesOnMesh();

                mqo.BoneMode = bone_mode;

                mqo.Write(tso);
                mqo.Close();

                ImportInfo ii = new ImportInfo();

                // テクスチャ情報
                foreach (TSOTex tex in tso.textures)
                    ii.textures.Add(new ImportTextureInfo(tex));

                // エフェクトの作成
                foreach (TSOEffect effect in tso.effects)
                {
                    ii.effects.Add(new ImportEffectInfo(effect));
                    File.WriteAllText(Path.Combine(out_path, effect.Name), effect.code, Encoding.Default);
                }

                // マテリアルの作成
                foreach (TSOMaterial mat in tso.materials)
                {
                    ii.materials.Add(new ImportMaterialInfo(mat));
                    File.WriteAllText(Path.Combine(out_path, mat.Name), mat.code, Encoding.Default);
                }

                ImportInfo.Save(xml_file, ii);
            }
        }
    }
}
