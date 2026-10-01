using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace NetworkingToolkit
{
    public partial class Form1 : Form
    {
        private bool cleaningRequests;
        private bool cleanupQueued;
        private bool calculatingVlsm;
        private bool prefixEdited;
        private readonly Dictionary<DataGridViewRow, string> manualPrefixes = new();
        private readonly TextBox plannerStatus = new();
        private readonly Color surface = Color.FromArgb(48, 48, 48);
        private readonly Color ink = Color.FromArgb(236, 236, 236);
        private readonly Color danger = Color.FromArgb(85, 39, 39);
        private readonly Color manualColor = Color.FromArgb(76, 65, 36);

        public Form1()
        {
            InitializeComponent();
            SetupInterface();

            // VLSM table
            gridRequests.ReadOnly = false;
            gridRequests.AllowUserToAddRows = true;
            gridRequests.AllowUserToDeleteRows = true;
            gridRequests.MultiSelect = false;
            gridRequests.SelectionMode =
                DataGridViewSelectionMode.CellSelect;
            gridRequests.EditMode =
                DataGridViewEditMode.EditOnKeystrokeOrF2;

            gridRequests.Rows.Clear();

            for (int i = 0; i < gridRequests.Columns.Count; i++)
            {
                // Only Subnet and Hosts Needed are editable
                gridRequests.Columns[i].ReadOnly = i != 0 && i != 1 && i != 3;
                gridRequests.Columns[i].SortMode =
                    DataGridViewColumnSortMode.NotSortable;
            }

            gridRequests.EditingControlShowing +=
                GridRequests_EditingControlShowing;

            gridRequests.KeyDown += GridRequests_KeyDown;

            gridRequests.CellBeginEdit += (s, e) => prefixEdited = false;
            gridRequests.CellEndEdit += (s, e) =>
            {
                if (e.ColumnIndex == 3 && prefixEdited)
                    SaveManualPrefix(gridRequests.Rows[e.RowIndex]);
                prefixEdited = false;
                QueueRequestCleanup();
            };

            gridRequests.CellValueChanged += (s, e) =>
            {
                if (calculatingVlsm || cleaningRequests || e.RowIndex < 0) return;
                if (e.ColumnIndex == 3)
                    SaveManualPrefix(gridRequests.Rows[e.RowIndex]);
                if (e.ColumnIndex == 0 || e.ColumnIndex == 1 || e.ColumnIndex == 3)
                    QueueRequestCleanup();
            };

            gridRequests.UserDeletedRow += (s, e) =>
            {
                manualPrefixes.Remove(e.Row);
                QueueRequestCleanup();
            };

            txtNetwork.TextChanged += (s, e) =>
                QueueRequestCleanup();

            Shown += (s, e) => QueueRequestCleanup();

            // Bits table
            int[] values =
            {
                32768, 16384, 8192, 4096,
                2048, 1024, 512, 256,
                128, 64, 32, 16,
                8, 4, 2, 1
            };

            for (int i = 0; i < 16; i++)
            {
                var valueLabel = new Label
                {
                    Text = values[i].ToString(),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = false,
                    Font = new Font("Consolas", 9),
                    BorderStyle = BorderStyle.FixedSingle,
                    Margin = new Padding(0)
                };

                var bitBox = new TextBox
                {
                    Text = "0",
                    MaxLength = 1,
                    Dock = DockStyle.Fill,
                    TextAlign = HorizontalAlignment.Center,
                    Name = $"bit{i}"
                };

                bitBox.KeyPress += (s, e) =>
                {
                    if (!char.IsControl(e.KeyChar))
                    {
                        e.Handled = true;
                        if (e.KeyChar == '0' || e.KeyChar == '1')
                        {
                            bitBox.Text = e.KeyChar.ToString();
                            bitBox.SelectAll();
                        }
                    }
                };
                bitBox.TextChanged += (s, e) =>
                {
                    if (bitBox.Text != "0" && bitBox.Text != "1")
                    {
                        bitBox.Text = "0";
                        bitBox.SelectAll();
                    }
                };

                bitBox.Enter += (s, e) => bitBox.SelectAll();
                bitBox.Click += (s, e) => bitBox.SelectAll();

                bitBox.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete)
                    {
                        e.SuppressKeyPress = true;
                        bitBox.Text = "0";
                        bitBox.SelectAll();
                        return;
                    }
                    int current = tblBits.GetColumn(bitBox);

                    int next = e.KeyCode == Keys.Right ? current + 1
                             : e.KeyCode == Keys.Left ? current - 1
                             : current;

                    if (next != current && next >= 0 && next < 16)
                    {
                        e.SuppressKeyPress = true;
                        tblBits.GetControlFromPosition(next, 1)?.Focus();
                    }
                };

                tblBits.Controls.Add(valueLabel, i, 0);
                tblBits.Controls.Add(bitBox, i, 1);
            }

            for (int i = 0; i < 16; i++)
            {
                var box =
                    (TextBox)tblBits.GetControlFromPosition(i, 1)!;

                box.TextChanged += BitBox_TextChanged;
            }

            BitBox_TextChanged(null, EventArgs.Empty);
            ApplyTheme(this);
            Shown += (s, e) => QueueRequestCleanup();
        }

        private void SaveManualPrefix(DataGridViewRow row)
        {
            string text = row.Cells[3].Value?.ToString()?.Trim() ?? "";
            if (text.Length == 0) manualPrefixes.Remove(row);
            else manualPrefixes[row] = text;
        }

        private void PrefixKeyPress(object? sender, KeyPressEventArgs e)
        {
            if (!calculatingVlsm && !char.IsControl(e.KeyChar) &&
                gridRequests.CurrentCell?.ColumnIndex == 3)
                prefixEdited = true;
        }

        private void SetStatus(string text, bool error = false)
        {
            plannerStatus.Text = text;
            plannerStatus.ForeColor = error ? Color.FromArgb(255, 170, 170) : ink;
            plannerStatus.BackColor = error ? danger : surface;
        }

        private void AddRowError(DataGridViewRow row, string text, List<string> errors)
        {
            row.ErrorText = text;
            string name = row.Cells[0].Value?.ToString()?.Trim() ?? "";
            errors.Add($"Row {row.Index + 1}{(name.Length > 0 ? " (" + name + ")" : "")}: {text}");
        }

        private Button MakeButton(string text, EventHandler click)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 32,
                MinimumSize = new Size(95, 32),
                Margin = new Padding(6, 0, 0, 0)
            };
            button.Click += click;
            return button;
        }

        private void SetupInterface()
        {
            Text = "Networking Toolkit";
            ClientSize = new Size(1260, 600);
            MinimumSize = new Size(900, 450);
            StartPosition = FormStartPosition.CenterScreen;
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl1.SizeMode = TabSizeMode.Fixed;
            tabControl1.ItemSize = new Size(150, 34);
            tabControl1.DrawItem += (s, e) =>
            {
                using var brush = new SolidBrush(e.Index == tabControl1.SelectedIndex
                    ? surface : Color.FromArgb(33, 33, 33));
                e.Graphics.FillRectangle(brush, e.Bounds);
                TextRenderer.DrawText(e.Graphics, tabControl1.TabPages[e.Index].Text,
                    Font, e.Bounds, ink, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            tabControl1.SelectedIndexChanged += (s, e) => tabControl1.Invalidate();
            tabPage1.UseVisualStyleBackColor = false;
            tabPage2.UseVisualStyleBackColor = false;
            tabPage1.AutoScroll = true;

            decimalInput.SetBounds(150, 24, 200, 27);
            hexInput.SetBounds(150, 64, 200, 27);
            binaryInput.SetBounds(150, 104, 200, 27);
            label3.Location = new Point(16, 29);
            label1.Location = new Point(16, 69);
            label2.Location = new Point(16, 109);
            convertButton.SetBounds(365, 22, 120, 32);
            convertHexButton.SetBounds(365, 62, 120, 32);
            convertBinaryButton.SetBounds(365, 102, 120, 32);
            resultLabel.SetBounds(515, 25, 300, 110);
            tblBits.SetBounds(16, 185, 800, 100);
            tblBits.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            tabPage1.Resize += (s, e) =>
                tblBits.Width = Math.Max(764, tabPage1.ClientSize.Width - 32);
            Shown += (s, e) => tblBits.Width = Math.Max(764, tabPage1.ClientSize.Width - 32);
            lblBitResult.Location = new Point(16, 300);
            var resetBits = MakeButton("Reset converters", (s, e) =>
            {
                decimalInput.Clear(); hexInput.Clear(); binaryInput.Clear();
                foreach (TextBox box in tblBits.Controls.OfType<TextBox>()) box.Text = "0";
                resultLabel.Text = "Results will appear here";
            });
            resetBits.Location = new Point(16, 345);
            tabPage1.Controls.Add(resetBits);

            if (!gridRequests.Columns.Contains("SubnetMask"))
                gridRequests.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "SubnetMask", HeaderText = "Subnet mask", ReadOnly = true });
            gridRequests.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridRequests.RowHeadersWidth = 34;
            gridRequests.RowTemplate.Height = 32;
            gridRequests.ColumnHeadersHeight = 42;
            gridRequests.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            foreach (DataGridViewColumn column in gridRequests.Columns)
            {
                column.MinimumWidth = column.Index == 3 ? 60 : 90;
                column.FillWeight = column.Index == 3 ? 55 : 110;
            }
            if (gridRequests.Columns["SubnetMask"] is DataGridViewColumn maskColumn)
            {
                maskColumn.DisplayIndex = 4;
            }
            gridRequests.ShowCellErrors = true;
            gridRequests.ShowRowErrors = true;
            gridRequests.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;

            tabPage2.Controls.Remove(label5);
            tabPage2.Controls.Remove(txtNetwork);
            label5.Text = "Starting IP / CIDR:";
            label5.Margin = new Padding(0, 8, 8, 0);
            txtNetwork.Width = 190;
            txtNetwork.Margin = new Padding(0, 4, 12, 0);
            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 58,
                Padding = new Padding(12),
                WrapContents = false,
                AutoScroll = true
            };
            toolbar.Controls.Add(label5);
            toolbar.Controls.Add(txtNetwork);
            toolbar.Controls.Add(MakeButton("Reset table", ResetPlanner));
            toolbar.Controls.Add(MakeButton("Copy table", CopyPlanner));
            toolbar.Controls.Add(MakeButton("Export CSV", ExportPlanner));
            plannerStatus.Dock = DockStyle.Bottom;
            plannerStatus.Height = 82;
            plannerStatus.Multiline = true;
            plannerStatus.ReadOnly = true;
            plannerStatus.ScrollBars = ScrollBars.Vertical;
            plannerStatus.BorderStyle = BorderStyle.FixedSingle;
            plannerStatus.TabStop = false;
            gridRequests.Dock = DockStyle.Fill;
            tabPage2.Controls.Add(toolbar);
            tabPage2.Controls.Add(plannerStatus);
            gridRequests.BringToFront();
        }

        private void ApplyTheme(Control control)
        {
            control.BackColor = Color.FromArgb(33, 33, 33);
            control.ForeColor = ink;
            if (control is TextBox box)
            {
                box.BackColor = surface;
                box.ForeColor = ink;
                box.BorderStyle = BorderStyle.FixedSingle;
            }
            if (control is Button button)
            {
                button.UseVisualStyleBackColor = false;
                button.FlatStyle = FlatStyle.Flat;
                button.BackColor = surface;
                button.FlatAppearance.BorderColor = Color.FromArgb(74, 74, 74);
                button.FlatAppearance.MouseOverBackColor = Color.FromArgb(65, 65, 65);
            }
            if (control is DataGridView grid)
            {
                grid.EnableHeadersVisualStyles = false;
                grid.BackgroundColor = Color.FromArgb(33, 33, 33);
                grid.GridColor = Color.FromArgb(74, 74, 74);
                grid.BorderStyle = BorderStyle.None;
                grid.DefaultCellStyle.BackColor = surface;
                grid.DefaultCellStyle.ForeColor = ink;
                grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(75, 75, 75);
                grid.DefaultCellStyle.SelectionForeColor = Color.White;
                grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 40, 40);
                grid.ColumnHeadersDefaultCellStyle.ForeColor = ink;
                grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = surface;
                grid.RowHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 40, 40);
                grid.RowHeadersDefaultCellStyle.ForeColor = ink;
                grid.RowHeadersDefaultCellStyle.SelectionBackColor = surface;
                grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(43, 43, 43);
                return;
            }
            foreach (Control child in control.Controls) ApplyTheme(child);
        }

        private void ResetPlanner(object? sender, EventArgs e)
        {
            gridRequests.CancelEdit();
            calculatingVlsm = true;
            try { manualPrefixes.Clear(); gridRequests.Rows.Clear(); }
            finally { calculatingVlsm = false; }
            // Keep the starting IP for the next plan.
            QueueRequestCleanup();
        }

        private bool PrepareExport()
        {
            if (!gridRequests.EndEdit()) return false;
            RemoveEmptyRequestRows();
            CalculateVlsm();
            var rows = gridRequests.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).ToList();
            bool populated = rows.Any(r => r.Cells[2].Value != null);
            bool incomplete = rows.Any(r => r.Cells[2].Value == null);
            if (!populated || incomplete)
            {
                SetStatus("Cannot export: finish all requests and fix the errors first.", true);
                return false;
            }
            return true;
        }

        private string ExportText(bool csv)
        {
            var columns = gridRequests.Columns.Cast<DataGridViewColumn>()
                .Where(c => c.Visible).OrderBy(c => c.DisplayIndex).ToList();
            var lines = new List<string>();
            string delimiter = csv ? "," : "\t";
            string Encode(string value)
            {
                if (csv) return "\"" + value.Replace("\"", "\"\"") + "\"";
                return value.Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
            }
            lines.Add(string.Join(delimiter, columns.Select(c => Encode(c.HeaderText))));
            foreach (DataGridViewRow row in gridRequests.Rows)
            {
                if (row.IsNewRow) continue;
                lines.Add(string.Join(delimiter, columns.Select(c =>
                    Encode(row.Cells[c.Index].Value?.ToString() ?? ""))));
            }
            return string.Join(Environment.NewLine, lines);
        }

        private void CopyPlanner(object? sender, EventArgs e)
        {
            if (!PrepareExport()) return;
            try { Clipboard.SetText(ExportText(false)); SetStatus("Table copied. Paste it into Excel or your document."); }
            catch (System.Runtime.InteropServices.ExternalException)
            { SetStatus("Clipboard is busy. Try Copy table again.", true); }
        }

        private void ExportPlanner(object? sender, EventArgs e)
        {
            if (!PrepareExport()) return;
            using var dialog = new SaveFileDialog
            {
                Filter = "CSV file (*.csv)|*.csv",
                FileName = "VLSM-plan.csv",
                DefaultExt = "csv",
                AddExtension = true,
                OverwritePrompt = true
            };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                File.WriteAllText(dialog.FileName, ExportText(true), new UTF8Encoding(true));
                SetStatus("CSV exported successfully.");
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            { SetStatus("Could not save CSV: " + ex.Message, true); }
        }

        private void GridRequests_EditingControlShowing(
            object? sender,
            DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is TextBox box)
            {
                box.PreviewKeyDown -= GridRequests_PreviewKeyDown;
                box.KeyDown -= GridRequests_EditingKeyDown;
                box.KeyPress -= PrefixKeyPress;

                box.PreviewKeyDown += GridRequests_PreviewKeyDown;
                box.KeyDown += GridRequests_EditingKeyDown;
                box.KeyPress += PrefixKeyPress;
                box.BackColor = surface;
                box.ForeColor = ink;
            }
        }

        private void GridRequests_PreviewKeyDown(
            object? sender,
            PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Left ||
                e.KeyCode == Keys.Right ||
                e.KeyCode == Keys.Up ||
                e.KeyCode == Keys.Down)
            {
                e.IsInputKey = true;
            }
        }

        private void GridRequests_KeyDown(
            object? sender,
            KeyEventArgs e)
        {
            if (e.Modifiers == Keys.None &&
                e.KeyCode == Keys.Back)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                ClearRequestCell();
            }
        }

        private void GridRequests_EditingKeyDown(
            object? sender,
            KeyEventArgs e)
        {
            if (e.Modifiers != Keys.None)
                return;

            if (e.KeyCode == Keys.Back)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                ClearRequestCell();
                return;
            }

            if (e.KeyCode != Keys.Left &&
                e.KeyCode != Keys.Right &&
                e.KeyCode != Keys.Up &&
                e.KeyCode != Keys.Down)
            {
                return;
            }

            var cell = gridRequests.CurrentCell;

            if (cell == null)
                return;

            e.Handled = true;
            e.SuppressKeyPress = true;

            int row = cell.RowIndex;
            var columns = gridRequests.Columns.Cast<DataGridViewColumn>()
                .Where(c => c.Visible).OrderBy(c => c.DisplayIndex).ToList();
            int position = columns.FindIndex(c => c.Index == cell.ColumnIndex);
            switch (e.KeyCode)
            {
                case Keys.Left: position--; break;
                case Keys.Right: position++; break;
                case Keys.Up: row--; break;
                case Keys.Down: row++; break;
            }
            if (!gridRequests.EndEdit()) return;
            if (position >= 0 && position < columns.Count &&
                row >= 0 && row < gridRequests.Rows.Count)
                gridRequests.CurrentCell = gridRequests.Rows[row].Cells[columns[position].Index];
        }

        private void ClearRequestCell()
        {
            var cell = gridRequests.CurrentCell;

            if (cell == null || cell.ReadOnly)
                return;

            if (gridRequests.IsCurrentCellInEditMode &&
                gridRequests.EditingControl is TextBox box)
            {
                box.Clear();

                if (!gridRequests.EndEdit())
                    return;
            }
            else if (cell.OwningRow is DataGridViewRow row &&
                     !row.IsNewRow)
            {
                cell.Value = null;
            }

            if (cell.ColumnIndex == 3 && cell.OwningRow is DataGridViewRow prefixRow)
                manualPrefixes.Remove(prefixRow);
            QueueRequestCleanup();
        }

        private void QueueRequestCleanup()
        {
            if (cleaningRequests || calculatingVlsm ||
                cleanupQueued ||
                !IsHandleCreated ||
                IsDisposed ||
                Disposing)
            {
                return;
            }

            cleanupQueued = true;

            BeginInvoke(new Action(() =>
            {
                cleanupQueued = false;

                if (!IsDisposed && !Disposing)
                {
                    RemoveEmptyRequestRows();
                    CalculateVlsm();
                }
            }));
        }

        private void RemoveEmptyRequestRows()
        {
            if (cleaningRequests ||
                gridRequests.IsCurrentCellInEditMode)
            {
                return;
            }

            cleaningRequests = true;

            try
            {
                var selectedCell = gridRequests.CurrentCell;
                var selectedRow = selectedCell?.OwningRow;

                int selectedIndex = selectedCell?.RowIndex ?? 0;
                int selectedColumn = selectedCell?.ColumnIndex ?? 0;

                bool selectedRowRemoved = false;

                for (int i = gridRequests.Rows.Count - 1; i >= 0; i--)
                {
                    var row = gridRequests.Rows[i];

                    // Keep the automatic empty row at the bottom
                    if (row.IsNewRow)
                        continue;

                    bool subnetEmpty = string.IsNullOrWhiteSpace(
                        row.Cells[0].Value?.ToString());

                    bool hostsEmpty = string.IsNullOrWhiteSpace(
                        row.Cells[1].Value?.ToString());

                    if (subnetEmpty && hostsEmpty && !manualPrefixes.ContainsKey(row))
                    {
                        if (row == selectedRow)
                            selectedRowRemoved = true;

                        manualPrefixes.Remove(row);
                        gridRequests.Rows.RemoveAt(i);
                    }
                }

                if (selectedRowRemoved &&
                    gridRequests.Rows.Count > 0)
                {
                    int nextRow = Math.Min(
                        selectedIndex,
                        gridRequests.Rows.Count - 1);

                    gridRequests.CurrentCell =
                        gridRequests.Rows[nextRow]
                            .Cells[selectedColumn];
                }
            }
            finally
            {
                foreach (var row in manualPrefixes.Keys.ToList())
                    if (row.DataGridView != gridRequests || row.IsNewRow)
                        manualPrefixes.Remove(row);
                cleaningRequests = false;
            }
        }

        private void CalculateVlsm()
        {
            if (gridRequests.IsCurrentCellInEditMode || calculatingVlsm) return;
            calculatingVlsm = true;
            try
            {
                foreach (DataGridViewRow row in gridRequests.Rows)
                {
                    if (row.IsNewRow) continue;
                    row.ErrorText = "";
                    for (int c = 2; c < gridRequests.Columns.Count; c++)
                        if (c != 3) row.Cells[c].Value = null;
                    bool manual = manualPrefixes.TryGetValue(row, out string? text);
                    row.Cells[3].Value = manual ? text : null;
                    row.Cells[3].Style.BackColor = manual ? manualColor : Color.Empty;
                    row.Cells[3].ToolTipText = manual
                        ? "Manual prefix. Backspace returns to automatic sizing."
                        : "Automatic prefix. Type a prefix to override.";
                }
                txtNetwork.BackColor = surface;
                string input = txtNetwork.Text.Trim();
                if (input.Length == 0)
                {
                    SetStatus("Enter a starting IPv4 address; /prefix is optional.");
                    return;
                }
                string[] parts = input.Split('/');
                if (parts.Length > 2 || !TryReadIpv4(parts[0].Trim(), out uint address))
                {
                    txtNetwork.BackColor = danger;
                    SetStatus("Invalid starting IP. Example: 192.168.10.0 or 192.168.8.0/22", true);
                    return;
                }
                ulong start = address;
                ulong end = 1UL << 32;
                if (parts.Length == 2)
                {
                    if (!int.TryParse(parts[1].Trim(), out int parent) || parent < 0 || parent > 32)
                    {
                        txtNetwork.BackColor = danger;
                        SetStatus("Starting CIDR must have a prefix from /0 to /32.", true);
                        return;
                    }
                    ulong size = 1UL << (32 - parent);
                    start = ((ulong)address / size) * size;
                    end = start + size;
                }
                var requests = new List<(DataGridViewRow Row, ulong Size, int Prefix)>();
                var errors = new List<string>();
                foreach (DataGridViewRow row in gridRequests.Rows)
                {
                    if (row.IsNewRow) continue;
                    string hostsText = row.Cells[1].Value?.ToString()?.Trim() ?? "";
                    bool manual = manualPrefixes.TryGetValue(row, out string? prefixText);
                    if (hostsText.Length == 0 && !manual) continue;
                    ulong hosts = 0;
                    if (hostsText.Length > 0 &&
                        (!ulong.TryParse(hostsText, out hosts) || hosts < 1 || hosts > 4294967294UL))
                    {
                        AddRowError(row, "Hosts Needed must be a whole number from 1 to 4294967294.", errors);
                        continue;
                    }
                    ulong block = 4;
                    int prefix = 30;
                    if (manual)
                    {
                        string raw = prefixText!.Trim();
                        if (raw.StartsWith("/")) raw = raw.Substring(1);
                        if (!int.TryParse(raw, out prefix) || prefix < 0 || prefix > 30)
                        {
                            AddRowError(row, "Enter /0 to /30; network and broadcast addresses are reserved.", errors);
                            continue;
                        }
                        block = 1UL << (32 - prefix);
                        if (hosts > block - 2)
                        {
                            AddRowError(row, $"/{prefix} has {block - 2} usable hosts; {hosts} requested.", errors);
                            continue;
                        }
                    }
                    else
                    {
                        while (block < hosts + 2) { block *= 2; prefix--; }
                    }
                    requests.Add((row, block, prefix));
                }
                if (errors.Count > 0)
                {
                    SetStatus(string.Join(Environment.NewLine, errors), true);
                    return;
                }
                var results = new List<(DataGridViewRow Row, ulong Network, ulong Broadcast, int Prefix)>();
                ulong next = start;
                ulong allocated = 0;
                foreach (var request in requests.OrderByDescending(r => r.Size))
                {
                    ulong network = ((next + request.Size - 1) / request.Size) * request.Size;
                    ulong subnetEnd = network + request.Size;
                    if (subnetEnd > end)
                    {
                        string message = parts.Length == 2
                            ? "Requests do not fit inside the starting CIDR. Use a larger network (smaller prefix) or reduce the requests."
                            : "Requests exceed the remaining IPv4 address space after the starting IP.";
                        foreach (var item in requests) item.Row.ErrorText = message;
                        SetStatus(message, true);
                        return;
                    }
                    results.Add((request.Row, network, subnetEnd - 1, request.Prefix));
                    next = subnetEnd;
                    allocated += request.Size;
                }
                foreach (var r in results)
                {
                    r.Row.Cells[2].Value = FormatIpv4(r.Network);
                    r.Row.Cells[3].Value = $"/{r.Prefix}";
                    r.Row.Cells[4].Value = FormatIpv4(r.Network + 1);
                    r.Row.Cells[5].Value = FormatIpv4(r.Broadcast - 1);
                    r.Row.Cells[6].Value = FormatIpv4(r.Broadcast);
                    r.Row.Cells[7].Value = r.Broadcast - r.Network - 1;
                    ulong mask = r.Prefix == 0 ? 0 : (0xFFFFFFFFUL << (32 - r.Prefix)) & 0xFFFFFFFFUL;
                    r.Row.Cells["SubnetMask"].Value = FormatIpv4(mask);
                }
                ulong gaps = requests.Count == 0 ? 0 : next - start - allocated;
                SetStatus(requests.Count == 0
                    ? "Enter Hosts Needed or a manual Prefix. Press Enter or an arrow to finish editing."
                    : $"{requests.Count} subnets calculated. {allocated:N0} addresses allocated. " +
                      $"{gaps:N0} skipped for alignment. Yellow prefixes are manual overrides.");
            }
            finally { calculatingVlsm = false; }
        }

        private static bool TryReadIpv4(
            string text,
            out uint address)
        {
            address = 0;

            string[] octets = text.Split('.');

            if (octets.Length != 4)
                return false;

            foreach (string octet in octets)
            {
                if (octet.Length == 0 ||
                    octet.Any(character =>
                        character < '0' || character > '9') ||
                    !byte.TryParse(octet, out byte value))
                {
                    return false;
                }

                address = (address << 8) | value;
            }

            return true;
        }

        private static string FormatIpv4(ulong address)
        {
            return $"{(address >> 24) & 255}." +
                   $"{(address >> 16) & 255}." +
                   $"{(address >> 8) & 255}." +
                   $"{address & 255}";
        }

        private void BitBox_TextChanged(object? sender, EventArgs e)
        {
            int total = 0;

            for (int i = 0; i < 16; i++)
            {
                var box =
                    (TextBox)tblBits.GetControlFromPosition(i, 1)!;

                if (box.Text != "" &&
                    box.Text != "0" &&
                    box.Text != "1")
                {
                    lblBitResult.Text =
                        "Use only 0 or 1 in each box";
                    return;
                }

                if (box.Text == "1")
                    total += 1 << (15 - i);
            }

            lblBitResult.Text =
                $"Decimal: {total}  |  Hex: {total:X4}";
        }

        private void convertButton_Click(object? sender, EventArgs e)
        {
            if (!ushort.TryParse(decimalInput.Text, out ushort number))
            {
                resultLabel.Text =
                    "Enter a whole number from 0 to 65535.";
                return;
            }

            resultLabel.Text =
                $"Hex: {number:X4}\n" +
                $"Binary: {Convert.ToString(number, 2).PadLeft(16, '0')}";
        }

        private void convertHexButton_Click(object? sender, EventArgs e)
        {
            if (!ushort.TryParse(
                hexInput.Text,
                System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture,
                out ushort number))
            {
                resultLabel.Text =
                    "Enter a hex number from 0000 to FFFF.";
                return;
            }

            resultLabel.Text =
                $"Decimal: {number}\n" +
                $"Binary: {Convert.ToString(number, 2).PadLeft(16, '0')}";
        }

        private void convertBinaryButton_Click(object? sender, EventArgs e)
        {
            string bits = binaryInput.Text.Trim();

            if (bits.Length == 0 ||
                bits.Length > 16 ||
                bits.Any(bit => bit != '0' && bit != '1'))
            {
                resultLabel.Text =
                    "Enter 1 to 16 binary digits (0 and 1 only).";
                return;
            }

            ushort number = Convert.ToUInt16(bits, 2);

            resultLabel.Text =
                $"Decimal: {number}\n" +
                $"Hex: {number:X4}";
        }

        private void tabControl1_SelectedIndexChanged(
            object? sender, EventArgs e)
        {
        }

        private void tabPage2_Click(object? sender, EventArgs e)
        {
        }

        private void tabPage1_Click(object? sender, EventArgs e)
        {
        }

        private void textBox1_TextChanged(object? sender, EventArgs e)
        {
        }

        private void tableLayoutPanel1_Paint(
            object? sender, PaintEventArgs e)
        {
        }

        private void resultLabel_Click(object? sender, EventArgs e)
        {
        }

        private void gridRequests_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {

        }

        private void txtNetwork_TextChanged(object? sender, EventArgs e)
        {

        }
    }
}