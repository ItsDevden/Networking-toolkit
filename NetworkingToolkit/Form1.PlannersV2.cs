using System;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Windows.Forms;

namespace NetworkingToolkit;

public partial class Form1
{
    private void SetupStaticRoutes()
    {
        var page = new TabPage("Static route planner") { BackColor = Color.FromArgb(33, 33, 33), Padding = new Padding(14) };
        tabControl1.TabPages.Add(page);
        var routes = new DataGridView
        {
            Dock = DockStyle.Fill, AllowUserToAddRows = true, AllowUserToDeleteRows = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            CellBorderStyle = DataGridViewCellBorderStyle.Single, GridColor = Color.FromArgb(85, 96, 113),
            RowHeadersVisible = false, RowTemplate = { Height = 32 },
            BackgroundColor = Color.FromArgb(33, 33, 33)
        };
        routes.Columns.Add("Destination", "Destination network (IPv4)");
        routes.Columns.Add("Prefix", "Prefix / CIDR");
        routes.Columns.Add("NextHop", "Next hop IP");
        routes.Columns.Add("Interface", "Exit interface (optional)");
        routes.Rows.Add("192.168.20.0", "24", "10.0.0.2", "");
        var output = new RichTextBox
        {
            Dock = DockStyle.Bottom, Height = 200, ReadOnly = true, Font = new Font("Consolas", 10),
            BackColor = Color.FromArgb(28, 34, 45), ForeColor = Color.FromArgb(232, 238, 245)
        };
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(7) };
        toolbar.Controls.Add(MakeButton("Generate routes", (_, _) => BuildRoutes()));
        toolbar.Controls.Add(MakeButton("Copy commands", (_, _) => { if (output.Text.Length > 0) Clipboard.SetText(output.Text); }));
        toolbar.Controls.Add(MakeButton("Add route", (_, _) => routes.Rows.Add()));
        toolbar.Controls.Add(MakeButton("Export Excel", (_, _) => ExportExcel(routes, "Static-routes.xlsx")));
        page.Controls.Add(routes);
        page.Controls.Add(output);
        page.Controls.Add(toolbar);
        void BuildRoutes()
        {
            routes.EndEdit();
            var result = new System.Text.StringBuilder("! Cisco IOS static routes\nconfigure terminal\n");
            int errors = 0;
            foreach (DataGridViewRow row in routes.Rows)
            {
                if (row.IsNewRow) continue;
                row.ErrorText = "";
                string ip = Convert.ToString(row.Cells[0].Value)?.Trim() ?? "";
                string prefixText = Convert.ToString(row.Cells[1].Value)?.Trim().TrimStart('/') ?? "";
                string nextHop = Convert.ToString(row.Cells[2].Value)?.Trim() ?? "";
                string exitInterface = Convert.ToString(row.Cells[3].Value)?.Trim() ?? "";
                if (ip.Length == 0 && nextHop.Length == 0 && prefixText.Length == 0) continue;
                if (!IPAddress.TryParse(ip, out var address) || address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork ||
                    !int.TryParse(prefixText, out int prefix) || prefix < 0 || prefix > 32 ||
                    !IPAddress.TryParse(nextHop, out var hop) || hop.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork ||
                    exitInterface.Any(c => !(char.IsLetterOrDigit(c) || c == '/' || c == '.' || c == '-')))
                {
                    row.ErrorText = "Invalid IPv4 network, prefix, next-hop or interface.";
                    errors++; continue;
                }
                uint mask = prefix == 0 ? 0U : uint.MaxValue << (32 - prefix);
                uint target = BitConverter.ToUInt32(address.GetAddressBytes().Reverse().ToArray(), 0);
                uint network = target & mask;
                static string Ip(uint v) => string.Join(".", BitConverter.GetBytes(v).Reverse());
                result.AppendLine($"ip route {Ip(network)} {Ip(mask)} {(exitInterface.Length > 0 ? exitInterface + " " : "")}{nextHop}");
            }
            result.AppendLine("end\ncopy running-config startup-config");
            output.Text = result.ToString() + (errors > 0 ? $"\n! {errors} invalid row(s) skipped. Hover over red row indicator." : "");
        }
        BuildRoutes();
    }

    private void SetupIpv6Flsm()
    {
        var page = new TabPage("IPv6 FLSM") { BackColor = Color.FromArgb(33, 33, 33), Padding = new Padding(12) };
        tabControl1.TabPages.Add(page);
        var input = new TextBox { Width = 265, Text = "2001:db8:1234::/48" };
        var count = new NumericUpDown { Width = 90, Minimum = 1, Maximum = 4096, Value = 1 };
        var target = new NumericUpDown { Width = 69, Minimum = 0, Maximum = 128, Value = 64 };
        var status = new Label { Dock = DockStyle.Bottom, Height = 60, ForeColor = ink, Padding = new Padding(8) };
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            CellBorderStyle = DataGridViewCellBorderStyle.Single, GridColor = Color.FromArgb(85, 96, 113), RowHeadersVisible = false,
            ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText
        };
        grid.Columns.Add("Index", "#"); grid.Columns.Add("Network", "Network ID"); grid.Columns.Add("Prefix", "Prefix");
        grid.Columns.Add("Size", "Address count (power of 2)");
        grid.Columns[0].FillWeight = 30; grid.Columns[0].MinimumWidth = 45;
        grid.Columns[1].FillWeight = 165; grid.Columns[2].FillWeight = 55;
        grid.Columns[3].FillWeight = 135;
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 86, Padding = new Padding(8), AutoScroll = false, WrapContents = true };
        toolbar.Controls.Add(new Label { Text = "IPv6 network / CIDR:", AutoSize = true, Margin = new Padding(0, 9, 5, 0) });
        toolbar.Controls.Add(input);
        toolbar.Controls.Add(new Label { Text = "Target prefix /", AutoSize = true, Margin = new Padding(12, 9, 4, 0) });
        toolbar.Controls.Add(target);
        toolbar.Controls.Add(new Label { Text = "Show:", AutoSize = true, Margin = new Padding(12, 9, 5, 0) });
        toolbar.Controls.Add(count);
        toolbar.Controls.Add(MakeButton("Calculate", (_, _) => Calculate()));
        toolbar.Controls.Add(MakeButton("Copy table", (_, _) => { grid.SelectAll(); var data = grid.GetClipboardContent(); if (data != null) Clipboard.SetDataObject(data); grid.ClearSelection(); }));
        toolbar.Controls.Add(MakeButton("Export Excel", (_, _) => ExportExcel(grid, "IPv6-FLSM.xlsx")));
        // Allow the export button to wrap onto a visible second row on smaller windows.
        page.Controls.Add(grid); page.Controls.Add(status); page.Controls.Add(toolbar);
        input.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Calculate(); } };
        grid.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter && count.Value < count.Maximum)
            {
                e.SuppressKeyPress = true;
                count.Value++;
            }
            else if (e.KeyCode == Keys.Back && count.Value > count.Minimum)
            {
                e.SuppressKeyPress = true;
                count.Value--;
            }
        };
        count.ValueChanged += (_, _) => Calculate();
        target.ValueChanged += (_, _) => Calculate();
        input.TextChanged += (_, _) => Calculate();
        void Calculate()
        {
            grid.Rows.Clear();
            var parts = input.Text.Trim().Split('/');
            if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var address) ||
                address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6 ||
                !int.TryParse(parts[1], out int prefix) || prefix < 0 || prefix > 128)
            { status.Text = "Enter a valid IPv6 CIDR such as 2001:db8:1234::/48"; return; }
            int requested = (int)count.Value;
            int child = (int)target.Value;
            if (child < prefix) { status.Text = "Target prefix must be greater than or equal to the parent prefix."; return; }
            BigInteger available = BigInteger.One << (child - prefix);
            if (requested > available) { status.Text = $"Only {available:N0} /{child} subnets fit inside /{prefix}."; return; }
            BigInteger value = new BigInteger(address.GetAddressBytes(), isUnsigned: true, isBigEndian: true);
            BigInteger parent = (value >> (128 - prefix)) << (128 - prefix);
            BigInteger step = BigInteger.One << (128 - child);
            for (int i = 0; i < requested; i++)
            {
                var ip = new IPAddress((new byte[16].Concat((parent + i * step).ToByteArray(isUnsigned: true, isBigEndian: true)).TakeLast(16)).ToArray());
                grid.Rows.Add(i + 1, ip.ToString(), "/" + child, "2^" + (128 - child));
            }
            status.Text = $"Parent /{prefix}  |  Target /{child}  |  {requested} shown  |  {available:N0} possible subnets  |  Network bits: {child}, interface bits: {128-child}";
        }
        Calculate();
    }
}
