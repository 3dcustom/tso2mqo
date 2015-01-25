using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Tso2MqoGui
{
    public class MqxWriter
    {
        public string MqoFile;

        public void Write(MqoBone[] bones, int numobjects)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = new String(' ', 4);
            XmlWriter writer = XmlWriter.Create(Path.ChangeExtension(MqoFile, ".mqx"), settings);
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

    public class MqoBone
    {
        public int id;
        public string name;
        public bool tail;
        //なければ-1
        public int pid;
        public List<int> cids = new List<int>();

        //根本position
        public Point3 q;

        //先端position
        public Point3 p;

        public List<MqoWeit> weits;

        public MqoBone()
        {
            weits = new List<MqoWeit>(2048*3*4);
        }

        public void Write(XmlWriter writer)
        {
            writer.WriteStartElement("Bone");
            writer.WriteAttributeString("id", (id+1).ToString());

            writer.WriteAttributeString("rtX", q.X.ToString());
            writer.WriteAttributeString("rtY", q.Y.ToString());
            writer.WriteAttributeString("rtZ", q.Z.ToString());

            writer.WriteAttributeString("tpX", p.X.ToString());
            writer.WriteAttributeString("tpY", p.Y.ToString());
            writer.WriteAttributeString("tpZ", p.Z.ToString());

            writer.WriteAttributeString("rotB", "0.0");
            writer.WriteAttributeString("rotH", "0.0");
            writer.WriteAttributeString("rotP", "0.0");

            writer.WriteAttributeString("mvX", "0.0");
            writer.WriteAttributeString("mvY", "0.0");
            writer.WriteAttributeString("mvZ", "0.0");

            writer.WriteAttributeString("sc", "1.0");

            writer.WriteAttributeString("maxAngB", "90.0");
            writer.WriteAttributeString("maxAngH", "180.0");
            writer.WriteAttributeString("maxAngP", "180.0");

            writer.WriteAttributeString("minAngB", "-90.0");
            writer.WriteAttributeString("minAngH", "-180.0");
            writer.WriteAttributeString("minAngP", "-180.0");

            writer.WriteAttributeString("isDummy", tail ? "1" : "0");
            writer.WriteAttributeString("name", name);

            writer.WriteStartElement("P");
            writer.WriteAttributeString("id", (pid+1).ToString());
            writer.WriteEndElement();

            foreach (int cid in cids)
            {
                writer.WriteStartElement("C");
                writer.WriteAttributeString("id", (cid+1).ToString());
                writer.WriteEndElement();
            }
            foreach (MqoWeit weit in weits)
            {
                weit.Write(writer);
            }

            writer.WriteEndElement();
        }
    }

    public class MqoWeit
    {
        public int object_id;
        public int vertex_id;
        //public int bone_id;
        public float weit;

        public void Write(XmlWriter writer)
        {
            float weit_percent = weit * 100.0f;

            writer.WriteStartElement("W");
            writer.WriteAttributeString("oi", object_id.ToString());
            writer.WriteAttributeString("vi", vertex_id.ToString());
            writer.WriteAttributeString("w", weit_percent.ToString());
            writer.WriteEndElement();
        }
    }
}
