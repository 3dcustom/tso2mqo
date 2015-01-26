using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Tso2MqoGui
{
    public class MqoBone
    {
        public int id;
        public string name;
        public bool tail;
        //親id
        //なければ0
        public int pid;
        //子id
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
            writer.WriteAttributeString("id", id.ToString());

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
            writer.WriteAttributeString("id", pid.ToString());
            writer.WriteEndElement();

            foreach (int cid in cids)
            {
                writer.WriteStartElement("C");
                writer.WriteAttributeString("id", cid.ToString());
                writer.WriteEndElement();
            }
            foreach (MqoWeit weit in weits)
            {
                weit.Write(writer);
            }

            writer.WriteEndElement();
        }

        public void Read(XmlReader reader)
        {
            this.id = int.Parse(reader.GetAttribute("id"));
            this.name = reader.GetAttribute("name");

            reader.Read();//Bone

            if (reader.IsStartElement("P"))
            {
                int id = int.Parse(reader.GetAttribute("id"));
                reader.Read();//P
                this.pid = id;
            }

            while (reader.IsStartElement("C"))
            {
                int id = int.Parse(reader.GetAttribute("id"));
                reader.Read();//C
                this.cids.Add(id);
            }

            while (reader.IsStartElement("W"))
            {
                MqoWeit weit = new MqoWeit();
                weit.Read(reader);
                weit.bone_id = this.id;
                this.weits.Add(weit);
            }

            reader.ReadEndElement();//Bone
        }

        public void Dump()
        {
            Console.WriteLine("Bone");

            Console.WriteLine("  id:{0}", this.id);
            Console.WriteLine("  name:{0}", this.name);

            Console.WriteLine("P");
            Console.WriteLine("  id:{0}", pid);

            foreach (int cid in cids)
            {
                Console.WriteLine("C");
                Console.WriteLine("  id:{0}", cid);
            }
        }
    }

    public class MqoWeit
    {
        public int object_id;
        public int vertex_id;
        public int bone_id;
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

        public void Read(XmlReader reader)
        {
            this.object_id = int.Parse(reader.GetAttribute("oi"));
            this.vertex_id = int.Parse(reader.GetAttribute("vi"));
            this.weit = float.Parse(reader.GetAttribute("w")) * 0.01f;

            reader.Read();//W
        }

        public void Dump()
        {
            Console.WriteLine("W");

            Console.WriteLine("  oi:{0}", this.object_id);
            Console.WriteLine("  vi:{0}", this.vertex_id);
            Console.WriteLine("  w:{0}", this.weit);
        }
    }
}
