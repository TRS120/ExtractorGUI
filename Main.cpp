using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;

namespace ScsExtractorGui
{
    public class MainForm : Form
    {
        private TextBox txtPath;
        private TextBox txtLog;
        private CheckBox chkDeep, chkAll, chkSeparate, chkSkip;
        private Button btnBrowse, btnStart;

        public MainForm()
        {
            // Window Settings
            this.Text = "SCS Extractor GUI";
            this.Size = new Size(600, 500);
            this.Font = new Font("Segoe UI", 9);

            // Path Selection
            Label lbl = new Label { Text = "Target File or Folder:", Location = new Point(20, 20), Size = new Size(150, 20) };
            txtPath = new TextBox { Location = new Point(20, 45), Size = new Size(440, 25) };
            btnBrowse = new Button { Text = "Browse", Location = new Point(470, 44), Size = new Size(90, 27) };
            btnBrowse.Click += (s, e) => {
                using (OpenFileDialog ofd = new OpenFileDialog { Filter = "SCS Files|*.scs|All Files|*.*" })
                {
                    if (ofd.ShowDialog() == DialogResult.OK) txtPath.Text = ofd.FileName;
                }
            };

            // Options
            chkDeep = new CheckBox { Text = "Deep Mode (--deep)", Location = new Point(20, 85), AutoSize = true };
            chkAll = new CheckBox { Text = "Extract All (--all)", Location = new Point(180, 85), AutoSize = true };
            chkSeparate = new CheckBox { Text = "Separate Folders (--separate)", Location = new Point(20, 115), AutoSize = true };
            chkSkip = new CheckBox { Text = "Skip Existing (--skip-existing)", Location = new Point(180, 115), AutoSize = true };

            // Log Output
            txtLog = new TextBox { Location = new Point(20, 150), Size = new Size(540, 220), Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.Black, ForeColor = Color.LightGray };

            // Start Button
            btnStart = new Button { Text = "START EXTRACTION", Location = new Point(20, 385), Size = new Size(540, 45), BackColor = Color.DarkSlateBlue, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnStart.Click += RunExtractor;

            this.Controls.AddRange(new Control[] { lbl, txtPath, btnBrowse, chkDeep, chkAll, chkSeparate, chkSkip, txtLog, btnStart });
        }

        private async void RunExtractor(object sender, EventArgs e)
        {
            string target = txtPath.Text;
            if (string.IsNullOrEmpty(target)) return;

            // Build arguments
            string args = $"\"{target}\"";
            if (chkDeep.Checked) args += " --deep";
            if (chkAll.Checked) args += " --all";
            if (chkSeparate.Checked) args += " --separate";
            if (chkSkip.Checked) args += " --skip-existing";

            btnStart.Enabled = false;
            txtLog.Clear();
            txtLog.AppendText($"Running: extractor.exe {args}\r\n\r\n");

            await System.Threading.Tasks.Task.Run(() => {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "extractor.exe",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (Process p = Process.Start(psi))
                {
                    while (!p.StandardOutput.EndOfStream)
                    {
                        string line = p.StandardOutput.ReadLine();
                        this.Invoke(new Action(() => txtLog.AppendText(line + Environment.NewLine)));
                    }
                    p.WaitForExit();
                }
            });

            btnStart.Enabled = true;
            MessageBox.Show("Extraction Finished!");
        }

        [STAThread]
        static void Main() { Application.EnableVisualStyles(); Application.Run(new MainForm()); }
    }
}
