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
        private ProgressBar progressBar;
        private Process? currentProcess;
        private readonly Color BgColor = Color.FromArgb(28, 28, 28);
        private readonly Color ControlBg = Color.FromArgb(45, 45, 45);
        private readonly Color AccentColor = Color.FromArgb(0, 120, 212);
        private readonly Color TextColor = Color.FromArgb(255, 255, 255);
        private readonly Color SecondaryText = Color.FromArgb(160, 160, 160);
        public MainForm()
        {
            this.Text = "Extractor GUI — Win11 Dark";
            this.Size = new Size(650, 920);
            this.BackColor = BgColor;
            this.ForeColor = TextColor;
            this.Font = new Font("Segoe UI Variable Display", 10);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormClosing += (s, e) => KillProcessTree();
            InitializeComponents();
        }
        private void InitializeComponents()
        {
            int left = 30, width = 575;
            AddHeader("Input & Output", left, 15);
            AddLabel("Target File or Folder:", left, 45);
            txtPath = CreateTextBox(left, 70, 465);
            btnBrowse = CreateButton("Browse", 505, 69, 100, 32, ControlBg);
            btnBrowse.Click += (s, e) => {
                using (OpenFileDialog ofd = new OpenFileDialog { Filter = "SCS Files|*.scs|All Files|*.*" })
                    if (ofd.ShowDialog() == DialogResult.OK) txtPath.Text = ofd.FileName;
            };
            AddLabel("Extraction Path (Optional):", left, 110);
            txtOutPath = CreateTextBox(left, 135, 465);
            btnOutBrowse = CreateButton("Select", 505, 134, 100, 32, ControlBg);
            btnOutBrowse.Click += (s, e) => {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                    if (fbd.ShowDialog() == DialogResult.OK) txtOutPath.Text = fbd.SelectedPath;
            };
            AddHeader("Parameters", left, 185);
            AddLabel("Partial Extraction (-p):", left, 215);
            txtPartial = CreateTextBox(left, 240, width);
            AddLabel("Filter Patterns (-f):", left, 285);
            txtFilter = CreateTextBox(left, 310, width);
            AddLabel("HashFS Salt:", left, 355);
            txtSalt = CreateTextBox(left, 380, 300);
            chkRaw = CreateCheckBox("Raw Dumps (--raw)", 350, 380);
            chkDeep = CreateCheckBox("Deep Mode", left, 425);
            chkSeparate = CreateCheckBox("Separate Folders", 200, 425);
            chkSkip = CreateCheckBox("Skip Existing", 400, 425);
            AddLabel("Manual Flags:", left, 465);
            txtManual = CreateTextBox(left, 490, width);
            txtLog = new TextBox { Location = new Point(left, 535), Size = new Size(width, 220), Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.Black, ForeColor = Color.FromArgb(0, 255, 128), Font = new Font("Cascadia Code", 9), BorderStyle = BorderStyle.None };
            progressBar = new ProgressBar { Location = new Point(left, 765), Size = new Size(width, 6), Style = ProgressBarStyle.Marquee, Visible = false, MarqueeAnimationSpeed = 30 };
            btnStart = CreateButton("START EXTRACTION", left, 790, 275, 55, AccentColor);
            btnStart.Font = new Font(this.Font, FontStyle.Bold);
            btnStart.Click += RunExtractor;
            btnStop = CreateButton("STOP", 330, 790, 275, 55, Color.FromArgb(190, 30, 30));
            btnStop.Enabled = false;
            btnStop.Click += (s, e) => KillProcessTree();
            this.Controls.AddRange(new Control[] { txtPath, btnBrowse, txtOutPath, btnOutBrowse, txtPartial, txtFilter, txtSalt, chkRaw, chkDeep, chkSeparate, chkSkip, txtManual, txtLog, progressBar, btnStart, btnStop });
        }
        private void AddHeader(string text, int x, int y) => this.Controls.Add(new Label { Text = text.ToUpper(), Location = new Point(x, y), AutoSize = true, ForeColor = AccentColor, Font = new Font("Segoe UI Variable Text", 9, FontStyle.Bold) });
        private void AddLabel(string text, int x, int y) => this.Controls.Add(new Label { Text = text, Location = new Point(x, y), AutoSize = true, ForeColor = SecondaryText });
        private TextBox CreateTextBox(int x, int y, int w) => new TextBox { Location = new Point(x, y), Size = new Size(w, 30), BackColor = ControlBg, ForeColor = TextColor, BorderStyle = BorderStyle.FixedSingle };
        private CheckBox CreateCheckBox(string text, int x, int y) => new CheckBox { Text = text, Location = new Point(x, y), AutoSize = true, FlatStyle = FlatStyle.Flat };
        private Button CreateButton(string text, int x, int y, int w, int h, Color bg) => new Button { Text = text, Location = new Point(x, y), Size = new Size(w, h), BackColor = bg, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Cursor = Cursors.Hand };
        private void Log(string msg) => this.Invoke(() => { txtLog.AppendText(msg + Environment.NewLine); txtLog.SelectionStart = txtLog.Text.Length; txtLog.ScrollToCaret(); });
        private async void RunExtractor(object? sender, EventArgs? e)
        {
            if (string.IsNullOrEmpty(txtPath.Text)) { MessageBox.Show("Please select an input file.", "Missing Input", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            btnStart.Enabled = false; btnStop.Enabled = true; progressBar.Visible = true; txtLog.Clear();
            string args = BuildArguments();
            Log($">> CMD: extractor.exe {args}\r\n");
            await Task.Run(() => {
                try {
                    currentProcess = new Process { StartInfo = new ProcessStartInfo { FileName = "extractor.exe", Arguments = args, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true } };
                    currentProcess.OutputDataReceived += (s, a) => { if (a.Data != null) Log(a.Data); };
                    currentProcess.ErrorDataReceived += (s, a) => { if (a.Data != null) Log("ERR: " + a.Data); };
                    currentProcess.Start();
                    currentProcess.BeginOutputReadLine();
                    currentProcess.BeginErrorReadLine();
                    currentProcess.WaitForExit();
                }
                catch (Exception ex) { Log("CRITICAL ERROR: " + ex.Message); }
            });
            btnStart.Enabled = true; btnStop.Enabled = false; progressBar.Visible = false;
            Log("\r\n>> Operation Finished.");
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
        private void KillProcessTree()
        {
            if (currentProcess == null) return;
            try { Process.Start(new ProcessStartInfo { FileName = "taskkill", Arguments = $"/F /T /PID {currentProcess.Id}", CreateNoWindow = true, UseShellExecute = false })?.WaitForExit(); Log("\r\n[!] ABORTED."); }
            catch { }
            finally { currentProcess = null; }
        }
        [STAThread]
        static void Main() { Application.EnableVisualStyles(); Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.Run(new MainForm()); }
    }
}
