using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tso2MqoGui
{
    public class MqoReader
    {
        delegate bool SectionHandler(string[] tokens);

        static char[] param_delimiters = new char[] { ' ', '\t', '(', ')' };

        StreamReader sr;
        MqoFile mqo;
        MqoObject obj;

        public MqoScene Scene { get { return mqo.scene; } }
        public List<MqoMaterial> Materials { get { return mqo.materials; } }
        public List<MqoObject> Objects { get { return mqo.objects; } }

        public void Load(string path)
        {
            using (FileStream fs = File.OpenRead(path))
            {
                sr = new StreamReader(fs, Encoding.Default);
                mqo = new MqoFile();
                ReadAll();
            }
        }

        public void ReadAll()
        {
            DoRead(SectionRoot);
        }

        static string[] SplitString(string s)
        {
            List<string> tokens = new List<string>();
            StringBuilder sb = new StringBuilder(s.Length);
            bool str = false;
            bool esc = false;
            bool paren = false;
            s = s.Trim(' ', '\t', '\r', '\n');

            foreach (char i in s)
            {
                if (esc)
                {
                    sb.Append(i);
                    esc = false;
                    continue;
                }

                switch (i)
                {
                    case '\\':
                        if (str)
                            sb.Append(i);
                        else
                            esc = true;
                        break;
                    case ' ':
                    case '\t':
                        if (paren)
                            sb.Append(i);
                        else if (str)
                            sb.Append(i);
                        else if (sb.Length > 0)
                        {
                            tokens.Add(sb.ToString()); sb.Length = 0;
                        }
                        break;
                    case '(':
                        sb.Append(i);
                        if (!str)
                            paren = true;
                        break;
                    case ')':
                        sb.Append(i);
                        if (!str)
                            paren = false;
                        break;
                    case '\"':
                        sb.Append(i);
                        str = !str;
                        break;
                    default:
                        sb.Append(i);
                        break;
                }
            }

            if (sb.Length > 0)
                tokens.Add(sb.ToString());

            return tokens.ToArray();
        }

        void DoRead(SectionHandler h)
        {
            for (int lineno = 1; ; ++lineno)
            {
                string line = sr.ReadLine();

                if (line == null)
                    break;

                line = line.Trim();
                string[] tokens = SplitString(line);

                try
                {
                    if (tokens.Length == 0)
                        continue;

                    if (!h(tokens))
                        break;
                }
                catch (Exception exception)
                {
                    throw new Exception(string.Format("File format error: {0} \"{1}\"", lineno, line), exception);
                }
            }
        }

        public void Error(string[] tokens)
        {
            throw new Exception(string.Format("File Format Error: \"{0}\"", string.Concat(tokens)));
        }

        bool SectionRoot(string[] tokens)
        {
            switch (tokens[0])
            {
                case "Metasequoia":
                    {
                        // Metasequoia Document
                        if (tokens[1] != "Document")
                            Error(tokens);
                    }
                    break;
                case "Format":
                    {
                        // @since v2.2
                        // Format Text Ver 1.0
                        // @since v4.0
                        // Format Text Ver 1.1
                        if (tokens[1] != "Text")
                            Error(tokens);
                        if (tokens[2] != "Ver")
                            Error(tokens);
                        if (tokens[3] != "1.0" && tokens[3] != "1.1")
                            Error(tokens);
                    }
                    break;
                case "Thumbnail":
                    {
                        // Thumbnail 128 128 24 rgb raw {
                        // ...
                        // }
                        if (tokens[6] != "{")
                            Error(tokens);

                        DoRead(SectionThumbnail);
                    }
                    break;
                case "Scene":
                    {
                        if (tokens[1] != "{")
                            Error(tokens);

                        DoRead(SectionScene);
                    }
                    break;
                case "Material":
                    {
                        if (tokens[2] != "{")
                            Error(tokens);

                        mqo.materials = new List<MqoMaterial>(int.Parse(tokens[1]));
                        DoRead(SectionMaterial);
                    }
                    break;
                case "Object":
                    {
                        if (tokens[2] != "{")
                            Error(tokens);

                        obj = new MqoObject(tokens[1].Trim('"'));
                        mqo.objects.Add(obj);
                        DoRead(SectionObject);
                    }
                    break;
                case "Eof":
                    return false;
            }
            return true;
        }

        bool SectionThumbnail(string[] tokens)
        {
            switch (tokens[0])
            {
                case "}":
                    return false;
            }
            return true;
        }

        bool SectionScene(string[] tokens)
        {
            MqoScene scene = new MqoScene();
            mqo.scene = scene;

            switch (tokens[0])
            {
                case "pos": scene.pos = Point3.Parse(tokens, 1); break;
                case "lookat": scene.lookat = Point3.Parse(tokens, 1); break;
                case "head": scene.head = float.Parse(tokens[1]); break;
                case "pich": scene.pich = float.Parse(tokens[1]); break;
                case "ortho": scene.ortho = float.Parse(tokens[1]); break;
                case "zoom2": scene.zoom2 = float.Parse(tokens[1]); break;
                case "amb": scene.amb = Color3.Parse(tokens, 1); break;
                case "dirlights":
                    {
                        // dirlights 1 {
                        // ...
                        // }
                        if (tokens[2] != "{")
                            Error(tokens);

                        DoRead(SectionDirlights);
                    }
                    break;
                case "}":
                    return false;
            }
            return true;
        }

        bool SectionDirlights(string[] tokens)
        {
            switch (tokens[0])
            {
                case "light":
                    {
                        // light {
                        // ...
                        // }
                        if (tokens[1] != "{")
                            Error(tokens);

                        DoRead(SectionLight);
                    }
                    break;
                case "}":
                    return false;
            }
            return true;
        }

        bool SectionLight(string[] tokens)
        {
            switch (tokens[0])
            {
                case "}":
                    return false;
            }
            return true;
        }

        static string[] SplitParam(string s)
        {
            return s.Split(param_delimiters, StringSplitOptions.RemoveEmptyEntries);
        }

        bool SectionMaterial(string[] tokens)
        {
            if (tokens[0] == "}")
                return false;

            StringBuilder sb = new StringBuilder();

            foreach (string i in tokens)
                sb.Append(' ').Append(i);

            string line = sb.ToString().Trim();
            MqoMaterial m = new MqoMaterial(tokens[0].Trim('"'));
            tokens = SplitString(line);
            mqo.materials.Add(m);

            for (int i = 1; i < tokens.Length; ++i)
            {
                string t = tokens[i];
                string t2 = t.ToLower();

                if (t2.StartsWith("shader(")) m.shader = int.Parse(SplitParam(t)[1]);
                else if (t2.StartsWith("col(")) m.col = Color3.Parse(SplitParam(t), 1);
                else if (t2.StartsWith("dif(")) m.dif = float.Parse(SplitParam(t)[1]);
                else if (t2.StartsWith("amb(")) m.amb = float.Parse(SplitParam(t)[1]);
                else if (t2.StartsWith("emi(")) m.emi = float.Parse(SplitParam(t)[1]);
                else if (t2.StartsWith("spc(")) m.spc = float.Parse(SplitParam(t)[1]);
                else if (t2.StartsWith("power(")) m.power = float.Parse(SplitParam(t)[1]);
                else if (t2.StartsWith("tex(")) m.tex = t.Substring(3).Trim('(', ')', '"');
            }
            return true;
        }

        bool SectionObject(string[] tokens)
        {
            switch (tokens[0])
            {
                case "uid": obj.id = int.Parse(tokens[1]); break;
                case "visible": obj.visible = int.Parse(tokens[1]); break;
                case "locking": obj.locking = int.Parse(tokens[1]); break;
                case "shading": obj.shading = int.Parse(tokens[1]); break;
                case "facet": obj.facet = float.Parse(tokens[1]); break;
                case "color": obj.color = Color3.Parse(tokens, 1); break;
                case "color_type": obj.color_type = int.Parse(tokens[1]); break;
                case "vertex":
                    {
                        if (tokens[2] != "{")
                            Error(tokens);

                        obj.vertices = new List<UVertex>(int.Parse(tokens[1]));
                        DoRead(SectionVertex);
                    }
                    break;
                case "vertexattr":
                    {
                        if (tokens[1] != "{")
                            Error(tokens);

                        DoRead(SectionVertexAttr);
                    }
                    break;
                case "face":
                    {
                        if (tokens[2] != "{")
                            Error(tokens);

                        obj.faces = new List<MqoFace>(int.Parse(tokens[1]));
                        DoRead(SectionFace);
                    }
                    break;
                case "}":
                    return false;
            }
            return true;
        }

        bool SectionVertex(string[] tokens)
        {
            if (tokens[0] == "}")
                return false;

            UVertex v = new UVertex();
            v.Pos = Point3.Parse(tokens, 0);
            obj.vertices.Add(v);

            return true;
        }

        bool SectionVertexAttr(string[] tokens)
        {
            switch (tokens[0])
            {
                case "uid":
                    {
                        // uid {
                        // ...
                        // }
                        if (tokens[1] != "{")
                            Error(tokens);

                        DoReadSectionUid();
                    }
                    break;
                case "}":
                    return false;
            }
            return true;
        }

        void DoReadSectionUid()
        {
            int i = 0;

            for (int lineno = 1; ; ++lineno)
            {
                string line = sr.ReadLine();

                if (line == null)
                    break;

                line = line.Trim();
                string[] tokens = SplitString(line);

                try
                {
                    if (tokens.Length == 0)
                        continue;

                    if (tokens[0] == "}")
                        break;

                    obj.vertices[i++].id = int.Parse(tokens[0]);
                }
                catch (Exception exception)
                {
                    throw new Exception(string.Format("File format error: {0} \"{1}\"", lineno, line), exception);
                }
            }
        }

        bool SectionFace(string[] tokens)
        {
            if (tokens[0] == "}")
                return false;

            int nface = int.Parse(tokens[0]);
            {
                StringBuilder sb = new StringBuilder();
                foreach (string i in tokens)
                    sb.Append(' ').Append(i);
                string line = sb.ToString().Trim();
                tokens = SplitString(line);
            }
            switch (nface)
            {
                case 3:
                    {
                        MqoFace f = new MqoFace();

                        for (int i = 1; i < tokens.Length; ++i)
                        {
                            string t = tokens[i];
                            string t2 = t.ToLower();

                            if (t2.StartsWith("v("))
                            {
                                string[] t3 = SplitParam(t);
                                f.a = ushort.Parse(t3[1]);
                                f.b = ushort.Parse(t3[2]);
                                f.c = ushort.Parse(t3[3]);
                            }
                            else if (t2.StartsWith("m("))
                            {
                                string[] t3 = SplitParam(t);
                                f.spec = ushort.Parse(t3[1]);
                            }
                            else if (t2.StartsWith("uv("))
                            {
                                string[] t3 = SplitParam(t);
                                f.ta = Point2.Parse(t3, 1);
                                f.tb = Point2.Parse(t3, 3);
                                f.tc = Point2.Parse(t3, 5);
                            }
                        }
                        obj.faces.Add(f);
                    }
                    break;
                case 4:
                    {
                        MqoFace f = new MqoFace();
                        MqoFace f2 = new MqoFace();

                        for (int i = 1; i < tokens.Length; ++i)
                        {
                            string t = tokens[i];
                            string t2 = t.ToLower();

                            if (t2.StartsWith("v("))
                            {
                                string[] t3 = SplitParam(t);
                                f.a = ushort.Parse(t3[1]);
                                f.b = ushort.Parse(t3[2]);
                                f.c = ushort.Parse(t3[3]);
                                f2.a = f.a;
                                f2.b = f.c;
                                f2.c = ushort.Parse(t3[4]);
                            }
                            else if (t2.StartsWith("m("))
                            {
                                string[] t3 = SplitParam(t);
                                f.spec = ushort.Parse(t3[1]);
                                f2.spec = f.spec;
                            }
                            else if (t2.StartsWith("uv("))
                            {
                                string[] t3 = SplitParam(t);
                                f.ta = Point2.Parse(t3, 1);
                                f.tb = Point2.Parse(t3, 3);
                                f.tc = Point2.Parse(t3, 5);
                                f2.ta = f.ta;
                                f2.tb = f.tc;
                                f2.tc = Point2.Parse(t3, 7);
                            }
                        }
                        obj.faces.Add(f);
                        obj.faces.Add(f2);
                    }
                    break;
            }
            return true;
        }
    }
}
