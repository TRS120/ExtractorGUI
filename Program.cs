using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ScsExtractorGui.WinUI3
{
    /// <summary>
    /// Self-contained WinUI 3 window that builds the entire UI programmatically in C#
    /// while preserving every feature and command-line flag of the original WinForms GUI.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        // Input & Filter Controls
        private TextBox? _txtPath, _txtDest, _txtFilter, _txtPartial, _txtPathsFile;

        // Mode RadioButtons
        private RadioButton? _rbModeNormal, _rbModeList, _rbModeListAll, _rbModeListEntries, _rbModeTree;

        // Basic CheckBoxes
        private CheckBox? _chkAll, _chkSeparate, _chkSkip, _chkQuiet, _chkDryRun;

        // Advanced Controls
        private TextBox? _txtManual, _txtLog;

        // HashFS Controls
        private CheckBox? _chkDeep, _chkRaw, _chkTableAtEnd;
        private TextBox? _txtSalt, _txtAdditionalFile;

        // Action & Status Controls
        private Button? _btnStart, _btnStop;
        private ProgressBar? _progressBar;
        private ScrollViewer? _logScrollViewer;

        // Process & Path State
        private Process? _currentProcess;
        private readonly string _extractorPath = Path.Combine(AppContext.BaseDirectory, "extractor.exe");

        public MainWindow()
        {
            Title = "SCS Extractor GUI";

            try
            {
                ConfigureWindowSizeAndIcon();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Window setup warning: {ex.Message}");
            }

            Content = BuildRootLayout();

            // Clean up child process tree when the window closes
            Closed += (_, _) => KillProcessTree();

            // Warn if extractor.exe is missing beside the application executable
            if (!File.Exists(_extractorPath))
            {
                _ = ShowDialogAsync(
                    "Warning",
                    $"extractor.exe not found at:\n{_extractorPath}\n\nPlease place extractor.exe in the application folder.");
            }
        }

        private void ConfigureWindowSizeAndIcon()
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

            appWindow.Resize(new SizeInt32(820, 760));

            // Center window on primary display
            DisplayArea displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
            int centerX = (displayArea.WorkArea.Width - 820) / 2;
            int centerY = (displayArea.WorkArea.Height - 760) / 2;
            appWindow.Move(new PointInt32(Math.Max(0, centerX), Math.Max(0, centerY)));
        }

        private UIElement BuildRootLayout()
        {
            Grid rootGrid = new Grid
            {
                Padding = new Thickness(16),
                RowSpacing = 12,
                AllowDrop = true
            };

            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            rootGrid.DragOver += RootGrid_DragOver;
            rootGrid.Drop += RootGrid_Drop;

            // Pivot (equivalent to WinForms TabControl)
            Pivot pivot = new Pivot();
            pivot.Items.Add(new PivotItem { Header = "Basic", Content = BuildBasicTab() });
            pivot.Items.Add(new PivotItem { Header = "Advanced", Content = BuildAdvancedTab() });
            pivot.Items.Add(new PivotItem { Header = "HashFS", Content = BuildHashFSTab() });

            Grid.SetRow(pivot, 0);
            rootGrid.Children.Add(pivot);

            // Indeterminate Progress Bar
            _progressBar = new ProgressBar
            {
                IsIndeterminate = true,
                Visibility = Visibility.Collapsed,
                Height = 4
            };
            Grid.SetRow(_progressBar, 1);
            rootGrid.Children.Add(_progressBar);

            // Start & Stop Action Buttons
            Grid buttonGrid = new Grid
            {
                ColumnSpacing = 12,
                Height = 46
            };
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _btnStart = new Button
            {
                Content = "START EXTRACTION",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 40, 167, 69)),
                Foreground = new SolidColorBrush(Colors.White)
            };
            _btnStart.Click += RunExtractorAsync;
            Grid.SetColumn(_btnStart, 0);

            _btnStop = new Button
            {
                Content = "STOP",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                FontWeight = FontWeights.Bold,
                IsEnabled = false,
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 53, 69)),
                Foreground = new SolidColorBrush(Colors.White)
            };
            _btnStop.Click += (_, _) => KillProcessTree();
            Grid.SetColumn(_btnStop, 1);

            buttonGrid.Children.Add(_btnStart);
            buttonGrid.Children.Add(_btnStop);

            Grid.SetRow(buttonGrid, 2);
            rootGrid.Children.Add(buttonGrid);

            return rootGrid;
        }

        private UIElement BuildBasicTab()
        {
            ScrollViewer scroll = new ScrollViewer
            {
                Padding = new Thickness(4, 12, 12, 12),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            StackPanel panel = new StackPanel { Spacing = 14 };

            // INPUT & OUTPUT
            panel.Children.Add(CreateSectionHeader("INPUT & OUTPUT"));

            panel.Children.Add(CreateFieldLabel("Target File/Folder (drag & drop):"));
            _txtPath = new TextBox { PlaceholderText = "Select or drop an .scs file or folder..." };
            Button btnBrowse = new Button { Content = "Browse...", Width = 105 };
            btnBrowse.Click += async (_, _) =>
            {
                string? picked = await PickOpenFileAsync(new[] { ".scs", "*" });
                if (!string.IsNullOrEmpty(picked) && _txtPath != null)
                {
                    _txtPath.Text = picked;
                }
            };
            panel.Children.Add(CreateInputWithButton(_txtPath, btnBrowse));

            panel.Children.Add(CreateFieldLabel("Destination (-d):"));
            _txtDest = new TextBox { Text = "./extracted" };
            Button btnDestBrowse = new Button { Content = "Browse...", Width = 105 };
            btnDestBrowse.Click += async (_, _) =>
            {
                string? folder = await PickFolderAsync();
                if (!string.IsNullOrEmpty(folder) && _txtDest != null)
                {
                    _txtDest.Text = folder;
                }
            };
            panel.Children.Add(CreateInputWithButton(_txtDest, btnDestBrowse));

            // MODES
            panel.Children.Add(CreateSectionHeader("MODES"));
            StackPanel modesPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 16
            };

            _rbModeNormal = new RadioButton { Content = "Normal", GroupName = "ExtractionMode", IsChecked = true };
            _rbModeList = new RadioButton { Content = "List", GroupName = "ExtractionMode" };
            _rbModeListAll = new RadioButton { Content = "List All", GroupName = "ExtractionMode" };
            _rbModeListEntries = new RadioButton { Content = "List Entries", GroupName = "ExtractionMode" };
            _rbModeTree = new RadioButton { Content = "Tree", GroupName = "ExtractionMode" };

            modesPanel.Children.Add(_rbModeNormal);
            modesPanel.Children.Add(_rbModeList);
            modesPanel.Children.Add(_rbModeListAll);
            modesPanel.Children.Add(_rbModeListEntries);
            modesPanel.Children.Add(_rbModeTree);
            panel.Children.Add(modesPanel);

            // FILTERING
            panel.Children.Add(CreateSectionHeader("FILTERING"));

            panel.Children.Add(CreateFieldLabel("Filter Patterns (-f):"));
            _txtFilter = new TextBox();
            panel.Children.Add(_txtFilter);
            panel.Children.Add(CreateHelperHint("Examples: *volvo*,*scania* | r/\\.pmg$/"));

            panel.Children.Add(CreateFieldLabel("Partial Paths (-p):"));
            _txtPartial = new TextBox();
            panel.Children.Add(_txtPartial);
            panel.Children.Add(CreateHelperHint("Examples: /def,/map | /locale"));

            panel.Children.Add(CreateFieldLabel("Paths File (-P):"));
            _txtPathsFile = new TextBox();
            Button btnPathsBrowse = new Button { Content = "Browse...", Width = 105 };
            btnPathsBrowse.Click += async (_, _) =>
            {
                string? picked = await PickOpenFileAsync(new[] { "*" });
                if (!string.IsNullOrEmpty(picked) && _txtPathsFile != null)
                {
                    _txtPathsFile.Text = picked;
                }
            };
            panel.Children.Add(CreateInputWithButton(_txtPathsFile, btnPathsBrowse));

            // BASIC CHECKBOXES (Two rows)
            StackPanel row1 = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 20 };
            _chkAll = new CheckBox { Content = "Extract All (-a)" };
            _chkSeparate = new CheckBox { Content = "Separate Folders (-S)" };
            _chkSkip = new CheckBox { Content = "Skip Existing (-s)" };
            row1.Children.Add(_chkAll);
            row1.Children.Add(_chkSeparate);
            row1.Children.Add(_chkSkip);
            panel.Children.Add(row1);

            StackPanel row2 = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 20 };
            _chkQuiet = new CheckBox { Content = "Quiet (-q)" };
            _chkDryRun = new CheckBox { Content = "Dry Run (--dry-run)" };
            row2.Children.Add(_chkQuiet);
            row2.Children.Add(_chkDryRun);
            panel.Children.Add(row2);

            scroll.Content = panel;
            return scroll;
        }

        private UIElement BuildAdvancedTab()
        {
            Grid grid = new Grid
            {
                Padding = new Thickness(4, 12, 12, 12),
                RowSpacing = 12
            };

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            TextBlock hdrFlags = CreateSectionHeader("MANUAL FLAGS");
            Grid.SetRow(hdrFlags, 0);
            grid.Children.Add(hdrFlags);

            TextBlock lblManual = CreateFieldLabel("Additional command-line flags (advanced):");
            Grid.SetRow(lblManual, 1);
            grid.Children.Add(lblManual);

            _txtManual = new TextBox();
            Grid.SetRow(_txtManual, 2);
            grid.Children.Add(_txtManual);

            TextBlock hdrLog = CreateSectionHeader("LOG OUTPUT");
            hdrLog.Margin = new Thickness(0, 8, 0, 0);
            Grid.SetRow(hdrLog, 3);
            grid.Children.Add(hdrLog);

            _txtLog = new TextBox
            {
                IsReadOnly = true,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                Background = new SolidColorBrush(Colors.Black),
                Foreground = new SolidColorBrush(Colors.LightGreen),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            _logScrollViewer = new ScrollViewer
            {
                Content = _txtLog,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            Grid.SetRow(_logScrollViewer, 4);
            grid.Children.Add(_logScrollViewer);

            return grid;
        }

        private UIElement BuildHashFSTab()
        {
            StackPanel panel = new StackPanel
            {
                Padding = new Thickness(4, 12, 12, 12),
                Spacing = 14
            };

            panel.Children.Add(CreateSectionHeader("HashFS OPTIONS"));

            StackPanel chkRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 24 };
            _chkDeep = new CheckBox { Content = "Deep Mode (--deep)" };
            _chkRaw = new CheckBox { Content = "Raw Dumps (--raw)" };
            _chkTableAtEnd = new CheckBox { Content = "Table at End (--table-at-end)" };
            chkRow.Children.Add(_chkDeep);
            chkRow.Children.Add(_chkRaw);
            chkRow.Children.Add(_chkTableAtEnd);
            panel.Children.Add(chkRow);

            panel.Children.Add(CreateFieldLabel("Override Salt (--salt):"));
            _txtSalt = new TextBox { MaxWidth = 360, HorizontalAlignment = HorizontalAlignment.Left };
            panel.Children.Add(_txtSalt);

            panel.Children.Add(CreateFieldLabel("Additional Paths File (--additional):"));
            _txtAdditionalFile = new TextBox();
            Button btnAdditionalBrowse = new Button { Content = "Browse...", Width = 105 };
            btnAdditionalBrowse.Click += async (_, _) =>
            {
                string? picked = await PickOpenFileAsync(new[] { "*" });
                if (!string.IsNullOrEmpty(picked) && _txtAdditionalFile != null)
                {
                    _txtAdditionalFile.Text = picked;
                }
            };
            panel.Children.Add(CreateInputWithButton(_txtAdditionalFile, btnAdditionalBrowse));

            return panel;
        }

        // --- UI Helpers ---

        private static TextBlock CreateSectionHeader(string text) => new TextBlock
        {
            Text = text,
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Colors.DodgerBlue)
        };

        private static TextBlock CreateFieldLabel(string text) => new TextBlock
        {
            Text = text,
            FontSize = 13
        };

        private static TextBlock CreateHelperHint(string text) => new TextBlock
        {
            Text = text,
            FontSize = 11,
            Foreground = new SolidColorBrush(Colors.Gray),
            Margin = new Thickness(4, -8, 0, 0)
        };

        private static Grid CreateInputWithButton(TextBox textBox, Button button)
        {
            Grid grid = new Grid { ColumnSpacing = 8 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            textBox.VerticalAlignment = VerticalAlignment.Center;
            button.VerticalAlignment = VerticalAlignment.Center;

            Grid.SetColumn(textBox, 0);
            Grid.SetColumn(button, 1);

            grid.Children.Add(textBox);
            grid.Children.Add(button);
            return grid;
        }

        // --- File & Folder Pickers ---

        private async Task<string?> PickOpenFileAsync(string[] extensions)
        {
            try
            {
                FileOpenPicker picker = new FileOpenPicker();
                IntPtr hwnd = WindowNative.GetWindowHandle(this);
                InitializeWithWindow.Initialize(picker, hwnd);
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

                foreach (string ext in extensions)
                {
                    picker.FileTypeFilter.Add(ext);
                }

                StorageFile? file = await picker.PickSingleFileAsync();
                return file?.Path;
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("Picker Error", ex.Message);
                return null;
            }
        }

        private async Task<string?> PickFolderAsync()
        {
            try
            {
                FolderPicker picker = new FolderPicker();
                IntPtr hwnd = WindowNative.GetWindowHandle(this);
                InitializeWithWindow.Initialize(picker, hwnd);
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add("*");

                StorageFolder? folder = await picker.PickSingleFolderAsync();
                return folder?.Path;
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("Folder Picker Error", ex.Message);
                return null;
            }
        }

        private async Task ShowDialogAsync(string title, string content)
        {
            if (Content?.XamlRoot == null) return;

            ContentDialog dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "OK",
                XamlRoot = Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        // --- Drag & Drop ---

        private void RootGrid_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
            }
        }

        private async void RootGrid_Drop(object sender, DragEventArgs e)
        {
            if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;

            var items = await e.DataView.GetStorageItemsAsync();
            IStorageItem? firstItem = items.FirstOrDefault();
            if (firstItem != null && _txtPath != null)
            {
                _txtPath.Text = firstItem.Path;
            }
        }

        // --- Logging & Process Execution ---

        private void Log(string message)
        {
            if (_txtLog == null) return;

            if (!DispatcherQueue.HasThreadAccess)
            {
                DispatcherQueue.TryEnqueue(() => Log(message));
                return;
            }

            _txtLog.Text += message + Environment.NewLine;
            _logScrollViewer?.ChangeView(null, _logScrollViewer.ScrollableHeight, null, disableAnimation: true);
        }

        private async void RunExtractorAsync(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_txtPath?.Text))
                {
                    await ShowDialogAsync("Input missing", "Please select an input file.");
                    return;
                }

                if (!File.Exists(_extractorPath))
                {
                    await ShowDialogAsync("Error", "extractor.exe not found!");
                    return;
                }

                if (_btnStart != null) _btnStart.IsEnabled = false;
                if (_btnStop != null) _btnStop.IsEnabled = true;
                if (_progressBar != null) _progressBar.Visibility = Visibility.Visible;
                if (_txtLog != null) _txtLog.Text = string.Empty;

                string args = BuildArguments();
                Log($">> CMD: {_extractorPath} {args}\r\n");

                await Task.Run(() => RunProcess(args));

                Log("\r\n>> Operation finished.");
            }
            catch (Exception ex)
            {
                Log("CRITICAL ERROR: " + ex.Message);
            }
            finally
            {
                if (_btnStart != null) _btnStart.IsEnabled = true;
                if (_btnStop != null) _btnStop.IsEnabled = false;
                if (_progressBar != null) _progressBar.Visibility = Visibility.Collapsed;
            }
        }

        private string BuildArguments()
        {
            string args = $"\"{_txtPath!.Text}\"";

            if (!string.IsNullOrWhiteSpace(_txtDest?.Text) && _txtDest.Text != "./extracted")
                args += $" -d \"{_txtDest.Text}\"";

            // Modes
            if (_rbModeList?.IsChecked == true)
                args += " --list";
            else if (_rbModeListAll?.IsChecked == true)
                args += " --list-all";
            else if (_rbModeListEntries?.IsChecked == true)
                args += " --list-entries";
            else if (_rbModeTree?.IsChecked == true)
                args += " --tree";

            // Basic flags
            if (_chkAll?.IsChecked == true) args += " -a";
            if (_chkSeparate?.IsChecked == true) args += " -S";
            if (_chkSkip?.IsChecked == true) args += " -s";
            if (_chkQuiet?.IsChecked == true) args += " -q";
            if (_chkDryRun?.IsChecked == true) args += " --dry-run";

            // Filtering
            if (!string.IsNullOrWhiteSpace(_txtFilter?.Text))
                args += $" -f=\"{_txtFilter.Text.Trim()}\"";
            if (!string.IsNullOrWhiteSpace(_txtPartial?.Text))
                args += $" -p=\"{_txtPartial.Text.Trim()}\"";
            if (!string.IsNullOrWhiteSpace(_txtPathsFile?.Text) && File.Exists(_txtPathsFile.Text))
                args += $" -P \"{_txtPathsFile.Text}\"";

            // HashFS
            if (_chkDeep?.IsChecked == true) args += " -D";
            if (_chkRaw?.IsChecked == true) args += " --raw";
            if (_chkTableAtEnd?.IsChecked == true) args += " --table-at-end";
            if (!string.IsNullOrWhiteSpace(_txtSalt?.Text))
                args += $" --salt={_txtSalt.Text.Trim()}";
            if (!string.IsNullOrWhiteSpace(_txtAdditionalFile?.Text) && File.Exists(_txtAdditionalFile.Text))
                args += $" --additional \"{_txtAdditionalFile.Text}\"";

            // Manual flags
            if (!string.IsNullOrWhiteSpace(_txtManual?.Text))
                args += $" {_txtManual.Text.Trim()}";

            return args;
        }

        private void RunProcess(string args)
        {
            try
            {
                using (_currentProcess = new Process())
                {
                    _currentProcess.StartInfo = new ProcessStartInfo
                    {
                        FileName = _extractorPath,
                        Arguments = args,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    };

                    _currentProcess.OutputDataReceived += (_, a) => { if (a.Data != null) Log(a.Data); };
                    _currentProcess.ErrorDataReceived += (_, a) => { if (a.Data != null) Log("ERR: " + a.Data); };

                    _currentProcess.Start();
                    _currentProcess.BeginOutputReadLine();
                    _currentProcess.BeginErrorReadLine();
                    _currentProcess.WaitForExit();

                    if (_currentProcess.ExitCode != 0)
                    {
                        Log($"\r\n>> Process exited with code: {_currentProcess.ExitCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log("CRITICAL ERROR: " + ex.Message);
            }
            finally
            {
                _currentProcess = null;
            }
        }

        private void KillProcessTree()
        {
            if (_currentProcess == null) return;

            try
            {
                if (!_currentProcess.HasExited)
                {
                    _currentProcess.Kill(entireProcessTree: true);
                    _currentProcess.WaitForExit(3000);
                }

                if (!_currentProcess.HasExited)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = $"/F /T /PID {_currentProcess.Id}",
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
                _currentProcess = null;
            }
        }
    }
}
