using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Tso2MqoGui
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            RegistryKey reg = Application.UserAppDataRegistry.CreateSubKey("Config");
            tbPath.Text = (string)reg.GetValue("OutPath", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            tabControl1.SelectedIndex = (int)reg.GetValue("TabPage", 0);
            tbMqoFile.Text = (string)reg.GetValue("MqoIn", "");
            tbTsoFileRef.Text = (string)reg.GetValue("Tso", "");
            tbTsoFile.Text = (string)reg.GetValue("TsoEx", "");
            tbMergeTso.Text = (string)reg.GetValue("MergeTso", "");
            rbRefBone.Checked = (int)reg.GetValue("RefBone", 1) == 1;
            rbOneBone.Checked = (int)reg.GetValue("OneBone", 0) == 1;
            rbBoneNone.Checked = (int)reg.GetValue("BoneNone", 1) == 1;
            rbBoneRokDeBone.Checked = (int)reg.GetValue("BoneRokDeBone", 0) == 1;
            cbMakeSub.Checked = (int)reg.GetValue("MakeSub", 1) == 1;
            cbCopyTSO.Checked = (int)reg.GetValue("CopyTSO", 1) == 1;
            cbShowMaterials.Checked = (int)reg.GetValue("ShowMaterials", 0) == 1;

            reg = Application.UserAppDataRegistry.CreateSubKey("Form1");
            Bounds = new Rectangle(
                (int)reg.GetValue("Left", 0),
                (int)reg.GetValue("Top", 0),
                (int)reg.GetValue("Width", 640),
                (int)reg.GetValue("Height", 320));

            EnableControlStuff();

            Config config = Config.Instance;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            RegistryKey reg = Application.UserAppDataRegistry.CreateSubKey("Config");
            reg.SetValue("OutPath", tbPath.Text);
            reg.SetValue("TabPage", tabControl1.SelectedIndex);
            reg.SetValue("MqoIn", tbMqoFile.Text);
            reg.SetValue("Tso", tbTsoFileRef.Text);
            reg.SetValue("TsoEx", tbTsoFile.Text);
            reg.SetValue("MergeTso", tbMergeTso.Text);
            reg.SetValue("RefBone", rbRefBone.Checked ? 1 : 0);
            reg.SetValue("OneBone", rbOneBone.Checked ? 1 : 0);
            reg.SetValue("BoneNone", rbBoneNone.Checked ? 1 : 0);
            reg.SetValue("BoneRokDeBone", rbBoneRokDeBone.Checked ? 1 : 0);
            reg.SetValue("MakeSub", cbMakeSub.Checked ? 1 : 0);
            reg.SetValue("CopyTSO", cbCopyTSO.Checked ? 1 : 0);
            reg.SetValue("ShowMaterials", cbShowMaterials.Checked ? 1 : 0);

            reg = Application.UserAppDataRegistry.CreateSubKey("Form1");

            if ((this.WindowState & FormWindowState.Minimized) == FormWindowState.Minimized)
            {
                reg.SetValue("Top", RestoreBounds.Top);
                reg.SetValue("Left", RestoreBounds.Left);
                reg.SetValue("Width", RestoreBounds.Width);
                reg.SetValue("Height", RestoreBounds.Height);
            }
            else
            {
                reg.SetValue("Top", Top);
                reg.SetValue("Left", Left);
                reg.SetValue("Width", Width);
                reg.SetValue("Height", Height);
            }

            Config.Save();
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                    return;

                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files.Length == 0)
                    return;

                switch (tabControl1.SelectedIndex)
                {
                    case 0:
                        foreach (string i in files)
                        {
                            if (Path.GetExtension(i).ToUpper() == ".TSO")
                                GenerateMqo(i);
                        }

                        break;

                    case 1:
                        switch (Path.GetExtension(files[0]).ToUpper())
                        {
                            case ".TSO": tbTsoFileRef.Text = files[0]; break;
                            case ".MQO": tbMqoFile.Text = files[0]; break;
                        }

                        break;

                    case 2:
                        AddMergeTso(files);
                        break;
                }
            }
            catch (Exception exception)
            {
                Util.ProcessError(exception);
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            e.Effect = DragDropEffects.Copy;
        }

        private void tbMergeTso_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            switch (Path.GetExtension(files[0]).ToUpper())
            {
                case ".TSO": tbMergeTso.Text = files[0]; break;
            }
        }

        private void tbMergeTso_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            e.Effect = DragDropEffects.Copy;
        }

        private void GenerateMqo(string tso_file)
        {
            string out_path = tbPath.Text;

            if (cbMakeSub.Checked)
            {
                out_path = Path.Combine(out_path, Path.GetFileNameWithoutExtension(tso_file));
                Directory.CreateDirectory(out_path);
            }

            try
            {
                label2.BackColor = Color.Tomato;
                label2.ForeColor = Color.White;
                label2.Text = "Processing";
                label2.Invalidate();
                label2.Update();

                MqoGenerator gen = new MqoGenerator();
                gen.Generate(tso_file, out_path, rbBoneRokDeBone.Checked);

                if (cbCopyTSO.Checked)
                {
                    string tso_path = Path.Combine(out_path, Path.GetFileName(tso_file));

                    if (tso_file != tso_path)
                        File.Copy(tso_file, tso_path, true);
                }
            }
            finally
            {
                label2.BackColor = SystemColors.Control;
                label2.BackColor = label2.Parent.BackColor;
                label2.ForeColor = SystemColors.ControlText;
                label2.Text = "Drop TSO File Here!";
            }
        }

        private void GenerateTso(string file)
        {
            TSOGeneratorConfig config = new TSOGeneratorConfig();
            config.ShowMaterials = cbShowMaterials.Checked;

            if (rbRefBone.Checked)
            {
                TSOGeneratorRefBone gen = new TSOGeneratorRefBone(config);
                gen.Generate(file, tbTsoFileRef.Text, tbTsoFile.Text);
            }
            else
                if (rbOneBone.Checked)
                {
                    TSOGeneratorOneBone gen = new TSOGeneratorOneBone(config);

                    foreach (ListViewItem item in lvObjects.Items)
                    {
                        if (item.SubItems[1].Text == "")
                        {
                            MessageBox.Show("すべてのオブジェクトにボーンを設定してください");
                            return;
                        }

                        gen.ObjectBoneNames.Add(item.SubItems[0].Text, item.SubItems[1].Text);
                    }

                    gen.Generate(file, tbTsoFileRef.Text, tbTsoFile.Text);
                }
                else
                {
                }
        }
        #region tso->mqo UI
        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.SelectedPath = tbPath.Text;

            if (dlg.ShowDialog() == DialogResult.OK)
                tbPath.Text = dlg.SelectedPath;
        }
        #endregion
        #region mqo->tso UI
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            EnableControlStuff();
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            EnableControlStuff();
        }

        private void EnableControlStuff()
        {
            gbBone.Enabled = rbOneBone.Checked;
        }

        private void BuildBoneTree(TreeNodeCollection nodes, TSONode node)
        {
            TreeNode tn = nodes.Add(node.ShortName);
            tn.Tag = node;

            if (node.children != null)
                foreach (TSONode i in node.children)
                    BuildBoneTree(tn.Nodes, i);
        }

        private void SaveAssign()
        {
            foreach (ListViewItem item in lvObjects.Items)
            {
                string obj = item.SubItems[0].Text;
                string bone = item.SubItems[1].Text;

                if (Config.Instance.object_bone_map.ContainsKey(obj))
                    Config.Instance.object_bone_map[obj] = bone;
                else
                    Config.Instance.object_bone_map.Add(obj, bone);
            }
        }

        private void btnMqoFile_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Filter = "Metasequoia File (*.mqo)|*.mqo";
                dlg.FileName = tbMqoFile.Text;

                if (dlg.ShowDialog() == DialogResult.OK)
                    tbMqoFile.Text = dlg.FileName;
            }
            catch (Exception exception)
            {
                Util.ProcessError(exception);
            }
        }

        private void btnTsoFileRef_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Filter = "TSO File (*.tso)|*.tso";
                dlg.FileName = tbTsoFileRef.Text;

                if (dlg.ShowDialog() == DialogResult.OK)
                    tbTsoFileRef.Text = dlg.FileName;
            }
            catch (Exception exception)
            {
                Util.ProcessError(exception);
            }
        }

        private void btnTsoFile_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = "TSO File (*.tso)|*.tso";
                dlg.FileName = tbTsoFile.Text;

                if (dlg.ShowDialog() == DialogResult.OK)
                    tbTsoFile.Text = dlg.FileName;
            }
            catch (Exception exception)
            {
                Util.ProcessError(exception);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                // 一旦現状を保存
                SaveAssign();

                // オブジェクト
                MqoReader mqo = new MqoReader();
                mqo.Load(tbMqoFile.Text);
                lvObjects.Items.Clear();

                foreach (MqoObject obj in mqo.Objects)
                {
                    ListViewItem item = lvObjects.Items.Add(obj.name);
                    item.Tag = obj;
                    string bone;

                    if (Config.Instance.object_bone_map.TryGetValue(obj.name, out bone))
                        item.SubItems.Add(bone);
                    else
                        item.SubItems.Add("");
                }

                // ボーン構造
                TSOFile tso = new TSOFile(tbTsoFileRef.Text);
                tso.ReadAll();
                tvBones.Visible = false;
                tvBones.Nodes.Clear();
                BuildBoneTree(tvBones.Nodes, tso.nodes[0]);
                tvBones.ExpandAll();
                tvBones.Nodes[0].EnsureVisible();
            }
            catch (Exception exception)
            {
                Util.ProcessError(exception);
            }
            finally
            {
                tvBones.Visible = true;
            }
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in lvObjects.Items)
                item.Selected = true;
        }

        private void btnDeselectAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in lvObjects.Items)
                item.Selected = false;
        }

        private void btnAssign_Click(object sender, EventArgs e)
        {
            try
            {
                TreeNode node = tvBones.SelectedNode;

                if (node == null)
                {
                    MessageBox.Show("割り当てるボーンを選択してください");
                    return;
                }

                foreach (ListViewItem item in lvObjects.SelectedItems)
                    item.SubItems[1].Text = node.Text;

                SaveAssign();
            }
            catch (Exception ex)
            {
                Util.ProcessError(ex);
            }
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            Color c = tabPage2.BackColor;

            try
            {
                tabPage2.BackColor = Color.Tomato;
                tabPage2.Update();
                string file = tbMqoFile.Text;
                GenerateTso(file);
            }
            catch (Exception exception)
            {
                Util.ProcessError(exception);
            }
            finally
            {
                tabPage2.BackColor = c;
            }
        }
        #endregion
        #region Merge UI
        private void AddMergeTso(string[] files)
        {
            foreach (string file in files)
            {
                if (Path.GetExtension(files[0]).ToUpper() != ".TSO")
                    continue;

                if (tvMerge.Nodes.Find(file, false).Length == 0)
                {
                    TreeNode node = tvMerge.Nodes.Add(file);
                    node.Name = file;
                    node.Checked = true;

                    TSOFile tso = new TSOFile(file);
                    tso.ReadAll();

                    foreach (TSOMesh j in tso.meshes)
                    {
                        TreeNode mesh = node.Nodes.Add(j.Name);
                        mesh.Name = j.Name;
                        mesh.Checked = true;
                    }
                }
            }
        }

        private void btnMerge_Click(object sender, EventArgs e)
        {
            Color c = tabPage2.BackColor;

            try
            {
                tabPage2.BackColor = Color.Tomato;
                List<TSOMesh> meshes = new List<TSOMesh>();
                Dictionary<string, Pair<TSOMaterial, int>> materialmap = new Dictionary<string, Pair<TSOMaterial, int>>();
                Dictionary<string, TSOTex> textures = new Dictionary<string, TSOTex>();
                TSOFile last = null;

                foreach (TreeNode node in tvMerge.Nodes)
                {
                    TSOFile tso = new TSOFile(node.Text);
                    last = tso;
                    ulong mtls = 0;
                    ulong mask = 1;
                    tso.ReadAll();

                    foreach (TSOMesh mesh in tso.meshes)
                    {
                        TreeNode[] found = node.Nodes.Find(mesh.Name, false);

                        if (found.Length == 0 || !found[0].Checked)
                            continue;

                        foreach (TSOSubMesh k in mesh.sub_meshes)
                            mtls |= 1ul << k.spec;

                        meshes.Add(mesh);
                    }

                    foreach (TSOMaterial mat in tso.materials)
                    {
                        if ((mask & mtls) != 0)
                        {
                            if (!materialmap.ContainsKey(mat.Name))
                            {
                                Pair<TSOMaterial, int> value = new Pair<TSOMaterial, int>(mat, materialmap.Count);
                                materialmap.Add(mat.Name, value);

                                if (!textures.ContainsKey(mat.ColorTex))
                                {
                                    TSOTex tex = tso.texturemap[mat.ColorTex];
                                    textures.Add(tex.Name, tex);
                                }

                                if (!textures.ContainsKey(mat.ShadeTex))
                                {
                                    TSOTex tex = tso.texturemap[mat.ShadeTex];
                                    textures.Add(tex.Name, tex);
                                }
                            }
                        }

                        mask <<= 1;
                    }
                }

                using (FileStream fs = File.OpenWrite(tbMergeTso.Text))
                {
                    fs.SetLength(0);

                    List<TSOTex> texlist = new List<TSOTex>(textures.Values);
                    TSOMaterial[] mtllist = new TSOMaterial[materialmap.Count];

                    foreach (var i in materialmap.Values)
                        mtllist[i.Second] = i.First;

                    foreach (TSOMesh mesh in meshes)
                    {
                        foreach (TSOSubMesh sub in mesh.sub_meshes)
                        {
                            TSOMaterial mtl = mesh.file.materials[sub.spec];
                            sub.spec = materialmap[mtl.Name].Second;
                        }
                    }

                    foreach (TSOTex tex in texlist)
                        TSOFile.ExchangeChannel(tex.data, tex.depth);

                    BinaryWriter bw = new BinaryWriter(fs);
                    TSOWriter.WriteHeader(bw);
                    TSOWriter.Write(bw, last.nodes);
                    TSOWriter.Write(bw, texlist.ToArray());
                    TSOWriter.Write(bw, last.effects);
                    TSOWriter.Write(bw, mtllist);
                    TSOWriter.Write(bw, meshes.ToArray());
                }
            }
            catch (Exception exception)
            {
                Util.ProcessError(exception);
            }
            finally
            {
                tabPage2.BackColor = c;
            }
        }

        private void btnMergeAdd_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Filter = "TSO File(*.tso)|*.tso";
                dlg.Multiselect = true;

                if (dlg.ShowDialog() == DialogResult.OK)
                    AddMergeTso(dlg.FileNames);
            }
            catch (Exception exception)
            {
                Util.ProcessError(exception);
            }
        }

        private void btnMergeDel_Click(object sender, EventArgs e)
        {
            if (tvMerge.SelectedNode != null && tvMerge.SelectedNode.Level == 0)
                tvMerge.SelectedNode.Remove();
        }

        private void btnMergeReset_Click(object sender, EventArgs e)
        {
            tvMerge.Nodes.Clear();
        }

        private void btnRefMergeTso_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = "TSO File (*.tso)|*.tso";
                dlg.FileName = tbMergeTso.Text;

                if (dlg.ShowDialog() == DialogResult.OK)
                    tbMergeTso.Text = dlg.FileName;
            }
            catch (Exception exception)
            {
                Util.ProcessError(exception);
            }
        }

        public static bool bTvMerge_AfterCheck = false;

        private void tvMerge_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (bTvMerge_AfterCheck)
                return;

            bTvMerge_AfterCheck = true;

            try
            {
                if (e.Node.Level == 0)
                {
                    foreach (TreeNode node in e.Node.Nodes)
                        node.Checked = e.Node.Checked;
                }
                else
                {
                    bool check = false;

                    foreach (TreeNode node in e.Node.Parent.Nodes)
                        if (node.Checked) check = true;

                    e.Node.Parent.Checked = check;
                }
            }
            finally
            {
                bTvMerge_AfterCheck = false;
            }
        }
        #endregion
    }

    public class Util
    {
        public static void ProcessError(Exception exception)
        {
            MessageBox.Show(exception.ToString());
        }
    }
}
