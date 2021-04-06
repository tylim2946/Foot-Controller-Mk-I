using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FC_Options
{
    public partial class ProfOpt : Form
    {
        public TextBox MyTxtName
        {
            get { return TxtName; }
        }

        public ProfOpt()
        {
            InitializeComponent();
        }
        
        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (TxtName.Text.Length > 0)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                Warning_InvalidValue warning = new Warning_InvalidValue();
                warning.ShowDialog();
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
