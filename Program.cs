using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ScsExtractorGui
{
    public class MainForm : Form
    {
        private TextBox txtPath, txtPartial, txtPathsFile, txtFilter, txtSalt, txtManual, txtLog;
        private CheckBox chkDeep, chkSeparate, chkSkip, chkRaw;
        private Button btnBrowse, btnBrowsePathsFile, btnStart, btnStop;
        private Process? currentProcess;
        private CancellationTokenSource? cts;

        public MainForm()
        {
            this.Text = "SCS Extractor GUI - Pro with Stop Feature";
            this.Size = new Size(650, 900);
            this.Font = new Font("Segoe UI", 9);
            this.StartPosition = FormStartPosition.CenterScreen;

            int leftMargin = 20;

            // Target Selection
            Label lbl = new Label { Text = "Target File or Folder:", Location = new Point(leftMargin, 10), AutoSize = true };
            txtPath = new TextBox { Location = new Point(leftMargin, 30), Size = new Size(480, 25) };
            btnBrowse = new Button { Text = "Browse", Location = new Point(510, 29), Size = new Size(100, 27) };
            btnBrowse.Click += (s, e) => {
                using (OpenFileDialog ofd = new OpenFileDialog { Filter = "SCS Files|*.scs|All Files|*.*" })
                    if (ofd.ShowDialog() == DialogResult.OK) txtPath.Text = ofd.FileName;
            };

            // Filter (-f)
            Label lblFilter = new Label { Text = "Filter Patterns (-f):", Location = new Point(leftMargin, 70), AutoSize = true };
            txtFilter = new TextBox { Location = new Point(leftMargin, 90), Size = new Size(590, 25) };

            // Partial Extraction (-p)
            Label lblPartial = new Label { Text = "Partial Extraction (-p):", Location = new Point(leftMargin, 130), AutoSize = true };
            txtPartial = new TextBox { Location = new Point(leftMargin, 150), Size = new Size(590, 25) };

            // Paths File (-P)
            Label lblPathsFile = new Label { Text = "Paths Text File (-P):", Location = new Point(leftMargin, 190), AutoSize = true };
            txtPathsFile = new TextBox { Location = new Point(leftMargin, 210), Size = new Size(480, 25) };
            btnBrowsePathsFile = new Button { Text = "Select File", Location = new Point(510, 209), Size = new Size(100, 27) };
            btnBrowsePathsFile.Click += (s, e) => {
                using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Text Files|*.txt|All Files|*.*" })
                    if (ofd.ShowDialog() == DialogResult.OK) txtPathsFile.Text = ofd.FileName;
            };

            // HashFS Options
            Label lblSalt = new Label { Text = "HashFS Salt (--salt):", Location = new Point(leftMargin, 250), AutoSize = true };
            txtSalt = new TextBox { Location = new Point(leftMargin, 270), Size = new Size(300, 25) };
            chkRaw = new CheckBox { Text = "Raw Dumps (--raw)", Location = new Point(350, 270), AutoSize = true };

            // Checkbox Options
            chkDeep = new CheckBox { Text = "Deep Mode (--deep)", Location = new Point(leftMargin, 310), AutoSize = true };
            chkSeparate = new CheckBox { Text = "Separate Folders (--separate)", Location = new Point(200, 310), AutoSize = true };
            chkSkip = new CheckBox { Text = "Skip Existing (--skip-existing)", Location = new Point(400, 310), AutoSize = true };

            // Manual Command
            Label lblManual = new Label { Text = "Additional Manual Commands:", Location = new Point(leftMargin, 350), AutoSize = true };
            txtManual = new TextBox { Location = new Point(leftMargin, 370), Size = new Size(590, 25) };

            // Log Output
            txtLog = new TextBox { Location = new Point(leftMargin, 410), Size = new Size(590, 300), Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.Black, ForeColor = Color.LightGray };

            // Start Button
            btnStart = new Button { Text = "START EXTRACTION", Location = new Point(leftMargin, 730), Size = new Size(285, 50), BackColor = Color.DarkSlateBlue, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnStart.Click += RunExtractor;

            // Stop Button - NEW
            btnStop = new Button { Text = "STOP", Location = new Point(325, 730), Size = new Size(285, 50), BackColor = Color.Firebrick, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Enabled = false };
            btnStop.Click += StopExtraction;

            this.Controls.AddRange(new Control[] { 
                lbl, txtPath, btnBrowse, lblFilter, txtFilter, lblPartial, txtPartial, 
                lblPathsFile, txtPathsFile, btnBrowsePathsFile, lblSalt, txtSalt, chkRaw,
                chkDeep, chkSeparate, chkSkip, lblManual, txtManual, txtLog, btnStart, btnStop 
            });
        }

        private void StopExtraction(object? sender, EventArgs e)
        {
            try {
                if (currentProcess != null && !currentProcess.HasExited) {
                    currentProcess.Kill(true); // Process tree shoho kill korbe
                    txtLog.AppendText("\r\n[!] Extraction stopped by user.\r\n");
                }
            } catch (Exception ex) {
                txtLog.AppendText("\r\nError stopping process: " + ex.Message + "\r\n");
            }
        }

        private async void RunExtractor(object? sender, EventArgs? e)
        {
            string target = txtPath.Text;
            if (string.IsNullOrEmpty(target)) {
                MessageBox.Show("Target file select korun!");
                return;
            }

            string args = $"\"{target}\"";
            if (chkDeep.Checked) args += " --deep";
            if (chkSeparate.Checked) args += " --separate";
            if (chkSkip.Checked) args += " --skip-existing";
            if (chkRaw.Checked) args += " --raw";
            if (!string.IsNullOrWhiteSpace(txtSalt.Text)) args += $" --salt={txtSalt.Text.Trim()}";
            if (!string.IsNullOrWhiteSpace(txtFilter.Text)) args += $" --filter=\"{txtFilter.Text.Trim()}\"";
            if (!string.IsNullOrWhiteSpace(txtPartial.Text)) args += $" --partial=\"{txtPartial.Text.Trim()}\"";
            if (!string.IsNullOrWhiteSpace(txtPathsFile.Text)) args += $" --paths=\"{txtPathsFile.Text.Trim()}\"";
            if (!string.IsNullOrWhiteSpace(txtManual.Text)) args += $" {txtManual.Text.Trim()}";

            btnStart.Enabled = false;
            btnStop.Enabled = true;
            txtLog.Clear();
            txtLog.AppendText($"Running: extractor.exe {args}\r\n\r\n");

            await Task.Run(() => {
                try {
                    ProcessStartInfo psi = new ProcessStartInfo {
                        FileName = "extractor.exe",
                        Arguments = args,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using (currentProcess = Process.Start(psi)) {
                        if (currentProcess != null) {
                            while (!currentProcess.StandardOutput.EndOfStream) {
                                string? line = currentProcess.StandardOutput.ReadLine();
                                this.Invoke(new Action(() => { if (line != null) txtLog.AppendText(line + Environment.NewLine); }));
                            }
                            currentProcess.WaitForExit();
                        }
                    }
                }
                catch (Exception ex) {
                    this.Invoke(new Action(() => txtLog.AppendText("ERROR: " + ex.Message)));
                }
            });

            btnStart.Enabled = true;
            btnStop.Enabled = false;
            currentProcess = null;
        }

        [STAThread]
        static void Main() { Application.EnableVisualStyles(); Application.Run(new MainForm()); }
    }
}
