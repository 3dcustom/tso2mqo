using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Tso2MqoGui
{
    /// MqxFileを書き出します。
    public class MqxWriter
    {
        // mqo path
        //todo: rename to MqoPath
        public string MqoFile;

        string GetMqxPath()
        {
            return Path.ChangeExtension(MqoFile, ".mqx");
        }

        // MqxFileを書き出す。
        //
        // bones:
        // ボーン配列
        // numobjects:
        // オブジェクトの総数
        // スキン設定の対象オブジェクトは全てのオブジェクトとする。
        //
        // todo: oids
        public void Write(MqoBone[] bones, int numobjects)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = new String(' ', 4);
            XmlWriter writer = XmlWriter.Create(GetMqxPath(), settings);
            writer.WriteStartElement("MetasequoiaDocument");
                writer.WriteElementString("IncludedBy", Path.GetFileName(MqoFile));

            writer.WriteStartElement("Plugin.56A31D20.71F282AB");
                writer.WriteAttributeString("name", "Bone");
            writer.WriteStartElement("BoneSet");

            foreach (MqoBone bone in bones)
                bone.Write(writer);

            writer.WriteEndElement();//BoneSet

            for (int i = 0; i < numobjects; i++)
            {
                writer.WriteStartElement("Obj");
                writer.WriteAttributeString("id", (i+1).ToString());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();//Plugin.56A31D20.71F282AB

            writer.WriteEndElement();//MetasequoiaDocument
            writer.Close();
        }
    }
}
