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

        //頂点ウェイト配列 [out]
        public MqoWeit[] weits;

        public void CreateWeits()
        {
            weits = new MqoWeit[4];
            for (int i = 0; i < 4; ++i)
            {
                weits[i] = new MqoWeit();
            }
        }

        // MqxFileを読み込む。
        public bool Load(string mqo_file)
        {
            MqoFile = mqo_file;
            string mqx_path = GetMqxPath();

            if (! File.Exists(mqx_path))
                return false;

            XmlReader reader = XmlReader.Create(mqx_path);
            Read(reader);
            reader.Close();

            return true;
        }

        Dictionary<int, MqoBone> bone_idmap;

        // create bone_idmap
        // map id to bone
        void CreateBoneMap()
        {
            bone_idmap = new Dictionary<int, MqoBone>();
            foreach (MqoBone bone in bones)
            {
                bone_idmap[bone.id] = bone;
            }
        }
        void UpdateBones()
        {
            // assign node.parent
            foreach (MqoBone bone in bones)
            {
                if (bone.pid != 0)
                {
                    bone.parent = bone_idmap[bone.pid];
                }
                else
                    bone.parent = null;
            }
            // assign node.path
            foreach (MqoBone bone in bones)
            {
                if (bone.parent != null)
                {
                    bone.path = bone.parent.path + "|" + bone.name;
                }
                else
                    bone.path = "|" + bone.name;
            }
            // assign node.local_position
            foreach (MqoBone bone in bones)
            {
                if (bone.parent != null)
                {
                    bone.local_position = bone.world_position - bone.parent.world_position;
                }
                else
                    bone.local_position = bone.world_position;
            }
        }

        public void Read(XmlReader reader)
        {
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

            len = i;
            Array.Resize(ref bones, len);

            CreateBoneMap();
            UpdateBones();

            while (reader.IsStartElement("Obj"))
            {
                //Console.WriteLine("Obj");
                //Console.WriteLine("  id:{0}", reader.GetAttribute("id"));
                reader.Read();//Obj
            }

            while (reader.IsStartElement("Poses"))
            {
                //Console.WriteLine("Poses");
                //Console.WriteLine("  isExist:{0}", reader.GetAttribute("isExist"));
                bool empty = reader.IsEmptyElement;
                reader.Read();//Poses
                if (empty)
                    continue;
                while (reader.IsStartElement("Pose"))
                {
                    //Console.WriteLine("Pose");
                    //Console.WriteLine("  id:{0}", reader.GetAttribute("id"));
                    reader.Read();//Pose
                }
                reader.ReadEndElement();//Poses
            }
            reader.ReadEndElement();//Plugin.56A31D20.71F282AB
            reader.ReadEndElement();//MetasequoiaDocument
        }

        List<Dictionary<int, List<MqoWeit>>> weitmap;

        public void CreateWeitMap()
        {
            int maxobjects = 255;
            weitmap = new List<Dictionary<int, List<MqoWeit>>>(maxobjects);
            for (int i = 0; i < maxobjects; i++)
            {
                weitmap.Add(new Dictionary<int, List<MqoWeit>>(2048));
            }
            foreach (MqoBone bone in bones)
            {
                foreach (MqoWeit weit in bone.weits)
                {
                    Dictionary<int, List<MqoWeit>> map = weitmap[weit.object_id];
                    List<MqoWeit> weits;
                    if (! map.TryGetValue(weit.vertex_id, out weits))
                    {
                        weits = map[weit.vertex_id] = new List<MqoWeit>(4);
                    }
                    weits.Add(weit);
                }
            }
        }

        public void UpdateWeits(int object_id, int vertex_id)
        {
            List<MqoWeit> weits = weitmap[object_id][vertex_id];
            int len = weits.Count;
            if (len > 4)
                len = 4;

            //todo: sort

            for (int i = 0; i < len; ++i)
            {
                this.weits[i].bone_id = weits[i].bone_id;
                this.weits[i].weit = weits[i].weit; 
            }
            for (int i = len; i < 4; ++i)
            {
                this.weits[i].bone_id = 1;
                this.weits[i].weit = 0.0f;
            }
        }
    }
}
