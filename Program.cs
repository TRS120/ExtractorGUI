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
        // Controls
        private TextBox? txtPath, txtDest, txtFilter, txtPartial, txtPathsFile, txtSalt, txtAdditionalFile, txtManual, txtLog;
        private CheckBox? chkAll, chkDeep, chkSeparate, chkSkip, chkRaw, chkQuiet, chkDryRun, chkTableAtEnd;
        private RadioButton? rbModeNormal, rbModeList, rbModeListAll, rbModeListEntries, rbModeTree;
        private Button? btnBrowse, btnDestBrowse, btnPathsBrowse, btnAdditionalBrowse, btnStart, btnStop;
        private ProgressBar? progressBar;
        private TabControl? tabControl;
        private Process? currentProcess;

        // Colors
        private readonly Color BgColor = SystemColors.Control;
        private readonly Color ControlBg = Color.White;
        private readonly Color TextColor = SystemColors.ControlText;
        private readonly Color SecondaryText = SystemColors.GrayText;
        private readonly Color ButtonBg = SystemColors.ButtonFace;
        private readonly Color StartButtonColor = Color.FromArgb(40, 167, 69);
        private readonly Color StopButtonColor = Color.FromArgb(220, 53, 69);
        private readonly string extractorPath = "extractor.exe";

        public MainForm()
        {
            this.Text = "SCS Extractor GUI — Complete Compact Edition";
            this.Size = new Size(750, 700);
            this.MinimumSize = new Size(750, 700);
            this.BackColor = BgColor;
            this.ForeColor = TextColor;
            this.Font = new Font("Segoe UI", 10);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.AllowDrop = true;

            // Global drag-drop
            this.DragEnter += (s, e) => { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
            this.DragDrop += (s, e) => {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0 && txtPath != null) txtPath.Text = files[0];
            };

            this.FormClosing += (s, e) => KillProcessTree();
            InitializeComponents();

            if (!File.Exists(extractorPath))
                MessageBox.Show("extractor.exe not found!\nPlease place it in this folder.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void InitializeComponents()
        {
            int left = 15, width = 705;

            tabControl = new TabControl
            {
                Location = new Point(left, 10),
                Size = new Size(width, 560),
                BackColor = ControlBg
            };

            tabControl.TabPages.Add(new TabPage("Basic") { BackColor = ControlBg });
            tabControl.TabPages.Add(new TabPage("Advanced") { BackColor = ControlBg });
            tabControl.TabPages.Add(new TabPage("HashFS") { BackColor = ControlBg });

            AddBasicControls(tabControl.TabPages[0]);
            AddAdvancedControls(tabControl.TabPages[1]);
            AddHashFSControls(tabControl.TabPages[2]);

            progressBar = new ProgressBar
            {
                Location = new Point(left, 575),
                Size = new Size(width, 5),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };

            btnStart = CreateButton("START EXTRACTION", left, 590, 345, 45, StartButtonColor);
            btnStart.Font = new Font(this.Font, FontStyle.Bold);
            btnStart.ForeColor = Color.White;
            btnStart.FlatStyle = FlatStyle.Flat;
            btnStart.Click += RunExtractor;

            btnStop = CreateButton("STOP", 375, 590, 345, 45, StopButtonColor);
            btnStop.ForeColor = Color.White;
            btnStop.FlatStyle = FlatStyle.Flat;
            btnStop.Enabled = false;
            btnStop.Click += (s, e) => KillProcessTree();

            this.Controls.AddRange(new Control[] { tabControl, progressBar, btnStart, btnStop });
        }

        private void AddBasicControls(TabPage page)
        {
            int left = 15, width = 665, y = 10;

            // Input
            AddHeader(page, "INPUT & OUTPUT", left, y);
            y += 22;
            AddLabel(page, "Target File/Folder (drag & drop):", left, y);
            y += 20;
            txtPath = CreateTextBox(left, y, 540);
            btnBrowse = CreateButton("Browse...", 565, y - 1, 100, 30, ButtonBg);
            btnBrowse.Click += (s, e) => {
                using (OpenFileDialog ofd = new OpenFileDialog { Filter = "SCS files|*.scs|All files|*.*" })
                    if (ofd.ShowDialog() == DialogResult.OK) txtPath!.Text = ofd.FileName;
            };
            page.Controls.AddRange(new Control[] { txtPath, btnBrowse });

            y += 35;
            AddLabel(page, "Destination (-d):", left, y);
            y += 20;
            txtDest = CreateTextBox(left, y, 540);
            txtDest.Text = "./extracted";
            btnDestBrowse = CreateButton("Browse...", 565, y - 1, 100, 30, ButtonBg);
            btnDestBrowse.Click += (s, e) => {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                    if (fbd.ShowDialog() == DialogResult.OK) txtDest!.Text = fbd.SelectedPath;
            };
            page.Controls.AddRange(new Control[] { txtDest, btnDestBrowse });

            // Modes (radio buttons)
            y += 45;
            AddHeader(page, "MODES", left, y);
            y += 20;
            rbModeNormal = CreateRadioButton("Normal", left, y, true);
            rbModeList = CreateRadioButton("List", left + 100, y, false);
            rbModeListAll = CreateRadioButton("List All", left + 180, y, false);
            rbModeListEntries = CreateRadioButton("List Entries", left + 280, y, false);
            rbModeTree = CreateRadioButton("Tree", left + 400, y, false);
            page.Controls.AddRange(new Control[] { rbModeNormal, rbModeList, rbModeListAll, rbModeListEntries, rbModeTree });

            // Filtering section
            y += 35;
            AddHeader(page, "FILTERING", left, y);
            y += 22;
            AddLabel(page, "Filter Patterns (-f):", left, y);
            txtFilter = CreateTextBox(left, y + 20, width);
            page.Controls.Add(txtFilter);
            AddLabel(page, "Examples: *volvo*,*scania* | r/\\.pmg$/", left + 5, y + 45, true, 9);

            y += 70;
            AddLabel(page, "Partial Paths (-p):", left, y);
            txtPartial = CreateTextBox(left, y + 20, width);
            page.Controls.Add(txtPartial);
            AddLabel(page, "Examples: /def,/map | /locale", left + 5, y + 45, true, 9);

            y += 70;
            AddLabel(page, "Paths File (-P):", left, y);
            txtPathsFile = CreateTextBox(left, y + 20, 540);
            btnPathsBrowse = CreateButton("Browse...", 565, y + 19, 100, 30, ButtonBg);
            btnPathsBrowse.Click += (s, e) => {
                using (OpenFileDialog ofd = new OpenFileDialog())
                    if (ofd.ShowDialog() == DialogResult.OK) txtPathsFile!.Text = ofd.FileName;
            };
            page.Controls.AddRange(new Control[] { txtPathsFile, btnPathsBrowse });

            // Basic checkboxes (two rows)
            y += 55;
            chkAll = CreateCheckBox("Extract All (-a)", left, y);
            chkSeparate = CreateCheckBox("Separate Folders (-S)", left + 150, y);
            chkSkip = CreateCheckBox("Skip Existing (-s)", left + 350, y);
            page.Controls.AddRange(new Control[] { chkAll, chkSeparate, chkSkip });

            y += 25;
            chkQuiet = CreateCheckBox("Quiet (-q)", left, y);
            chkDryRun = CreateCheckBox("Dry Run (--dry-run)", left + 150, y);
            page.Controls.AddRange(new Control[] { chkQuiet, chkDryRun });
        }

        private void AddAdvancedControls(TabPage page)
        {
            int left = 15, width = 665, y = 10;

            AddHeader(page, "MANUAL FLAGS", left, y);
            y += 25;
            AddLabel(page, "Additional command-line flags (advanced):", left, y);
            txtManual = CreateTextBox(left, y + 20, width);
            page.Controls.Add(txtManual);

            y += 70;
            AddHeader(page, "LOG OUTPUT", left, y);
            y += 25;
            txtLog = new TextBox
            {
                Location = new Point(left, y),
                Size = new Size(width, 270),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.Black,
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 9),
                BorderStyle = BorderStyle.FixedSingle
            };
            page.Controls.Add(txtLog);
        }

        private void AddHashFSControls(TabPage page)
        {
            int left = 15, width = 665, y = 10;

            AddHeader(page, "HashFS OPTIONS", left, y);
            y += 25;
            chkDeep = CreateCheckBox("Deep Mode (--deep)", left, y);
            chkRaw = CreateCheckBox("Raw Dumps (--raw)", left + 200, y);
            chkTableAtEnd = CreateCheckBox("Table at End (--table-at-end)", left + 400, y);
            page.Controls.AddRange(new Control[] { chkDeep, chkRaw, chkTableAtEnd });

            y += 35;
            AddLabel(page, "Override Salt (--salt):", left, y);
            txtSalt = CreateTextBox(left, y + 20, 300);
            page.Controls.Add(txtSalt);

            y += 60;
            AddLabel(page, "Additional Paths File (--additional):", left, y);
            txtAdditionalFile = CreateTextBox(left, y + 20, 540);
            btnAdditionalBrowse = CreateButton("Browse...", 565, y + 19, 100, 30, ButtonBg);
            btnAdditionalBrowse.Click += (s, e) => {
                using (OpenFileDialog ofd = new OpenFileDialog())
                    if (ofd.ShowDialog() == DialogResult.OK) txtAdditionalFile!.Text = ofd.FileName;
            };
            page.Controls.AddRange(new Control[] { txtAdditionalFile, btnAdditionalBrowse });
        }

        // Helper methods
        private void AddHeader(TabPage page, string text, int x, int y) =>
            page.Controls.Add(new Label { Text = text, Location = new Point(x, y), AutoSize = true, ForeColor = Color.DodgerBlue, Font = new Font("Segoe UI", 9, FontStyle.Bold) });

        private void AddLabel(TabPage page, string text, int x, int y, bool small = false, int fontSize = 10) =>
            page.Controls.Add(new Label { Text = text, Location = new Point(x, y), AutoSize = true, ForeColor = small ? SecondaryText : TextColor, Font = new Font("Segoe UI", small ? 8 : fontSize) });

        private TextBox CreateTextBox(int x, int y, int w) =>
            new TextBox { Location = new Point(x, y), Size = new Size(w, 25), BackColor = ControlBg, BorderStyle = BorderStyle.FixedSingle };

        private CheckBox CreateCheckBox(string text, int x, int y) =>
            new CheckBox { Text = text, Location = new Point(x, y), AutoSize = true, ForeColor = TextColor };

        private RadioButton CreateRadioButton(string text, int x, int y, bool check) =>
            new RadioButton { Text = text, Location = new Point(x, y), AutoSize = true, Checked = check };

        private Button CreateButton(string text, int x, int y, int w, int h, Color bg) =>
            new Button { Text = text, Location = new Point(x, y), Size = new Size(w, h), BackColor = bg, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };

        private void Log(string msg)
        {
            if (txtLog!.InvokeRequired)
            {
                txtLog.Invoke(new Action<string>(Log), msg);
                return;
            }
            txtLog.AppendText(msg + Environment.NewLine);
            txtLog.ScrollToCaret();
        }

        private async void RunExtractor(object? sender, EventArgs? e)
        {
            if (string.IsNullOrEmpty(txtPath?.Text))
            {
                MessageBox.Show("Please select an input file.", "Input missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!File.Exists(extractorPath))
            {
                MessageBox.Show("extractor.exe not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnStart!.Enabled = false;
            btnStop!.Enabled = true;
            progressBar!.Visible = true;
            txtLog!.Clear();

            string args = BuildArguments();
            Log($">> CMD: {extractorPath} {args}\r\n");

            await Task.Run(() => RunProcess(args));

            btnStart.Enabled = true;
            btnStop.Enabled = false;
            progressBar.Visible = false;
            Log("\r\n>> Operation finished.");
        }

        private string BuildArguments()
        {
            string args = $"\"{txtPath!.Text}\"";
            if (!string.IsNullOrWhiteSpace(txtDest?.Text) && txtDest.Text != "./extracted")
                args += $" -d \"{txtDest.Text}\"";

            // Modes
            if (rbModeList!.Checked)
                args += " --list";
            else if (rbModeListAll!.Checked)
                args += " --list-all";
            else if (rbModeListEntries!.Checked)
                args += " --list-entries";
            else if (rbModeTree!.Checked)
                args += " --tree";

            // Basic flags
            if (chkAll!.Checked) args += " -a";
            if (chkSeparate!.Checked) args += " -S";
            if (chkSkip!.Checked) args += " -s";
            if (chkQuiet!.Checked) args += " -q";
            if (chkDryRun!.Checked) args += " --dry-run";

            // Filtering
            if (!string.IsNullOrWhiteSpace(txtFilter?.Text))
                args += $" -f=\"{txtFilter.Text.Trim()}\"";
            if (!string.IsNullOrWhiteSpace(txtPartial?.Text))
                args += $" -p=\"{txtPartial.Text.Trim()}\"";
            if (!string.IsNullOrWhiteSpace(txtPathsFile?.Text) && File.Exists(txtPathsFile.Text))
                args += $" -P \"{txtPathsFile.Text}\"";

            // HashFS
            if (chkDeep!.Checked) args += " -D";
            if (chkRaw!.Checked) args += " --raw";
            if (chkTableAtEnd!.Checked) args += " --table-at-end";
            if (!string.IsNullOrWhiteSpace(txtSalt?.Text))
                args += $" --salt={txtSalt.Text.Trim()}";
            if (!string.IsNullOrWhiteSpace(txtAdditionalFile?.Text) && File.Exists(txtAdditionalFile.Text))
                args += $" --additional \"{txtAdditionalFile.Text}\"";

            // Manual flags
            if (!string.IsNullOrWhiteSpace(txtManual?.Text))
                args += $" {txtManual.Text.Trim()}";

            return args;
        }

        private void RunProcess(string args)
        {
            try
            {
                using (currentProcess = new Process())
                {
                    currentProcess.StartInfo = new ProcessStartInfo
                    {
                        FileName = extractorPath,
                        Arguments = args,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8,
                        StandardErrorEncoding = System.Text.Encoding.UTF8
                    };

                    currentProcess.OutputDataReceived += (s, a) => { if (a.Data != null) Log(a.Data); };
                    currentProcess.ErrorDataReceived += (s, a) => { if (a.Data != null) Log("ERR: " + a.Data); };

                    currentProcess.Start();
                    currentProcess.BeginOutputReadLine();
                    currentProcess.BeginErrorReadLine();
                    currentProcess.WaitForExit();

                    if (currentProcess.ExitCode != 0)
                        Log($"\r\n>> Process exited with code: {currentProcess.ExitCode}");
                }
            }
            catch (Exception ex)
            {
                Log("CRITICAL ERROR: " + ex.Message);
            }
            finally
            {
                currentProcess = null;
            }
        }

        private void KillProcessTree()
        {
            if (currentProcess == null || currentProcess.HasExited) return;
            try
            {
                currentProcess.Kill();
                currentProcess.WaitForExit(3000);
                if (!currentProcess.HasExited)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = $"/F /T /PID {currentProcess.Id}",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    })?.WaitForExit();
                }
                Log("\r\n[!] PROCESS TERMINATED.");
            }
            catch (Exception ex)
            {
                Log($"Error killing process: {ex.Message}");
            }
            finally
            {
                currentProcess = null;
            }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.Run(new MainForm());
        }
    }
}
