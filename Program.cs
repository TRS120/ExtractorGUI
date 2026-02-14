using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace ScsExtractorGui
{
    public class MainForm : Form
    {
        private TextBox txtPath, txtPartial, txtPathsFile, txtLog;
        private CheckBox chkDeep, chkAll, chkSeparate, chkSkip;
        private Button btnBrowse, btnBrowsePathsFile, btnStart;

        public MainForm()
        {
            // Window Settings
            this.Text = "SCS Extractor GUI";
            this.Size = new Size(620, 650);
            this.Font = new Font("Segoe UI", 9);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Target Selection
            Label lbl = new Label { Text = "Target File or Folder:", Location = new Point(20, 10), AutoSize = true };
            txtPath = new TextBox { Location = new Point(20, 30), Size = new Size(460, 25) };
            btnBrowse = new Button { Text = "Browse", Location = new Point(490, 29), Size = new Size(90, 27) };
            btnBrowse.Click += (s, e) => {
                using (OpenFileDialog ofd = new OpenFileDialog { Filter = "SCS Files|*.scs|All Files|*.*" })
                    if (ofd.ShowDialog() == DialogResult.OK) txtPath.Text = ofd.FileName;
            };

            // Partial Extraction (-p)
            Label lblPartial = new Label { Text = "Partial Extraction (comma separated, e.g. /def,/map):", Location = new Point(20, 70), AutoSize = true };
            txtPartial = new TextBox { Location = new Point(20, 90), Size = new Size(560, 25) };

            // Paths File (-P)
            Label lblPathsFile = new Label { Text = "Paths Text File (-P):", Location = new Point(20, 130), AutoSize = true };
            txtPathsFile = new TextBox { Location = new Point(20, 150), Size = new Size(460, 25), PlaceholderText = "Select .txt file with paths..." };
            btnBrowsePathsFile = new Button { Text = "Select File", Location = new Point(490, 149), Size = new Size(90, 27) };
            btnBrowsePathsFile.Click += (s, e) => {
                using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Text Files|*.txt|All Files|*.*" })
                    if (ofd.ShowDialog() == DialogResult.OK) txtPathsFile.Text = ofd.FileName;
            };

            // Options
            chkDeep = new CheckBox { Text = "Deep Mode (--deep)", Location = new Point(20, 190), AutoSize = true };
            chkAll = new CheckBox { Text = "Extract All (--all)", Location = new Point(200, 190), AutoSize = true };
            chkSeparate = new CheckBox { Text = "Separate Folders (--separate)", Location = new Point(20, 220), AutoSize = true };
            chkSkip = new CheckBox { Text = "Skip Existing (--skip-existing)", Location = new Point(200, 220), AutoSize = true };

            // Log Output
            txtLog = new TextBox { Location = new Point(20, 260), Size = new Size(560, 280), Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.Black, ForeColor = Color.LightGray };

            // Start Button
            btnStart = new Button { Text = "START EXTRACTION", Location = new Point(20, 555), Size = new Size(560, 45), BackColor = Color.DarkSlateBlue, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            btnStart.Click += RunExtractor;

            this.Controls.AddRange(new Control[] { lbl, txtPath, btnBrowse, lblPartial, txtPartial, lblPathsFile, txtPathsFile, btnBrowsePathsFile, chkDeep, chkAll, chkSeparate, chkSkip, txtLog, btnStart });
        }

        private async void RunExtractor(object? sender, EventArgs? e)
        {
            string target = txtPath.Text;
            if (string.IsNullOrEmpty(target)) {
                MessageBox.Show("Target file select korun!");
                return;
            }

            // Arguments toiri kora
            string args = $"\"{target}\"";
            if (chkDeep.Checked) args += " --deep";
            if (chkAll.Checked) args += " --all";
            if (chkSeparate.Checked) args += " --separate";
            if (chkSkip.Checked) args += " --skip-existing";
            
            // -p option
            if (!string.IsNullOrWhiteSpace(txtPartial.Text))
                args += $" --partial=\"{txtPartial.Text.Trim()}\"";

            // -P option
            if (!string.IsNullOrWhiteSpace(txtPathsFile.Text))
                args += $" --paths=\"{txtPathsFile.Text.Trim()}\"";

            btnStart.Enabled = false;
            txtLog.Clear();
            txtLog.AppendText($"Command: extractor.exe {args}\r\n\r\n");

            await System.Threading.Tasks.Task.Run(() => {
                try {
                    ProcessStartInfo psi = new ProcessStartInfo {
                        FileName = "extractor.exe",
                        Arguments = args,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using (Process p = Process.Start(psi)!) {
                        while (!p.StandardOutput.EndOfStream) {
                            string? line = p.StandardOutput.ReadLine();
                            this.Invoke(new Action(() => { if (line != null) txtLog.AppendText(line + Environment.NewLine); }));
                        }
                        p.WaitForExit();
                    }
                }
                catch (Exception ex) {
                    this.Invoke(new Action(() => txtLog.AppendText("ERROR: " + ex.Message)));
                }
            });

            btnStart.Enabled = true;
            MessageBox.Show("Extraction Finished!");
        }

        [STAThread]
        static void Main() { Application.EnableVisualStyles(); Application.Run(new MainForm()); }
    }
}
