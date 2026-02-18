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
        private TextBox? txtPath, txtDest, txtFilter, txtPartial, txtSalt;
        private TextBox? txtManual, txtLog, txtPathsFile, txtAdditionalFile;
        private CheckBox? chkAll, chkDeep, chkSeparate, chkSkip, chkRaw, chkQuiet, chkDryRun;
        private RadioButton? rbModeNormal, rbModeList, rbModeListAll, rbModeListEntries, rbModeTree;
        private Button? btnBrowse, btnDestBrowse, btnStart, btnStop, btnPathsBrowse, btnAdditionalBrowse;
        private ProgressBar? progressBar;
        private TabControl? tabControl;
        private Process? currentProcess;
        private readonly Color BgColor = Color.FromArgb(28, 28, 28);
        private readonly Color ControlBg = Color.FromArgb(45, 45, 45);
        private readonly Color AccentColor = Color.FromArgb(0, 120, 212);
        private readonly Color TextColor = Color.FromArgb(255, 255, 255);
        private readonly Color SecondaryText = Color.FromArgb(160, 160, 160);
        private readonly string extractorPath = "extractor.exe";
        public MainForm()
        {
            this.Text = "SCS Extractor GUI — Complete Edition";
            this.Size = new Size(750, 1000);
            this.BackColor = BgColor;
            this.ForeColor = TextColor;
            this.Font = new Font("Segoe UI", 10);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormClosing += (s, e) => KillProcessTree();
            InitializeComponents();
            if (!File.Exists(extractorPath))
            {
                MessageBox.Show("extractor.exe not found in application directory!\nPlease place extractor.exe in the same folder as this GUI.",
                    "Missing Extractor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void InitializeComponents()
        {
            int left = 20, width = 690;

            tabControl = new TabControl
            {
                Location = new Point(left, 10),
                Size = new Size(width, 850),
                BackColor = ControlBg,
                ForeColor = TextColor
            };
            TabPage basicTab = new TabPage("Basic Options") { BackColor = BgColor };
            AddBasicControls(basicTab);
            TabPage advancedTab = new TabPage("Advanced") { BackColor = BgColor };
            AddAdvancedControls(advancedTab);
            TabPage hashfsTab = new TabPage("HashFS") { BackColor = BgColor };
            AddHashFSControls(hashfsTab);
            tabControl.TabPages.AddRange(new TabPage[] { basicTab, advancedTab, hashfsTab });
            progressBar = new ProgressBar
            {
                Location = new Point(left, 870),
                Size = new Size(width, 6),
                Style = ProgressBarStyle.Marquee,
                Visible = false,
                MarqueeAnimationSpeed = 30
            };
            btnStart = CreateButton("START EXTRACTION", left, 890, 340, 45, AccentColor);
            btnStart.Font = new Font(this.Font, FontStyle.Bold);
            btnStart.Click += RunExtractor;
            btnStop = CreateButton("STOP", 370, 890, 340, 45, Color.FromArgb(190, 30, 30));
            btnStop.Enabled = false;
            btnStop.Click += (s, e) => KillProcessTree();
            this.Controls.AddRange(new Control[] { tabControl, progressBar, btnStart, btnStop });
        }
        private void AddBasicControls(TabPage page)
        {
            int left = 20, width = 630, y = 20;

            AddHeader(page, "INPUT & OUTPUT", left, y);
            y += 25;
            AddLabel(page, "Target File/Folder:", left, y);
            y += 20;
            txtPath = CreateTextBox(left, y, 500);
            btnBrowse = CreateButton("Browse...", 530, y - 2, 120, 32, ControlBg);
            btnBrowse.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog { Filter = "SCS Files|*.scs|All Files|*.*", Title = "Select SCS File" })
                    if (ofd.ShowDialog() == DialogResult.OK) txtPath!.Text = ofd.FileName;
            };
            page.Controls.AddRange(new Control[] { txtPath, btnBrowse });
            y += 35;
            AddLabel(page, "Destination Folder (-d):", left, y);
            y += 20;
            txtDest = CreateTextBox(left, y, 500);
            txtDest.Text = "./extracted";
            btnDestBrowse = CreateButton("Browse...", 530, y - 2, 120, 32, ControlBg);
            btnDestBrowse.Click += (s, e) =>
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                    if (fbd.ShowDialog() == DialogResult.OK) txtDest!.Text = fbd.SelectedPath;
            };
            page.Controls.AddRange(new Control[] { txtDest, btnDestBrowse });
            y += 45;
            AddHeader(page, "EXTRACTION MODES", left, y);
            y += 25;
            rbModeNormal = CreateRadioButton("Normal Extraction", left, y, true);
            rbModeList = CreateRadioButton("List Contents (--list)", left + 200, y, false);
            rbModeListAll = CreateRadioButton("List All (--list-all)", left + 400, y, false);
            page.Controls.AddRange(new Control[] { rbModeNormal, rbModeList, rbModeListAll });
            y += 30;
            rbModeListEntries = CreateRadioButton("List Entries (--list-entries)", left, y, false);
            rbModeTree = CreateRadioButton("Show Tree (--tree)", left + 200, y, false);
            page.Controls.AddRange(new Control[] { rbModeListEntries, rbModeTree });
            y += 45;
            AddHeader(page, "FILTERING", left, y);
            y += 25;
            AddLabel(page, "Filter Patterns (-f):", left, y);
            y += 20;
            txtFilter = CreateTextBox(left, y, width);
            AddLabel(page, "Examples: *volvo*,*scania* | r/\\.pmg$/", left + 5, y + 25, true, 10);
            y += 55;
            AddLabel(page, "Partial Extraction (-p):", left, y);
            y += 20;
            txtPartial = CreateTextBox(left, y, width);
            AddLabel(page, "Examples: /def,/map | /locale", left + 5, y + 25, true, 10);
            y += 55;
            AddLabel(page, "Paths File (-P):", left, y);
            y += 20;
            txtPathsFile = CreateTextBox(left, y, 500);
            btnPathsBrowse = CreateButton("Browse...", 530, y - 2, 120, 32, ControlBg);
            btnPathsBrowse.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                    if (ofd.ShowDialog() == DialogResult.OK) txtPathsFile!.Text = ofd.FileName;
            };
            page.Controls.AddRange(new Control[] { txtPathsFile, btnPathsBrowse });
            y += 45;
            chkAll = CreateCheckBox("Extract All Archives in Directory (-a)", left, y);
            y += 25;
            chkSeparate = CreateCheckBox("Separate Folders for Multiple Archives (-S)", left, y);
            y += 25;
            chkSkip = CreateCheckBox("Skip Existing Files (-s)", left, y);
            y += 25;
            chkQuiet = CreateCheckBox("Quiet Mode (-q)", left, y);
            y += 25;
            chkDryRun = CreateCheckBox("Dry Run (--dry-run)", left, y);
            page.Controls.AddRange(new Control[] { chkAll, chkSeparate, chkSkip, chkQuiet, chkDryRun });
        }
        private void AddAdvancedControls(TabPage page)
        {
            int left = 20, width = 630, y = 20;
            AddHeader(page, "ADVANCED OPTIONS", left, y);
            y += 40;
            AddLabel(page, "Manual Flags (for unsupported options):", left, y);
            y += 25;
            txtManual = CreateTextBox(left, y, width);
            y += 45;
            AddHeader(page, "OUTPUT LOG", left, y);
            y += 30;
            txtLog = new TextBox
            {
                Location = new Point(left, y),
                Size = new Size(width, 350),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.Black,
                ForeColor = Color.FromArgb(0, 255, 128),
                Font = new Font("Cascadia Code", 9),
                BorderStyle = BorderStyle.FixedSingle
            };
            page.Controls.Add(txtLog);
        }
        private void AddHashFSControls(TabPage page)
        {
            int left = 20, width = 630, y = 20;

            AddHeader(page, "HashFS OPTIONS", left, y);
            y += 40;
            chkDeep = CreateCheckBox("Deep Mode (--deep) - Scan for referenced paths", left, y);
            page.Controls.Add(chkDeep);
            y += 30;
            chkRaw = CreateCheckBox("Raw Dumps (--raw) - Keep hashed filenames", left, y);
            page.Controls.Add(chkRaw);
            y += 40;
            AddLabel(page, "Override Salt (--salt):", left, y);
            y += 25;
            txtSalt = CreateTextBox(left, y, 300);
            page.Controls.Add(txtSalt);
            y += 40;
            AddLabel(page, "Additional Paths File (--additional):", left, y);
            y += 25;
            txtAdditionalFile = CreateTextBox(left, y, 500);
            btnAdditionalBrowse = CreateButton("Browse...", 530, y - 2, 120, 32, ControlBg);
            btnAdditionalBrowse.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                    if (ofd.ShowDialog() == DialogResult.OK) txtAdditionalFile!.Text = ofd.FileName;
            };
            page.Controls.AddRange(new Control[] { txtAdditionalFile, btnAdditionalBrowse });
            y += 45;
            CheckBox chkTableAtEnd = CreateCheckBox("Table at End (--table-at-end) [v1 only]", left, y);
            page.Controls.Add(chkTableAtEnd);
            Label infoLabel = new Label
            {
                Text = "Note: HashFS options are for archives using HashFS format.\nUse Deep Mode when archives don't have a top-level directory listing.",
                Location = new Point(left, y + 40),
                Size = new Size(width, 60),
                ForeColor = SecondaryText,
                Font = new Font("Segoe UI", 9)
            };
            page.Controls.Add(infoLabel);
        }
        private void AddHeader(TabPage page, string text, int x, int y)
        {
            page.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = AccentColor,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            });
        }
        private void AddLabel(TabPage page, string text, int x, int y, bool small = false, int fontSize = 10)
        {
            page.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = small ? SecondaryText : TextColor,
                Font = new Font("Segoe UI", small ? 8 : fontSize)
            });
        }
        private TextBox CreateTextBox(int x, int y, int w)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(w, 30),
                BackColor = ControlBg,
                ForeColor = TextColor,
                BorderStyle = BorderStyle.FixedSingle
            };
        }
        private CheckBox CreateCheckBox(string text, int x, int y)
        {
            return new CheckBox
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                ForeColor = TextColor
            };
        }
        private RadioButton CreateRadioButton(string text, int x, int y, bool checkedState)
        {
            return new RadioButton
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                ForeColor = TextColor,
                Checked = checkedState
            };
        }
        private Button CreateButton(string text, int x, int y, int w, int h, Color bg)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = bg,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
        }
        private void Log(string msg)
        {
            if (txtLog!.InvokeRequired)
            {
                txtLog.Invoke(new Action<string>(Log), msg);
                return;
            }
            txtLog.AppendText(msg + Environment.NewLine);
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }
        private async void RunExtractor(object? sender, EventArgs? e)
        {
            if (string.IsNullOrEmpty(txtPath?.Text))
            {
                MessageBox.Show("Please select an input file/folder.", "Missing Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            Log("\r\n>> Operation Finished.");
        }
        private string BuildArguments()
        {
            string args = $"\"{txtPath!.Text}\"";

            if (!string.IsNullOrWhiteSpace(txtDest?.Text) && txtDest.Text != "./extracted")
                args += $" -d \"{txtDest.Text}\"";

            if (rbModeList!.Checked)
                args += " --list";
            else if (rbModeListAll!.Checked)
                args += " --list-all";
            else if (rbModeListEntries!.Checked)
                args += " --list-entries";
            else if (rbModeTree!.Checked)
                args += " --tree";
            if (chkAll!.Checked) args += " -a";
            if (chkSeparate!.Checked) args += " -S";
            if (chkSkip!.Checked) args += " -s";
            if (chkQuiet!.Checked) args += " -q";
            if (chkDryRun!.Checked) args += " --dry-run";
            if (!string.IsNullOrWhiteSpace(txtFilter?.Text))
                args += $" -f=\"{txtFilter.Text.Trim()}\"";
            if (!string.IsNullOrWhiteSpace(txtPartial?.Text))
                args += $" -p=\"{txtPartial.Text.Trim()}\"";
            if (!string.IsNullOrWhiteSpace(txtPathsFile?.Text) && File.Exists(txtPathsFile.Text))
                args += $" -P \"{txtPathsFile.Text}\"";
            if (chkDeep!.Checked) args += " -D";
            if (chkRaw!.Checked) args += " --raw";
            if (!string.IsNullOrWhiteSpace(txtSalt?.Text))
                args += $" --salt={txtSalt.Text.Trim()}";
            if (!string.IsNullOrWhiteSpace(txtAdditionalFile?.Text) && File.Exists(txtAdditionalFile.Text))
                args += $" --additional \"{txtAdditionalFile.Text}\"";
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
            if (currentProcess == null || currentProcess.HasExited)
                return;
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
