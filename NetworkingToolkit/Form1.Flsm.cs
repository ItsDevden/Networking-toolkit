using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NetworkingToolkit
{
    public partial class Form1
    {
        private readonly TextBox flsmParent = new() { Width = 195, Text = "192.168.1.0/24" };
        private readonly NumericUpDown flsmCount = new() { Width = 85, Minimum = 1, Maximum = 4096, Value = 4 };
        private readonly TextBox flsmStatus = new() { ReadOnly = true, Multiline = true, Dock = DockStyle.Bottom, Height = 68 };
        private readonly DataGridView flsmGrid = new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
            ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText
        };

        private void SetupFlsm()
        {
            var page = new TabPage("FLSM Planner") { UseVisualStyleBackColor = false };
            tabControl1.TabPages.Add(page);
            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, Height = 58, Padding = new Padding(12),
                AutoScroll = true, WrapContents = false
            };
            toolbar.Controls.Add(new Label { Text = "Parent IPv4 / CIDR:", AutoSize = true, Margin = new Padding(0, 8, 8, 0) });
            toolbar.Controls.Add(flsmParent);
            toolbar.Controls.Add(new Label { Text = "Subnets needed:", AutoSize = true, Margin = new Padding(14, 8, 8, 0) });
            toolbar.Controls.Add(flsmCount);
            toolbar.Controls.Add(MakeButton("Calculate", (s, e) => CalculateFlsm()));
            toolbar.Controls.Add(MakeButton("Copy table", (s, e) =>
            {
                if (flsmGrid.Rows.Count == 0) return;
                flsmGrid.SelectAll();
                var data = flsmGrid.GetClipboardContent();
                if (data != null) Clipboard.SetDataObject(data);
                flsmGrid.ClearSelection();
            }));
            toolbar.Controls.Add(MakeButton("Export CSV", (s, e) => ExportFlsm()));
            foreach (string name in new[] { "#", "Network ID", "CIDR", "Subnet mask", "First usable", "Last usable", "Broadcast", "Usable hosts" })
                flsmGrid.Columns.Add(name.Replace(" ", ""), name);
            flsmGrid.RowTemplate.Height = 29;
            flsmGrid.ColumnHeadersHeight = 38;
            page.Controls.Add(flsmGrid);
            page.Controls.Add(flsmStatus);
            page.Controls.Add(toolbar);
            flsmParent.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; CalculateFlsm(); } };
            flsmCount.ValueChanged += (s, e) => CalculateFlsm();
            CalculateFlsm();
        }

        private void CalculateFlsm()
        {
            flsmGrid.Rows.Clear();
            string[] parts = flsmParent.Text.Trim().Split('/');
            if (parts.Length != 2 || !TryReadIpv4(parts[0], out uint ip) ||
                !int.TryParse(parts[1], out int parentPrefix) || parentPrefix < 0 || parentPrefix > 30)
            {
                FlsmMessage("Enter a valid network in CIDR format, e.g. 192.168.1.0/24 (prefix /0 to /30).", true);
                return;
            }
            int requested = (int)flsmCount.Value;
            int borrowedBits = 0;
            while ((1UL << borrowedBits) < (ulong)requested) borrowedBits++;
            int childPrefix = parentPrefix + borrowedBits;
            // Conventional IPv4 LAN subnets reserve network and broadcast addresses.
            if (childPrefix > 30)
            {
                FlsmMessage($"Cannot fit {requested} conventional subnets inside /{parentPrefix}. Maximum: {1UL << (30 - parentPrefix):N0} with /30 subnets.", true);
                return;
            }
            ulong parentSize = 1UL << (32 - parentPrefix);
            ulong parentNetwork = ((ulong)ip / parentSize) * parentSize;
            ulong childSize = 1UL << (32 - childPrefix);
            ulong capacity = 1UL << borrowedBits;
            ulong mask = (0xFFFFFFFFUL << (32 - childPrefix)) & 0xFFFFFFFFUL;
            // Display exactly the requested number; capacity may be greater because of binary subdivision.
            for (int i = 0; i < requested; i++)
            {
                ulong network = parentNetwork + (ulong)i * childSize;
                ulong broadcast = network + childSize - 1;
                flsmGrid.Rows.Add(i + 1, FormatIpv4(network), $"/{childPrefix}", FormatIpv4(mask),
                    FormatIpv4(network + 1), FormatIpv4(broadcast - 1),
                    FormatIpv4(broadcast), childSize - 2);
            }
            FlsmMessage($"Parent: {FormatIpv4(parentNetwork)}/{parentPrefix}  |  Each subnet: /{childPrefix}, {childSize - 2:N0} usable hosts" +
                $"  |  {requested:N0} shown, {capacity:N0} possible equal subnets ({capacity - (ulong)requested:N0} unused).", false);
        }

        private void FlsmMessage(string message, bool isError)
        {
            flsmStatus.Text = message;
            flsmStatus.BackColor = isError ? danger : surface;
            flsmStatus.ForeColor = isError ? Color.FromArgb(255, 170, 170) : ink;
        }

        private void ExportFlsm()
        {
            if (flsmGrid.Rows.Count == 0) return;
            using var dialog = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = "FLSM-plan.csv" };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            static string EscapeCsv(object? value) => "\"" + (value?.ToString() ?? "").Replace("\"", "\"\"") + "\"";
            var lines = new System.Collections.Generic.List<string> {
                string.Join(",", flsmGrid.Columns.Cast<DataGridViewColumn>().Select(c => EscapeCsv(c.HeaderText)))
            };
            foreach (DataGridViewRow row in flsmGrid.Rows)
                lines.Add(string.Join(",", row.Cells.Cast<DataGridViewCell>().Select(c => EscapeCsv(c.Value))));
            File.WriteAllLines(dialog.FileName, lines, new UTF8Encoding(true));
        }
    }
}
