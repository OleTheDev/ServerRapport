using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using WUApiLib;
using System.Management;

namespace Server_Rapport_Checker
{
    public partial class Form1 : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private void Form1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        public Form1()
        {
            InitializeComponent();

            log.Select();
            log.SelectionAlignment = HorizontalAlignment.Center;
            log.DeselectAll();
        }

        private static string FormatBytes(long bytes)
        {
            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return String.Format("{0:0.##} {1}", dblSByte, Suffix[i]);
        }

        public void GetWindowsUpdates()
        {
            this.Invoke((MethodInvoker)(() =>
            {
                var updateSession = new UpdateSession();
                var updateSearcher = updateSession.CreateUpdateSearcher();
                updateSearcher.Online = false; //set to true if you want to search online
                try
                {
                    var searchResult = updateSearcher.Search("IsInstalled=0 And IsHidden=0");
                    if (searchResult.Updates.Count > 0)
                    {
                        log.AppendText("There are " + searchResult.Updates.Count + " updates available for installation");
                    }
                }
                catch (Exception ex)
                {
                    log.AppendText("ERROR: " + ex.Message);
                }
            }));
        }

        public void GetCPUTemp()
        {
            this.Invoke((MethodInvoker)(() =>
            {
                try
                {
                    Double CPUtprt = 0;
                    System.Management.ManagementObjectSearcher mos = new System.Management.ManagementObjectSearcher(@"root\WMI", "Select * From MSAcpi_ThermalZoneTemperature");
                    foreach (System.Management.ManagementObject mo in mos.Get())
                    {
                        CPUtprt = Convert.ToDouble(Convert.ToDouble(mo.GetPropertyValue("CurrentTemperature").ToString()) - 2732) / 10;
                        log.AppendText("CPU temp : " + CPUtprt.ToString() + " °C" + Environment.NewLine);
                    }
                } catch (Exception er)
                {
                    log.AppendText("Should not get CPU temp! Error: " + er);
                }
            }));
        }

        public void GetCPUUsage()
        {
            this.Invoke((MethodInvoker)(() =>
            {
                try
                {
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_PerfFormattedData_PerfOS_Processor");
                    var usageTotal = "None";
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var usage = obj["PercentProcessorTime"];
                        usageTotal = usage.ToString();
                    }
                    log.AppendText("CPU Usage: " + usageTotal + "%");
                } catch (Exception er)
                {
                    log.AppendText("Should not get CPU usage! Error: " + er);
                }
            }));
        }

        public void GetDiskSpace()
        {
            this.Invoke((MethodInvoker)(() =>
            {
                DriveInfo[] allDrives = DriveInfo.GetDrives();

                foreach (DriveInfo d in allDrives)
                {
                    log.AppendText("********************" + Environment.NewLine);
                    log.AppendText("Drive " + d.Name + Environment.NewLine);
                    log.AppendText("Drive type: " + d.DriveType + Environment.NewLine);
                    if (d.IsReady == true)
                    {
                        log.AppendText("Volume label: " + d.VolumeLabel + Environment.NewLine);
                        log.AppendText("File system: " + d.DriveFormat + Environment.NewLine);
                        log.AppendText("Available space to current user:" + FormatBytes(d.AvailableFreeSpace) + Environment.NewLine);
                        log.AppendText("Total available space: " + FormatBytes(d.TotalFreeSpace) + Environment.NewLine);
                        log.AppendText("Total size of drive: " + FormatBytes(d.TotalSize) + Environment.NewLine);
                    }
                    log.AppendText(Environment.NewLine + "********************");
                }
            }));
        }

        public async void StartCheck()
        {
            log.Clear();
            log.AppendText("Server Rapport started!" + Environment.NewLine);
            log.AppendText("Checking Disk space..." + Environment.NewLine);

            await Task.Run(() => GetDiskSpace());

            log.AppendText(Environment.NewLine + "All disks has been checked!" + Environment.NewLine);
            log.AppendText(Environment.NewLine + "Checking for Windows update..." + Environment.NewLine);

            await Task.Run(() => GetWindowsUpdates());

            log.AppendText(Environment.NewLine + "Windows update checked!");
            log.AppendText(Environment.NewLine + "Checking CPU..." + Environment.NewLine);

            await Task.Run(() => GetCPUTemp());
            await Task.Run(() => GetCPUUsage());

            log.AppendText(Environment.NewLine + "CPU Checked!");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            StartCheck();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
    }
}
