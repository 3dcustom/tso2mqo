using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Tso2MqoGui
{
    public partial class FormMaterial : Form
    {
        public Dictionary<string, MaterialInfo> materials;

        public FormMaterial()
        {
            InitializeComponent();
            DialogResult = DialogResult.Cancel;
        }

        private void FormMaterial_Load(object sender, EventArgs e)
        {
            foreach (MaterialInfo mat_info in materials.Values)
            {
                ListViewItem item = lvMaterials.Items.Add(mat_info.Name);
                item.Tag = mat_info;
                item.SubItems.Add(mat_info.ColorTexture ?? "");
                item.SubItems.Add(mat_info.ShadeTexture ?? "");
                item.SubItems.Add(mat_info.ShaderFile ?? "");
            }
        }

        private void bOk_Click(object sender, EventArgs e)
        {
            string error_message = null;

            // 正しく情報が設定されているかをチェックする
            foreach (ListViewItem item in lvMaterials.Items)
            {
                string material_name = item.Text;

                for (int i = 1; i < 4; i++)
                {
                    string column_name = lvMaterials.Columns[i].Text;
                    string text = item.SubItems[i].Text;

                    if (text == "")
                        error_message = string.Format("マテリアル名 {0} の {1} を設定する必要があります。", material_name, column_name);
                    else if (!File.Exists(text))
                        error_message = string.Format("マテリアル名 {0} の {1} は存在しません。", material_name, column_name);

                    if (error_message != null)
                    {
                        MessageBox.Show(error_message);
                        item.Selected = true;
                        return;
                    }
                }
            }

            DialogResult = DialogResult.OK;
            Hide();
        }

        private void bCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Hide();
        }

        private void lvMaterials_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvMaterials.SelectedItems.Count > 0)
            {
                MaterialInfo mat_info = lvMaterials.SelectedItems[0].Tag as MaterialInfo;
                pgMaterial.SelectedObject = mat_info;
            }
            else
            {
                pgMaterial.SelectedObject = null;
            }
        }

        private void pgMaterial_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            if (lvMaterials.SelectedItems.Count > 0)
            {
                ListViewItem item = lvMaterials.SelectedItems[0];

                switch (e.ChangedItem.PropertyDescriptor.Name)
                {
                    case "ColorTexture":
                        item.SubItems[1].Text = e.ChangedItem.Value.ToString();
                        break;
                    case "ShadeTexture":
                        item.SubItems[2].Text = e.ChangedItem.Value.ToString();
                        break;
                    case "ShaderFile":
                        item.SubItems[3].Text = e.ChangedItem.Value.ToString();
                        break;
                }
            }
        }
    }
}
