using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using InputManager;
using System.Collections;
using InTheHand.Net.Sockets;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace FC_Options
{
    /* 
     * Tips
     *  Double click an item to modify it
     *  ctrl+z supported!
     *  Use different profile for different games
     *  Press printscreen to relocate reference axes
     *  Device is disabled while editing profiles
     *      
     *  Features
     *      ctrl + z support
     *      start as tray (Hide at start up)
     *          Auto connect
     *      one app at a time (show already opened window) (single instance application)
     *          or one app per device
     *      Set title to the name of connected device
     *          'Disconnected' if not connected
     *      Set notifyIcon1 text to selected profile and device name
     *        HC-06
     *        The Witcher 3
     *      //Disconnect when reconnect
     *          close serial conection
                kill thread
            //Close serial connection when disconnected
                Use exception to detect disconnection
                What if it happened by an accident?
                    reconnect?
     *      
     *  Problems (Bugs)
     *  
     */

    //startup => different application to install and setup startup (install.exe & FC Options.exe)

    public partial class FormFCOptions : Form
    {
        //RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

        public static string docPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FC Options");

        public static BluetoothClient btc;
        BluetoothListener btl;

        Thread clk; //clock

        int? ry = null, rp = null, rr = null; //reference yaw, pitch, roll

        public static int yaw, pitch, roll;

        public static readonly object _lock = new object();
        public static readonly object _lock_profile = new object();
        public static readonly object _lock_settings = new object();

        public static int? assignOptRowIndex = null;

        const String separator = "($€₱)";
        public static String[][] profKeys = new string[0][];

        public FormFCOptions()
        {
            InitializeComponent();

            FirstStart();
            Initialize();
            SetupKeyboardHooks();
            CmbProf.SelectedItem = ReadSetting("profile", "(default)");
            Test();
        }

        void Initialize()
        {
            GridKeys.Columns[1].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            GridKeys.Columns[2].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            GridKeys.Columns[3].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            GridKeys.Columns[4].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            GridKeys.Columns[5].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            GridKeys.Columns[6].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            GridKeys.Columns[7].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            GridKeys.Columns[8].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;

            RefreshProfiles();
        }

        void FirstStart()
        {
            //if does not exist
            //regKey.SetValue("FC_Options", Application.ExecutablePath.ToString());
            //regKey.DeleteValue("FC_Options", false); //delete when requested
        }

        void Test()
        {
            //currValue.Text = "hello";
        }

        private void CmbProf_DropDown(object sender, EventArgs e)
        {
            RefreshProfiles();
        }

        private void CmbProf_SelectedIndexChanged(object sender, EventArgs e)
        {
            SaveSetting("profile", CmbProf.Text);
            RefreshProfile();
        }

        private void GridKeys_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            isEditing = true;

            if (e.RowIndex >= 0)
            {
                assignOptRowIndex = e.RowIndex;
                AssignOpt assign = new AssignOpt();
                DialogResult dr = assign.ShowDialog();

                if (dr == DialogResult.OK)
                {
                    string source = Path.Combine(docPath, "Profiles", CmbProf.Text);

                    string str = assign.MyTxtName.Text + separator + assign.MyTxtKey.Text + separator + assign.MyNudYawMin.Text + separator + assign.MyNudYawMax.Text + separator + assign.MyNudPitchMin.Text + separator + assign.MyNudPitchMax.Text + separator + assign.MyNudRollMin.Text + separator + assign.MyNudRollMax.Text + separator + assign.MyNudDelay.Value.ToString();

                    if (File.Exists(source))
                    {
                        ArrayList lines = new ArrayList(File.ReadAllLines(source));

                        string[] myLines = new string[lines.Count];

                        lines.RemoveAt((int)assignOptRowIndex);
                        lines.Insert((int)assignOptRowIndex, str);

                        lines.CopyTo(myLines);

                        OverwriteFile(docPath + "\\Profiles", CmbProf.Text, myLines);
                    }

                    SaveSetting("profile", CmbProf.Text);
                    RefreshProfiles();

                    GridKeys.ClearSelection();
                    GridKeys.Rows[(int)assignOptRowIndex].Selected = true;
                }
            }

            isEditing = false;
        }

        private void GridKeys_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            //ctrl+z support? => keep data in the list

            string source = Path.Combine(docPath, "Profiles", CmbProf.Text);

            if (File.Exists(source))
            {
                ArrayList lines = new ArrayList(File.ReadAllLines(source));

                if (lines.Count == 1)
                {
                    OverwriteFile(docPath + "\\Profiles", CmbProf.Text, new string[0]);
                }
                else
                {
                    string[] myLines = new string[lines.Count - 1];

                    lines.RemoveAt(e.Row.Index);
                    lines.CopyTo(myLines);

                    OverwriteFile(docPath + "\\Profiles", CmbProf.Text, myLines);
                }
            }
        }

        private void BtnAddProf_Click(object sender, EventArgs e)
        {
            isEditing = true;

            ProfOpt addProf = new ProfOpt();
            DialogResult dr = addProf.ShowDialog();

            if (dr == DialogResult.OK)
            {
                int i = CreateFile(docPath + "\\Profiles", addProf.MyTxtName.Text, new string[0]);
                string item = "";

                if (i == 0)
                {
                    item = addProf.MyTxtName.Text;
                }
                else
                {
                    item = addProf.MyTxtName.Text + " (" + i + ")";
                }

                SaveSetting("profile", item);
                RefreshProfiles();
            }

            isEditing = false;
        }

        private void BtnAssignKey_Click(object sender, EventArgs e)
        {
            isEditing = true;

            if (CmbProf.SelectedItem == null)
            {
                Warning_SelectProf warning = new Warning_SelectProf();
                warning.ShowDialog();
            }
            else
            {
                assignOptRowIndex = null;
                AssignOpt assign = new AssignOpt();
                DialogResult dr = assign.ShowDialog();

                if (dr == DialogResult.OK)
                {
                    string[] str = { assign.MyTxtName.Text + separator + assign.MyTxtKey.Text + separator + assign.MyNudYawMin.Text + separator + assign.MyNudYawMax.Text + separator + assign.MyNudPitchMin.Text + separator + assign.MyNudPitchMax.Text + separator + assign.MyNudRollMin.Text + separator + assign.MyNudRollMax.Text + separator + assign.MyNudDelay.Value.ToString() };

                    WriteFile(docPath + "\\Profiles", CmbProf.Text, str);

                    SaveSetting("profile", CmbProf.Text);
                    RefreshProfiles();
                }
            }

            isEditing = false;
        }

        private void BtnDevice_Click(object sender, EventArgs e)
        {
            isEditing = true;

            DeviceOpt port = new DeviceOpt();
            DialogResult dr = port.ShowDialog();

            if (dr == DialogResult.OK)
            {
                clk = new Thread(new ThreadStart(ThreadJob));
                clk.IsBackground = true;
                StopThread();

                btl = new BluetoothListener(Guid.NewGuid());
                btl.Start();

                StartThread();

                SaveSetting("device_index", port.MyCmbDevice.SelectedIndex);
                SaveSetting("device_name", port.MyCmbDevice.SelectedValue);
            }

            isEditing = false;
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            HideForm();
        }

        private void FormFCOptions_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideForm();
            }
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            isEditing = true;

            if (CmbProf.SelectedItem == null)
            {
                Warning_SelectProf warning = new Warning_SelectProf();
                warning.ShowDialog();
            }
            else if (CmbProf.Text.Equals("(default)"))
            {
                //warning_cannot remove the selected profile
            }
            else
            {
                CfrmRemove cfrm = new CfrmRemove();
                DialogResult dr = cfrm.ShowDialog();

                if (dr == DialogResult.OK && cfrm.isConfirmed)
                {
                    DeleteFile(docPath + "\\Profiles", CmbProf.Text);
                    CmbProf.SelectedIndex = 0;
                }
            }

            isEditing = false;
        }

        private void BtnRef_Click(object sender, EventArgs e)
        {
            ry = null;
            rp = null;
            rr = null;
        }

        private GlobalKeyboardHook _globalKeyboardHook;

        public void SetupKeyboardHooks()
        {
            _globalKeyboardHook = new GlobalKeyboardHook();
            _globalKeyboardHook.KeyboardPressed += OnKeyPressed;
        }

        private void OnKeyPressed(object sender, GlobalKeyboardHookEventArgs e)
        {
            if (e.KeyboardData.VirtualCode != GlobalKeyboardHook.VkSnapshot)
                return;

            if (e.KeyboardState == GlobalKeyboardHook.KeyboardState.KeyDown)
            {
                ry = null;
                rp = null;
                rr = null;
                e.Handled = false;
            }
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowForm();
            }
            else if (e.Button == MouseButtons.Right)
            {
                contextMenuStrip1.Show(Cursor.Position.X, Cursor.Position.Y);
            }
        }

        private void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem.Name == "exitToolStripMenuItem")
            {
                Application.Exit();
            }
        }

        public static int CreateFile(string path, string fileName, string[] content)
        {
            string source = Path.Combine(path, fileName);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (!File.Exists(source))
            {
                using (FileStream fs = new FileStream(source, FileMode.Create))
                {
                    using (TextWriter tw = new StreamWriter(fs))
                    {
                        foreach (string s in content)
                        {
                            tw.WriteLine(s);
                        }
                    }
                }

                return 0;
            }
            else
            {
                lock (_lock)
                {
                    int i = 0;

                    while (File.Exists(source))
                    {
                        i++;
                        source = Path.Combine(path, fileName + " (" + i + ")");
                    }

                    CreateFile(path, fileName + " (" + i + ")", content);

                    return i;
                }
            }
        }

        public static int WriteFile(string path, string fileName, string[] content)
        {
            string source = Path.Combine(path, fileName);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (!File.Exists(source))
            {
                int i = CreateFile(path, fileName, content);

                return i;
            }
            else
            {
                using (FileStream fs = new FileStream(Path.Combine(path, fileName), FileMode.Append))
                {
                    using (TextWriter tw = new StreamWriter(fs))
                    {
                        foreach (string s in content)
                        {
                            tw.WriteLine(s);
                        }
                    }
                }

                return 0;
            }
        }

        public static void DeleteFile(string path, string fileName)
        {
            string source = System.IO.Path.Combine(path, fileName);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (File.Exists(source))
            {
                File.Delete(source);
            }
        }

        public static void OverwriteFile(string path, string fileName, string[] content)
        {
            string source = Path.Combine(path, fileName);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            using (FileStream fs = new FileStream(source, FileMode.Create))
            {
                using (TextWriter tw = new StreamWriter(fs))
                {
                    foreach (string s in content)
                    {
                        tw.WriteLine(s);
                    }
                }
            }
        }

        public static void SaveSetting(string key, object value)
        {
            string source = Path.Combine(docPath, "settings");
            bool ok = false;

            if (!Directory.Exists(docPath))
            {
                Directory.CreateDirectory(docPath);
            }

            if (File.Exists(source))
            {
                string[] lines = File.ReadAllLines(source);

                using (FileStream fs = File.Open(source, FileMode.Open))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        string myKey = "";

                        for (int i = 0; i < lines.Length; i++)
                        {
                            myKey = lines[i].Substring(0, lines[i].IndexOf(' '));

                            if (myKey.Equals(key))
                            {
                                lines[i] = myKey + " " + value;
                                ok = true;
                                break;
                            }
                        }
                    }
                }

                if (ok)
                {
                    OverwriteFile(docPath, "settings", lines);
                }
                else
                {
                    string[] str = { key + " " + value };
                    WriteFile(docPath, "settings", str);
                }
            }
            else
            {
                CreateFile(docPath, "settings", new string[] { key + " " + value });
            }
        }

        public static string ReadSetting(string key, object defaultValue)
        {
            string source = Path.Combine(docPath, "settings");

            if (!Directory.Exists(docPath))
            {
                Directory.CreateDirectory(docPath);
            }

            if (File.Exists(source))
            {
                using (FileStream fs = File.Open(source, FileMode.Open))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        string line = null;
                        string myKey = "";
                        string myValue = "";

                        do
                        {
                            line = sr.ReadLine();

                            if (line == null)
                            {
                                break;
                            }

                            myKey = line.Substring(0, line.IndexOf(' '));
                            myValue = line.Substring(line.IndexOf(' ') + 1);

                            if (myKey.Equals(key))
                            {
                                return myValue;
                            }

                        } while (true);
                    }
                }

                SaveSetting(key, defaultValue);
                return defaultValue.ToString();
            }
            else
            {
                SaveSetting(key, defaultValue);
                return defaultValue.ToString();
            }
        }

        void RefreshProfiles()
        {
            CmbProf.Items.Clear();

            if (!Directory.Exists(docPath + "\\Profiles"))
            {
                Directory.CreateDirectory(docPath + "\\Profiles");
            }

            if (Directory.GetFiles(docPath + "\\Profiles").Count() == 0)
            {
                string[] profDefault = {
                            "Forward" + separator + "W" + separator + "0" + separator + "0" + separator + "20" + separator + "3" + separator + "0" + separator + "0" + separator + "0",
                            "Left" + separator + "A" + separator + "0" + separator + "0" + separator + "0" + separator + "0" + separator + "-10" + separator + "3" + separator + "0",
                            "Backward" + separator + "S" + separator + "0" + separator + "0" + separator + "-20" + separator + "3" + separator + "0" + separator + "0" + separator +  "0",
                            "Right" + separator + "D" + separator + "0" + separator + "0" + separator + "0" + separator + "0" + separator + "10" + separator + "3" + separator + "0",
                            "Aim" + separator + "{mouse_right}" + separator + "0" + separator + "0" + separator + "0" + separator + "0" + separator + "0" + separator + "0" + separator + "0",
                            "Grenade" + separator + "G" + separator + "0" + separator + "0" + separator + "0" + separator + "0" + separator + "0" + separator + "0" + separator + "-1"};
                CreateFile(docPath + "\\Profiles", "(default)", profDefault);
                CmbProf.Items.Add("(default)");
                CmbProf.SelectedItem = "(default)";
            }
            else
            {
                foreach (string s in Directory.EnumerateFiles(docPath + "\\Profiles"))
                {
                    string[] split = s.Split('\\');
                    string result = split[split.Length - 1];
                    CmbProf.Items.Add(result);
                }

                CmbProf.SelectedItem = ReadSetting("profile", "(default)");
            }
        }

        private void RefreshProfile()
        {
            string source = Path.Combine(docPath + "\\Profiles", CmbProf.Text);
            List<string[]> profKeysList = new List<string[]>();

            if (File.Exists(source))
            {
                GridKeys.Rows.Clear();
                profKeysList.Clear();

                using (FileStream fs = File.Open(source, FileMode.Open))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        string line = null;
                        do
                        {
                            line = sr.ReadLine();
                            if (line == null)
                            {
                                break;
                            }
                            string[] items = line.Split(new string[] { separator }, StringSplitOptions.None);
                            GridKeys.Rows.Add(items);

                            profKeysList.Add(items);

                        } while (true);
                    }
                }
                if (profKeysList.Count() > 0)
                {
                    profKeys = profKeysList.ToArray();
                }
            }
        }

        void ShowForm()
        {
            Show();
            notifyIcon1.Visible = false;
        }

        void HideForm()
        {
            Hide();
            notifyIcon1.Visible = true;
        }

        void StartThread()
        {
            clk.Start();
        }

        void StopThread()
        {
            if (clk.IsAlive)
            {
                clk.Abort();
            }
        }

        void SetReferenceAxes()
        {
            ry = Int32.Parse(values[0]);
            rp = Int32.Parse(values[1]);
            rr = Int32.Parse(values[2]);
        }

        Keys ConvertNumpadKey(string key)
        {
            if (key.Contains("num"))
            {
                switch (key[key.Length - 1])
                {
                    case '0':
                        return Keys.NumPad0;
                    case '1':
                        return Keys.NumPad1;
                    case '2':
                        return Keys.NumPad2;
                    case '3':
                        return Keys.NumPad3;
                    case '4':
                        return Keys.NumPad4;
                    case '5':
                        return Keys.NumPad5;
                    case '6':
                        return Keys.NumPad6;
                    case '7':
                        return Keys.NumPad7;
                    case '8':
                        return Keys.NumPad8;
                    case '9':
                        return Keys.NumPad9;
                    default:
                        return (Keys)Enum.Parse(typeof(Keys), key, true);
                }
            }
            else
            {
                return (Keys)Enum.Parse(typeof(Keys), key, true); ;
            }
        }

        string value;
        string[] values;

        string lastSelectedKey = null;
        string currentKey = null;

        bool isEditing = false;

        void ThreadJob()
        {
            try
            {
                StreamReader peer = new StreamReader(btc.GetStream());



                while (true)
                {
                    value = peer.ReadLine();


                    values = value.Split(' '); //not works when getting blank space

                    if (values.Length == 3)
                    {

                        currValue.Invoke((MethodInvoker)delegate
                        {
                            // Running on the UI thread
                            currValue.Text = value
                                + " " + yaw.ToString() + " " + pitch.ToString() + " " + roll.ToString()
                                + " " + ry.ToString() + " " + rp.ToString() + " " + rr.ToString();
                        });

                        if (ry == null || rp == null || rr == null)
                        {
                            SetReferenceAxes();
                        }
                        else
                        {

                            yaw = (Int32.Parse(values[0]) - (int)ry);
                            pitch = (Int32.Parse(values[1]) - (int)rp);
                            roll = (Int32.Parse(values[2]) - (int)rr);

                            //Console.WriteLine(profKeys[0][0]);

                            lastSelectedKey = null;

                            foreach (String[] sa in profKeys)
                            {
                                //sa[0] = name 
                                //sa[1] = key
                                //Int32.Parse(sa[2]) = ymin
                                //Int32.Parse(sa[3]) = ymax 
                                //Int32.Parse(sa[4]) = pmin 
                                //Int32.Parse(sa[5]) = pmax 
                                //Int32.Parse(sa[6]) = rmin 
                                //Int32.Parse(sa[7]) = rmax 
                                //Int32.Parse(sa[8]) = delay 

                                if (yaw >= Int32.Parse(sa[2]) && yaw <= Int32.Parse(sa[3]) &&
                                    pitch >= Int32.Parse(sa[4]) && pitch <= Int32.Parse(sa[5]) &&
                                    roll >= Int32.Parse(sa[6]) && roll <= Int32.Parse(sa[7]) &&
                                    lastSelectedKey == null)
                                {
                                    lastSelectedKey = sa[1];
                                }
                            }

                            if (lastSelectedKey != currentKey && !isEditing)
                            {
                                
                                if (lastSelectedKey != null && currentKey == null)
                                {
                                    //Keyboard.KeyDown((Keys)Enum.Parse(typeof(Keys), lastSelectedKey, true));
                                    Keyboard.KeyDown(ConvertNumpadKey(lastSelectedKey));

                                    Console.WriteLine(lastSelectedKey + " down");
                                }
                                else if (lastSelectedKey == null && currentKey != null)
                                {
                                    //Keyboard.KeyUp((Keys)Enum.Parse(typeof(Keys), currentKey, true));
                                    Keyboard.KeyUp(ConvertNumpadKey(currentKey));

                                    Console.WriteLine(currentKey + " up");
                                }
                                else if (lastSelectedKey != null && currentKey != null)
                                {
                                    //Keyboard.KeyUp((Keys)Enum.Parse(typeof(Keys), currentKey, true));
                                    Keyboard.KeyUp(ConvertNumpadKey(currentKey));
                                    //Keyboard.KeyDown((Keys)Enum.Parse(typeof(Keys), lastSelectedKey, true));
                                    Keyboard.KeyUp(ConvertNumpadKey(lastSelectedKey));

                                    Console.WriteLine(lastSelectedKey + " down");
                                    Console.WriteLine(currentKey + " up");
                                    
                                    //동시에 안눌림 >>> KeyPress && delay 10ms && if it is already happening, skip
                                }

                                currentKey = lastSelectedKey;
                            }








                            //if i == length + 1 >>> 해당안됨 null

                            //분리 >>> name[] yaw[] pitch[] roll[] key[] continuous[]
                            //for (int i = 0;i < name.length;i++)
                            //{
                            //if(yaw <= value + allowance && yaw >= value - allowance)
                            //}






                            //activated = null; //null if nothing, name if something //continuous check








                            //continuous check

                            //if (rxString != null)
                            //{
                            //    if ((activated.equals("W") == Continuous)
                            //    {
                            //        Keyboard.KeyDown(Keys.W);
                            //        activated = "W"
                            //    }
                            //}

                            //read profile and search for values
                        }
                    }

                    //Thread.Sleep(500);

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine(value);
                //case disconnected:
                //      delay(2000);
                //      btc.reconnect();
                //      if timeout disconnect();
            }
        }

        class GlobalKeyboardHookEventArgs : HandledEventArgs
        {
            public GlobalKeyboardHook.KeyboardState KeyboardState { get; private set; }
            public GlobalKeyboardHook.LowLevelKeyboardInputEvent KeyboardData { get; private set; }

            public GlobalKeyboardHookEventArgs(
                GlobalKeyboardHook.LowLevelKeyboardInputEvent keyboardData,
                GlobalKeyboardHook.KeyboardState keyboardState)
            {
                KeyboardData = keyboardData;
                KeyboardState = keyboardState;
            }
        }
        //Based on https://gist.github.com/Stasonix
        class GlobalKeyboardHook : IDisposable
        {
            public event EventHandler<GlobalKeyboardHookEventArgs> KeyboardPressed;

            public GlobalKeyboardHook()
            {
                _windowsHookHandle = IntPtr.Zero;
                _user32LibraryHandle = IntPtr.Zero;
                _hookProc = LowLevelKeyboardProc; // we must keep alive _hookProc, because GC is not aware about SetWindowsHookEx behaviour.

                _user32LibraryHandle = LoadLibrary("User32");
                if (_user32LibraryHandle == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode, $"Failed to load library 'User32.dll'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
                }



                _windowsHookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, _user32LibraryHandle, 0);
                if (_windowsHookHandle == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode, $"Failed to adjust keyboard hooks for '{Process.GetCurrentProcess().ProcessName}'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
                }
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    // because we can unhook only in the same thread, not in garbage collector thread
                    if (_windowsHookHandle != IntPtr.Zero)
                    {
                        if (!UnhookWindowsHookEx(_windowsHookHandle))
                        {
                            int errorCode = Marshal.GetLastWin32Error();
                            throw new Win32Exception(errorCode, $"Failed to remove keyboard hooks for '{Process.GetCurrentProcess().ProcessName}'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
                        }
                        _windowsHookHandle = IntPtr.Zero;

                        // ReSharper disable once DelegateSubtraction
                        _hookProc -= LowLevelKeyboardProc;
                    }
                }

                if (_user32LibraryHandle != IntPtr.Zero)
                {
                    if (!FreeLibrary(_user32LibraryHandle)) // reduces reference to library by 1.
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        throw new Win32Exception(errorCode, $"Failed to unload library 'User32.dll'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
                    }
                    _user32LibraryHandle = IntPtr.Zero;
                }
            }

            ~GlobalKeyboardHook()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private IntPtr _windowsHookHandle;
            private IntPtr _user32LibraryHandle;
            private HookProc _hookProc;

            delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

            [DllImport("kernel32.dll")]
            private static extern IntPtr LoadLibrary(string lpFileName);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
            private static extern bool FreeLibrary(IntPtr hModule);

            /// <summary>
            /// The SetWindowsHookEx function installs an application-defined hook procedure into a hook chain.
            /// You would install a hook procedure to monitor the system for certain types of events. These events are
            /// associated either with a specific thread or with all threads in the same desktop as the calling thread.
            /// </summary>
            /// <param name="idHook">hook type</param>
            /// <param name="lpfn">hook procedure</param>
            /// <param name="hMod">handle to application instance</param>
            /// <param name="dwThreadId">thread identifier</param>
            /// <returns>If the function succeeds, the return value is the handle to the hook procedure.</returns>
            [DllImport("USER32", SetLastError = true)]
            static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);

            /// <summary>
            /// The UnhookWindowsHookEx function removes a hook procedure installed in a hook chain by the SetWindowsHookEx function.
            /// </summary>
            /// <param name="hhk">handle to hook procedure</param>
            /// <returns>If the function succeeds, the return value is true.</returns>
            [DllImport("USER32", SetLastError = true)]
            public static extern bool UnhookWindowsHookEx(IntPtr hHook);

            /// <summary>
            /// The CallNextHookEx function passes the hook information to the next hook procedure in the current hook chain.
            /// A hook procedure can call this function either before or after processing the hook information.
            /// </summary>
            /// <param name="hHook">handle to current hook</param>
            /// <param name="code">hook code passed to hook procedure</param>
            /// <param name="wParam">value passed to hook procedure</param>
            /// <param name="lParam">value passed to hook procedure</param>
            /// <returns>If the function succeeds, the return value is true.</returns>
            [DllImport("USER32", SetLastError = true)]
            static extern IntPtr CallNextHookEx(IntPtr hHook, int code, IntPtr wParam, IntPtr lParam);

            [StructLayout(LayoutKind.Sequential)]
            public struct LowLevelKeyboardInputEvent
            {
                /// <summary>
                /// A virtual-key code. The code must be a value in the range 1 to 254.
                /// </summary>
                public int VirtualCode;

                /// <summary>
                /// A hardware scan code for the key. 
                /// </summary>
                public int HardwareScanCode;

                /// <summary>
                /// The extended-key flag, event-injected Flags, context code, and transition-state flag. This member is specified as follows. An application can use the following values to test the keystroke Flags. Testing LLKHF_INJECTED (bit 4) will tell you whether the event was injected. If it was, then testing LLKHF_LOWER_IL_INJECTED (bit 1) will tell you whether or not the event was injected from a process running at lower integrity level.
                /// </summary>
                public int Flags;

                /// <summary>
                /// The time stamp stamp for this message, equivalent to what GetMessageTime would return for this message.
                /// </summary>
                public int TimeStamp;

                /// <summary>
                /// Additional information associated with the message. 
                /// </summary>
                public IntPtr AdditionalInformation;
            }

            public const int WH_KEYBOARD_LL = 13;
            //const int HC_ACTION = 0;

            public enum KeyboardState
            {
                KeyDown = 0x0100,
                KeyUp = 0x0101,
                SysKeyDown = 0x0104,
                SysKeyUp = 0x0105
            }

            public const int VkSnapshot = 0x2c; //printscreen
                                                //const int VkLwin = 0x5b;
                                                //const int VkRwin = 0x5c;
                                                //const int VkTab = 0x09;
                                                //const int VkEscape = 0x18;
                                                //const int VkControl = 0x11;
            const int KfAltdown = 0x2000;
            public const int LlkhfAltdown = (KfAltdown >> 8);

            public IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
            {
                bool fEatKeyStroke = false;

                var wparamTyped = wParam.ToInt32();
                if (Enum.IsDefined(typeof(KeyboardState), wparamTyped))
                {
                    object o = Marshal.PtrToStructure(lParam, typeof(LowLevelKeyboardInputEvent));
                    LowLevelKeyboardInputEvent p = (LowLevelKeyboardInputEvent)o;

                    var eventArguments = new GlobalKeyboardHookEventArgs(p, (KeyboardState)wparamTyped);

                    EventHandler<GlobalKeyboardHookEventArgs> handler = KeyboardPressed;
                    handler?.Invoke(this, eventArguments);

                    fEatKeyStroke = eventArguments.Handled;
                }

                return fEatKeyStroke ? (IntPtr)1 : CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
            }
        }
    }
}