using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace NetworkingToolkit;

public partial class Form1
{
    private DataGridView ipListGrid = null!;
    private Label ipListStatus = null!;
    private bool ipListCleanupQueued;

    private void SetupIpList()
    {
        var page = new TabPage("IP List")
        {
            BackColor = Color.FromArgb(29, 35, 45), Padding = new Padding(15)
        };
        tabControl1.TabPages.Add(page);
        var heading = new Label
        {
            Dock = DockStyle.Top, Height = 48,
            Text = "IP LIST  |  TOPOLOGY INVENTORY",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 16, FontStyle.Bold)
        };
        var hint = new Label
        {
            Dock = DockStyle.Top, Height = 33,
            Text = "Document device addresses and physical or virtual interfaces. One row per interface/address.",
            ForeColor = Color.FromArgb(183, 204, 226), Font = new Font("Segoe UI", 9)
        };
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 51,
            FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
            BackColor = page.BackColor
        };
        Button MakeAction(string title, Action action)
        {
            var button = new Button
            {
                Text = title, AutoSize = false, Width = 126, Height = 35,
                Margin = new Padding(0, 3, 9, 0), FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(48, 62, 80), ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(91, 116, 143);
            button.Click += (_, _) => action();
            return button;
        }
        ipListGrid = new DataGridView
        {
            Name = "TopologyIpList",
            Dock = DockStyle.Fill,
            BackgroundColor = Color.FromArgb(29, 35, 45),
            GridColor = Color.FromArgb(80, 99, 125),
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AllowUserToAddRows = true,
            AllowUserToDeleteRows = true,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            MultiSelect = false,
            EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 37,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        };
        ipListGrid.RowTemplate.Height = 31;
        ipListGrid.DefaultCellStyle.BackColor = Color.FromArgb(37, 47, 61);
        ipListGrid.DefaultCellStyle.ForeColor = Color.FromArgb(230, 240, 250);
        ipListGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(58, 89, 119);
        ipListGrid.DefaultCellStyle.SelectionForeColor = Color.White;
        ipListGrid.DefaultCellStyle.Font = new Font("Segoe UI", 9);
        ipListGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(44, 55, 70);
        ipListGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 56, 84);
        ipListGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        ipListGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        foreach (var (header, width) in new (string, int)[]
        {
            ("Device", 136), ("Interface / Port", 82),
            ("IP Address", 120), ("Prefix / Mask", 104), ("VLAN", 76),
            ("Default Gateway", 112), ("Role", 84), ("Notes", 220)
        })
        {
            var column = new DataGridViewTextBoxColumn
            {
                HeaderText = header, Name = header.Replace(" ", "").Replace("/", ""),
                Width = width, SortMode = DataGridViewColumnSortMode.NotSortable
            };
            ipListGrid.Columns.Add(column);
        }
        // Every column can be resized independently by dragging its header divider.
        ipListGrid.AllowUserToResizeColumns = true;
        foreach (DataGridViewColumn col in ipListGrid.Columns)
        {
            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            col.MinimumWidth = 55;
            col.Resizable = DataGridViewTriState.True;
        }
        // Give Notes the remaining room when available without forcing the other columns wider.
        // The full grid remains horizontally scrollable on narrow windows.
        ipListGrid.Columns["Notes"].Width = 220;
        ipListStatus = new Label
        {
            Dock = DockStyle.Bottom, Height = 30,
            ForeColor = Color.FromArgb(170, 202, 226),
            Text = "Enter a device and its interface details. Duplicate addresses will be highlighted."
        };
        actions.Controls.Add(MakeAction("Export Excel", () => ExportExcel(ipListGrid, "topology-ip-list.xlsx")));
        actions.Controls.Add(MakeAction("Copy table", () =>
        {
            ipListGrid.EndEdit();
            var lines = new List<string>();
            lines.Add(string.Join("\t", ipListGrid.Columns.Cast<DataGridViewColumn>().Select(c => c.HeaderText)));
            foreach (DataGridViewRow row in ipListGrid.Rows)
                if (!row.IsNewRow && row.Cells.Cast<DataGridViewCell>().Any(c => !string.IsNullOrWhiteSpace(Convert.ToString(c.Value)))) lines.Add(string.Join("\t", row.Cells.Cast<DataGridViewCell>().Select(c => Convert.ToString(c.Value) ?? "")));
            Clipboard.SetText(string.Join(Environment.NewLine, lines));
        }));
        ipListGrid.KeyDown += (_, e) =>
        {
            // When a cell is selected, clear it without entering text-edit mode.
            if ((e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete) &&
                !ipListGrid.IsCurrentCellInEditMode && ipListGrid.CurrentCell is { ReadOnly: false } cell &&
                !cell.OwningRow.IsNewRow)
            {
                e.SuppressKeyPress = true;
                cell.Value = null;
                UpdateIpListValidation();
            }
        };
        ipListGrid.CellEndEdit += (_, _) => QueueIpListCleanup();
        ipListGrid.CellValueChanged += (_, _) => { UpdateIpListValidation(); QueueIpListCleanup(); };
        ipListGrid.RowsRemoved += (_, _) => UpdateIpListValidation();
        ipListGrid.UserAddedRow += (_, _) => UpdateIpListValidation();
        ipListGrid.DataError += (_, e) => { e.ThrowException = false; };
        page.Controls.Add(ipListGrid);
        page.Controls.Add(ipListStatus);
        page.Controls.Add(actions);
        page.Controls.Add(hint);
        page.Controls.Add(heading);
        RebuildNav();
    }

    private void QueueIpListCleanup()
    {
        if (ipListCleanupQueued || !ipListGrid.IsHandleCreated) return;
        ipListCleanupQueued = true;
        ipListGrid.BeginInvoke(new Action(() =>
        {
            ipListCleanupQueued = false;
            if (ipListGrid.IsDisposed || ipListGrid.IsCurrentCellInEditMode) return;
            // WinForms keeps exactly one automatic new-row placeholder.
            // Prune previously committed rows that have become completely empty.
            foreach (DataGridViewRow row in ipListGrid.Rows.Cast<DataGridViewRow>().ToArray())
            {
                if (!row.IsNewRow && !row.Cells.Cast<DataGridViewCell>().Any(c => !string.IsNullOrWhiteSpace(Convert.ToString(c.Value))))
                {
                    if (ipListGrid.CurrentCell?.RowIndex == row.Index) ipListGrid.CurrentCell = null;
                    ipListGrid.Rows.Remove(row);
                }
            }
            UpdateIpListValidation();
        }));
    }

    private void UpdateIpListValidation()
    {
        if (ipListGrid == null) return;
        var seen = new Dictionary<string, List<DataGridViewCell>>(StringComparer.OrdinalIgnoreCase);
        int count = 0, invalid = 0;
        foreach (DataGridViewRow row in ipListGrid.Rows)
        {
            if (row.IsNewRow) continue;
            // Do not count otherwise empty rows as inventory entries.
            if (row.Cells.Cast<DataGridViewCell>().Any(c => !string.IsNullOrWhiteSpace(Convert.ToString(c.Value)))) count++;
            var cell = row.Cells["IPAddress"];
            cell.Style.BackColor = Color.Empty;
            cell.ToolTipText = "";
            string ip = (Convert.ToString(cell.Value) ?? "").Trim();
            if (ip.Length == 0) continue;
            if (!IPAddress.TryParse(ip, out var parsed) ||
                parsed.AddressFamily is not (AddressFamily.InterNetwork or AddressFamily.InterNetworkV6))
            {
                invalid++;
                cell.Style.BackColor = Color.FromArgb(102, 52, 57);
                cell.ToolTipText = "Invalid IPv4 or IPv6 address";
                continue;
            }
            string canonical = parsed.ToString();
            if (!seen.TryGetValue(canonical, out var cells)) seen[canonical] = cells = new List<DataGridViewCell>();
            cells.Add(cell);
        }
        int duplicates = 0;
        foreach (var cells in seen.Values.Where(c => c.Count > 1))
        {
            duplicates += cells.Count;
            foreach (var cell in cells)
            {
                cell.Style.BackColor = Color.FromArgb(112, 70, 42);
                cell.ToolTipText = "Duplicate IP address: verify this is intentional";
            }
        }
        ipListStatus.Text = $"{count} entries  |  {duplicates} duplicate-address cells  |  {invalid} invalid addresses  |  Changes are not saved until exported.";
    }
}
