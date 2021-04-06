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
    public partial class AssignOpt : Form
    {
        public TextBox MyTxtName
        {
            get { return TxtName; }
        }

        public TextBox MyTxtKey
        {
            get { return TxtKey; }
        }

        public NumericUpDown MyNudYawMin
        {
            get { return NudYawMin; }
        }

        public NumericUpDown MyNudYawMax
        {
            get { return NudYawMax; }
        }

        public NumericUpDown MyNudPitchMin
        {
            get { return NudPitchMin; }
        }

        public NumericUpDown MyNudPitchMax
        {
            get { return NudPitchMax; }
        }

        public NumericUpDown MyNudRollMin
        {
            get { return NudRollMin; }
        }

        public NumericUpDown MyNudRollMax
        {
            get { return NudRollMax; }
        }

        public NumericUpDown MyNudDelay
        {
            get { return NudDelay; }
        }

        //detect sensor and keyboard if possible
        public AssignOpt()
        {
            InitializeComponent();

            if (FormFCOptions.assignOptRowIndex != null && FormFCOptions.assignOptRowIndex >= 0)
            {
                TxtName.Text = FormFCOptions.profKeys[(int)FormFCOptions.assignOptRowIndex][0];
                //receive key itself, not just string
                //detect what the user have pressed
                TxtKey.Text = FormFCOptions.profKeys[(int)FormFCOptions.assignOptRowIndex][1];
                NudYawMin.Text = FormFCOptions.profKeys[(int)FormFCOptions.assignOptRowIndex][2];
                NudYawMax.Text = FormFCOptions.profKeys[(int)FormFCOptions.assignOptRowIndex][3];
                NudPitchMin.Text = FormFCOptions.profKeys[(int)FormFCOptions.assignOptRowIndex][4];
                NudPitchMax.Text = FormFCOptions.profKeys[(int)FormFCOptions.assignOptRowIndex][5];
                NudRollMin.Text = FormFCOptions.profKeys[(int)FormFCOptions.assignOptRowIndex][6];
                NudRollMax.Text = FormFCOptions.profKeys[(int)FormFCOptions.assignOptRowIndex][7];
                NudDelay.Text = FormFCOptions.profKeys[(int)FormFCOptions.assignOptRowIndex][8];
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (TxtName.Text.Length > 0 &&
                TxtKey.Text.Length > 0 &&
                NudYawMin.Text.Length > 0 &&
                NudYawMax.Text.Length > 0 &&
                NudPitchMin.Text.Length > 0 &&
                NudPitchMax.Text.Length > 0 &&
                NudRollMin.Text.Length > 0 &&
                NudRollMax.Text.Length > 0 &&
                NudDelay.Text.Length > 0)
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

        int step = 0;
        int yaw1, yaw2, pitch1, pitch2, roll1, roll2;

        private void BtnDetect_Click(object sender, EventArgs e)
        {
            switch (step)
            {
                case 0:
                    label13.Text = "Select 1st point and click \'Detect\'";
                    step = 1;
                    break;

                case 1:
                    yaw1 = FormFCOptions.yaw;
                    pitch1 = FormFCOptions.pitch;
                    roll1 = FormFCOptions.roll;

                    label13.Text = "Select 2nd point and click \'Detect\'";
                    step = 2;
                    break;

                case 2:
                    yaw2 = FormFCOptions.yaw;
                    pitch2 = FormFCOptions.pitch;
                    roll2 = FormFCOptions.roll;

                    NudYawMin.Value = Math.Min(yaw1, yaw2);
                    NudPitchMin.Value = Math.Min(pitch1, pitch2);
                    NudRollMin.Value = Math.Min(roll1, roll2);
                    NudYawMax.Value = Math.Max(yaw1, yaw2);
                    NudPitchMax.Value = Math.Max(pitch1, pitch2);
                    NudRollMax.Value = Math.Max(roll1, roll2);

                    label13.Text = "Done";
                    step = 0;
                    break;
            }
        }
    }
}
