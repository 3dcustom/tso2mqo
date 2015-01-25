using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Tso2MqoGui
{
    /// MqxFileを読み込みます。
    public class MqxReader
    {
        // mqo path
        //todo: rename to MqoPath
        public string MqoFile;

        string GetMqxPath()
        {
            return Path.ChangeExtension(MqoFile, ".mqx");
        }

        // ボーン配列 [out]
        public MqoBone[] bones;

        // MqxFileを読み込む。
        public void Read()
        {
            XmlReader reader = XmlReader.Create(GetMqxPath());
            reader.Read();

            reader.ReadStartElement("MetasequoiaDocument");

            reader.ReadStartElement("IncludedBy");
            string mqo_file = reader.ReadString();
            Console.WriteLine(mqo_file);
            reader.ReadEndElement();//IncludedBy

            reader.ReadStartElement("Plugin.56A31D20.71F282AB");
            reader.ReadStartElement("BoneSet");
            int len = 255;
            bones = new MqoBone[len];
            int i = 0;
            while (reader.IsStartElement("Bone"))
            {
                MqoBone bone = new MqoBone();
                bone.Read(reader);
                this.bones[i++] = bone;
            }
            reader.ReadEndElement();//BoneSet

            while (reader.IsStartElement("Obj"))
            {
                Console.WriteLine("Obj");
                Console.WriteLine("  id:{0}", reader.GetAttribute("id"));
                reader.Read();//Obj
            }

            while (reader.IsStartElement("Poses"))
            {
                Console.WriteLine("Poses");
                Console.WriteLine("  isExist:{0}", reader.GetAttribute("isExist"));
                bool empty = reader.IsEmptyElement;
                reader.Read();//Poses
                if (empty)
                    continue;
                while (reader.IsStartElement("Pose"))
                {
                    Console.WriteLine("Pose");
                    Console.WriteLine("  id:{0}", reader.GetAttribute("id"));
                    reader.Read();//Pose
                }
                reader.ReadEndElement();//Poses
            }
            reader.ReadEndElement();//Plugin.56A31D20.71F282AB
            reader.ReadEndElement();//MetasequoiaDocument

            reader.Close();
        }
    }
}
