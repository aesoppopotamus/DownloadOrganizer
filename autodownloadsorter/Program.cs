// --- File: Program.cs (includes tray UI and settings GUI) ---
using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;

namespace SortDownloadsTrayApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApplicationContext());
        }
    }

    class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;

        public TrayApplicationContext()
        {
            DownloadsOrganizer.LoadRulesFromFile();
            DownloadsOrganizer.StartWatcher();

            // Load custom icon if present, fallback to system icon
            Icon trayIconImage;
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "autodownloadsorter_icon.ico");
            if (File.Exists(iconPath))
                trayIconImage = new Icon(iconPath);
            else
                trayIconImage = SystemIcons.Application;

            _trayIcon = new NotifyIcon
            {
                Icon = trayIconImage,
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true,
                Text = "Sort Downloads"
            };
            _trayIcon.ContextMenuStrip.Items.AddRange(new ToolStripItem[]
            {
                new ToolStripMenuItem("Sort Now", null, SortNow),
                new ToolStripMenuItem("Settings", null, ShowSettings),
                new ToolStripMenuItem("View Logs", null, ViewLogs),
                new ToolStripMenuItem("Exit", null, Exit)
            });
        }

        private void ShowSettings(object? sender, EventArgs e)
        {
            using var form = new RulesForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                DownloadsOrganizer.SaveRulesToFile();
            }
        }

        private void SortNow(object? sender, EventArgs e)
        {
            int movedCount = DownloadsOrganizer.Run();
            DownloadsOrganizer.SaveRulesToFile();
            MessageBox.Show($"Moved {movedCount} file(s) and organized your Downloads folder.", "Sort Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ViewLogs(object? sender, EventArgs e)
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)!,
                "SortDownloadsTrayApp",
                "Logs",
                "sortdownloads.log"
            );
            if (!File.Exists(logPath))
            {
                MessageBox.Show("No log file found.", "View Logs", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = logPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open log: {ex.Message}", "View Logs Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Exit(object? sender, EventArgs e)
        {
            DownloadsOrganizer.StopWatcher();
            _trayIcon.Dispose();
            Application.Exit();
        }
    }

    class RulesForm : Form
    {
        private readonly DataGridView _grid;
        private readonly Button _btnSave;
        private readonly Button _btnCancel;

        public RulesForm()
        {
            Text = "Edit File Sort Rules";
            Size = new Size(400, 500);
            StartPosition = FormStartPosition.CenterScreen;

            _grid = new DataGridView { Dock = DockStyle.Top, Height = 380, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            _grid.Columns.Add("Extension", "Extension");
            _grid.Columns.Add("Folder", "Folder Name");

            var rules = DownloadsOrganizer.GetRules();
            foreach (var kv in rules)
                _grid.Rows.Add(kv.Key, kv.Value);

            _btnSave = new Button { Text = "Save", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom, Height = 30 };
            _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Dock = DockStyle.Bottom, Height = 30 };
            AcceptButton = _btnSave;
            CancelButton = _btnCancel;

            Controls.Add(_grid);
            Controls.Add(_btnSave);
            Controls.Add(_btnCancel);

            _btnSave.Click += (s, e) => OnSave();
        }

        private void OnSave()
        {
            var newRules = new Dictionary<string, string>();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow) continue;
                var ext = row.Cells[0].Value?.ToString()?.Trim() ?? string.Empty;
                var folder = row.Cells[1].Value?.ToString()?.Trim() ?? string.Empty;
                newRules[ext] = folder;
            }
            DownloadsOrganizer.SetRules(newRules);
            Close();
        }
    }
}
