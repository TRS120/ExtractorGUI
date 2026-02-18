using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
namespace ScsExtractorGui
{
    public class MainForm : Form
    {
        private TextBox txtPath, txtOutPath, txtPartial, txtFilter, txtSalt, txtManual, txtLog;
        private CheckBox chkDeep, chkSeparate, chkSkip, chkRaw;
        private Button btnBrowse, btnOutBrowse, btnStart, btnStop;
        private ProgressBar progress;
        private Process? currentProcess;
        private Color BgColor = Color.FromArgb(32, 32, 32);
        private Color ControlBg = Color.FromArgb(45, 45, 48);
        private Color AccentColor = Color.FromArgb(0, 120, 215);
        private Color TextColor = Color.FromArgb(240, 240, 240);

        public MainForm()
        {
            this.Text = "Extractor GUI — Windows 11 Edition";
            this.Size = new Size(650, 920);
            this.BackColor = BgColor;
            this.ForeColor = TextColor;
            this.Font = new Font("Segoe UI Variable Display", 10);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormClosing += (s, e) => KillProcessTree();

            InitializeModernUI();
        }
        private void InitializeModernUI()
        {
            int left = 25, width = 580;
            AddLabel("Target File or Folder (Input):", left, 15);
            txtPath = CreateTextBox(left, 40, 470);
            btnBrowse = CreateButton("Browse", 505, 39, 100, 32, ControlBg);
            btnBrowse.Click += (s, e) => {
                using (OpenFileDialog ofd = new OpenFileDialog { Filter = "SCS Files|*.scs|All Files|*.*" })
                    if (ofd.ShowDialog() == DialogResult.OK) txtPath.Text = ofd.FileName;
            };
            AddLabel("Extraction Location (Output Path):", left, 85);
            txtOutPath = CreateTextBox(left, 110, 470);
            btnOutBrowse = CreateButton("Select", 505, 109, 100, 32, ControlBg);
            btnOutBrowse.Click += (s, e) => {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                    if (fbd.ShowDialog() == DialogResult.OK) txtOutPath.Text = fbd.SelectedPath;
            };
            AddLabel("Partial Extraction (-p):", left, 155);
            txtPartial = CreateTextBox(left, 180, width);
            AddLabel("Filter Patterns (-f):", left, 225);
            txtFilter = CreateTextBox(left, 250, width);
            AddLabel("HashFS Salt:", left, 295);
            txtSalt = CreateTextBox(left, 320, 300);
            chkRaw = CreateCheckBox("Raw Dumps (--raw)", 350, 320);
            chkDeep = CreateCheckBox("Deep Mode", left, 365);
            chkSeparate = CreateCheckBox("Separate Folders", 200, 365);
            chkSkip = CreateCheckBox("Skip Existing", 400, 365);
            AddLabel("Manual Commands:", left, 405);
            txtManual = CreateTextBox(left, 430, width);
            txtLog = new TextBox {
                Location = new Point(left, 475),
                Size = new Size(width, 280),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.LimeGreen,
                Font = new Font("Cascadia Code", 9),
                BorderStyle = BorderStyle.None
            };
            progress = new ProgressBar {
                Location = new Point(left, 770),
                Size = new Size(width, 10),
                Style = ProgressBarStyle.Marquee,
                Visible = false,
                MarqueeAnimationSpeed = 30
            };
            btnStart = CreateButton("START EXTRACTION", left, 800, 280, 50, AccentColor);
            btnStart.Click += RunExtractor;
            btnStop = CreateButton("STOP", 325, 800, 280, 50, Color.FromArgb(180, 0, 0));
            btnStop.Enabled = false;
            btnStop.Click += (s, e) => KillProcessTree();

            this.Controls.AddRange(new Control[] { txtPath, btnBrowse, txtOutPath, btnOutBrowse, txtPartial, txtFilter, txtSalt, chkRaw, chkDeep, chkSeparate, chkSkip, txtManual, txtLog, progress, btnStart, btnStop });
        }
        private void AddLabel(string text, int x, int y) => this.Controls.Add(new Label { Text = text, Location = new Point(x, y), AutoSize = true, ForeColor = Color.DarkGray });
        private TextBox CreateTextBox(int x, int y, int w) => new TextBox { Location = new Point(x, y), Size = new Size(w, 28), BackColor = ControlBg, ForeColor = TextColor, BorderStyle = BorderStyle.FixedSingle };
        private CheckBox CreateCheckBox(string text, int x, int y) => new CheckBox { Text = text, Location = new Point(x, y), AutoSize = true, FlatStyle = FlatStyle.Flat };
        private Button CreateButton(string text, int x, int y, int w, int h, Color bg) => new Button { Text = text, Location = new Point(x, y), Size = new Size(w, h), BackColor = bg, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Cursor = Cursors.Hand };
        private async void RunExtractor(object? sender, EventArgs? e)
        {
            if (string.IsNullOrWhiteSpace(txtPath.Text)) {
                MessageBox.Show("Please select an input file/folder.");
                return;
            }
            string args = BuildArguments();
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            progress.Visible = true;
            txtLog.Clear();
            Log($">> Launching: extractor.exe {args}\n");
            await Task.Run(() => {
                try {
                    currentProcess = new Process {
                        StartInfo = new ProcessStartInfo {
                            FileName = "extractor.exe",
                            Arguments = args,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };
                    currentProcess.OutputDataReceived += (s, evt) => { if (evt.Data != null) Log(evt.Data); };
                    currentProcess.ErrorDataReceived += (s, evt) => { if (evt.Data != null) Log("ERR: " + evt.Data); };
                    currentProcess.Start();
                    currentProcess.BeginOutputReadLine();
                    currentProcess.BeginErrorReadLine();
                    currentProcess.WaitForExit();
                }
                catch (Exception ex) { Log("CRITICAL ERROR: " + ex.Message); }
            });
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            progress.Visible = false;
            Log("\n>> Task Completed.");
        }
        private string BuildArguments()
        {
            string args = $"\"{txtPath.Text}\"";
            if (!string.IsNullOrWhiteSpace(txtOutPath.Text)) args += $" \"{txtOutPath.Text}\"";
            if (chkDeep.Checked) args += " --deep";
            if (chkSeparate.Checked) args += " --separate";
            if (chkSkip.Checked) args += " --skip-existing";
            if (chkRaw.Checked) args += " --raw";
            if (!string.IsNullOrWhiteSpace(txtSalt.Text)) args += $" --salt={txtSalt.Text.Trim()}";
            if (!string.IsNullOrWhiteSpace(txtFilter.Text)) args += $" --filter=\"{txtFilter.Text.Trim()}\"";
            if (!string.IsNullOrWhiteSpace(txtPartial.Text)) args += $" --partial=\"{txtPartial.Text.Trim()}\"";
            if (!string.IsNullOrWhiteSpace(txtManual.Text)) args += $" {txtManual.Text.Trim()}";
            return args;
        }
        private void Log(string msg) => this.Invoke(() => {
            txtLog.AppendText(msg + Environment.NewLine);
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        });
        private void KillProcessTree()
        {
            if (currentProcess == null) return;
            try {
                Process.Start(new ProcessStartInfo { FileName = "taskkill", Arguments = $"/F /T /PID {currentProcess.Id}", CreateNoWindow = true, UseShellExecute = false })?.WaitForExit();
                Log("\r\n[!] TERMINATED.");
            } catch { }
            finally { currentProcess = null; }
        }
        [STAThread]
        static void Main() { 
            Application.EnableVisualStyles(); 
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); 
            Application.Run(new MainForm()); 
        }
    }
}
