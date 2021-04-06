using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FC_Options
{
    public partial class DeviceOpt : Form
    {
        public ComboBox MyCmbDevice
        {
            get { return CmbDevice; }
        }

        public DeviceOpt()
        {
            InitializeComponent();

            //CmbDevice.SelectedItem = FormFCOptions.ReadSetting("device", "ArduinoFC");

            FormFCOptions.btc = new BluetoothClient();
            BluetoothDeviceInfo[] devices = FormFCOptions.btc.DiscoverDevices(8, true, true, false, false);

            CmbDevice.DataSource = devices;
            CmbDevice.DisplayMember = "DeviceName";
            CmbDevice.ValueMember = "DeviceAddress";

            if (devices[Int32.Parse(FormFCOptions.ReadSetting("device_index", "0"))].DeviceAddress.ToString() == FormFCOptions.ReadSetting("device_name", ""))
            {
                CmbDevice.SelectedIndex = Int32.Parse(FormFCOptions.ReadSetting("device_index", "0"));
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {

            if (CmbDevice.SelectedValue != null)
            {


                //1. Disconnect and then reconnect >> close thread while disconnected
                //3. If already connected, than leave it as is/show dialog >> do not close thread
                //4. Device was disconnected by itself (use try catch in thread to detect) >> close thread and wait for the reconnect
                //2. Connect >> start thread
                try
                {
                    FormFCOptions.btc.Connect(new BluetoothEndPoint((BluetoothAddress)CmbDevice.SelectedValue, BluetoothService.SerialPort));
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (SocketException err)
                {
                    Console.WriteLine(err);

                }



            }
            else
            {
                Warning_InvalidValue warning = new Warning_InvalidValue();
                warning.ShowDialog();
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            //FormFCOptions.btc.Close();
            this.Close();
        }
    }
}
