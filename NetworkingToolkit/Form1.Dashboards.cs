using System;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace NetworkingToolkit;

public partial class Form1
{
    private readonly Color dashboardCard = Color.FromArgb(39, 48, 63);
    private readonly Color dashboardActive = Color.FromArgb(38, 115, 146);
    private readonly Color dashboardText = Color.FromArgb(220, 232, 247);
    private bool syncingIpv4;
    private bool syncingIpv6;
    private readonly TextBox[] ipv6Groups = new TextBox[8];
    private readonly Button[] ipv6Bits = new Button[16];
    private readonly Panel[] ipv6GroupCards = new Panel[8];
    private readonly Label ipv6CompressedLabel = new();
    private readonly Label ipv6ExpandedLabel = new();
    private readonly Label ipv6NetworkLabel = new();
    private readonly Label ipv6TypeLabel = new();
    private readonly Label ipv6BitLabel = new();
    private readonly NumericUpDown ipv6Prefix = new();
    private readonly NumericUpDown ipv6SelectedGroup = new();
    private readonly Label ipv6Status = new();
    private readonly Button[] ipv4Tiles = new Button[16];
    private readonly Label ipv4ActiveBits = new();

    private Label DashLabel(string text, int size = 10, bool bold = false) => new()
    {
        Text = text, ForeColor = dashboardText, AutoSize = false,
        Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
        TextAlign = ContentAlignment.MiddleLeft
    };
    private Button DashButton(string text, EventHandler action, int width = 116)
    {
        var b = new Button { Text = text, Width = width, Height = 37, Cursor = Cursors.Hand,
            BackColor = Color.FromArgb(56, 72, 92), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        b.FlatAppearance.BorderColor = Color.FromArgb(92, 116, 145);
        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(58, 111, 149);
        b.Click += action; return b;
    }
    private Panel DashCard(int x, int y, int w, int h)
    {
        var panel = new Panel { Bounds = new Rectangle(x, y, w, h), BackColor = dashboardCard };
        panel.Paint += (_, e) => ControlPaint.DrawBorder(e.Graphics, panel.ClientRectangle,
            Color.FromArgb(70, 94, 120), ButtonBorderStyle.Solid);
        return panel;
    }
    private void SetupFinishedDashboards()
    {
        SetupIpv4Dashboard();
        SetupIpv6Calculator();
        RebuildNav();
    }
    private void SetupIpv4Dashboard()
    {
        // Keep existing original converter and bit handlers; arrange the controls into a modern workspace.
        var page = tabPage1;
        page.AutoScroll = true;
        foreach (Control c in new Control[] { label1, label2, label3, convertButton, convertHexButton, convertBinaryButton, resultLabel }) c.Visible = false;
        var title = DashLabel("IPv4 / BINARY CALCULATOR", 18, true);
        title.SetBounds(24, 16, 750, 38); page.Controls.Add(title);
        var subtitle = DashLabel("Edit any value, or toggle the weighted bits. All three formats stay in sync.", 9);
        subtitle.SetBounds(26, 53, 750, 29); page.Controls.Add(subtitle);
        var fields = new[] { ("DECIMAL", decimalInput), ("HEXADECIMAL", hexInput), ("BINARY (16 BITS)", binaryInput) };
        for (int i = 0; i < 3; i++)
        {
            var card = DashCard(24 + i * 292, 100, 276, 112);
            var label = DashLabel(fields[i].Item1, 9, true); label.SetBounds(14, 9, 245, 26);
            var input = fields[i].Item2;
            input.Parent = card; input.SetBounds(14, 45, 247, 38);
            input.Font = new Font("Consolas", 12, FontStyle.Bold);
            input.BackColor = Color.FromArgb(60, 77, 100); input.ForeColor = Color.White;
            card.Controls.Add(label); page.Controls.Add(card);
        }
        var editorCard = DashCard(24, 226, 860, 292); page.Controls.Add(editorCard);
        var editorTitle = DashLabel("16-BIT BINARY EDITOR", 11, true);
        editorTitle.SetBounds(16, 9, 350, 28); editorCard.Controls.Add(editorTitle);
        var editorHint = DashLabel("Toggle bits to calculate values", 9);
        editorHint.SetBounds(16, 37, 500, 23); editorCard.Controls.Add(editorHint);
        tblBits.Visible = false; // The original grid remains the single source of truth for calculations.
        for (int i = 0; i < 16; i++)
        {
            int bitIndex = i;
            int weight = 1 << (15 - i);
            var tile = new Button
            {
                Bounds = new Rectangle(15 + (i % 8) * 105, 70 + (i / 8) * 58, 99, 52),
                Text = weight + Environment.NewLine + "0", Font = new Font("Consolas", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(49, 60, 80),
                ForeColor = Color.FromArgb(197, 213, 233), Cursor = Cursors.Hand,
                AccessibleName = $"Toggle bit with weight {weight}"
            };
            tile.FlatAppearance.BorderColor = Color.FromArgb(83, 100, 122);
            tile.FlatAppearance.MouseOverBackColor = Color.FromArgb(55, 91, 126);
            tile.Click += (_, _) => SetIpv4Value((ushort)(GetIpv4Value() ^ (1 << (15 - bitIndex))));
            editorCard.Controls.Add(tile); ipv4Tiles[i] = tile;
        }
        var decValue = DashLabel("LIVE VALUE", 8, true); decValue.SetBounds(17, 194, 125, 18); editorCard.Controls.Add(decValue);
        lblBitResult.Parent = editorCard; lblBitResult.SetBounds(17, 216, 460, 31);
        lblBitResult.Font = new Font("Consolas", 13, FontStyle.Bold);
        ipv4ActiveBits.SetBounds(568, 216, 255, 31);
        ipv4ActiveBits.Font = new Font("Consolas", 12, FontStyle.Bold);
        ipv4ActiveBits.ForeColor = dashboardText; editorCard.Controls.Add(ipv4ActiveBits);
        var actions = new FlowLayoutPanel { Left = 24, Top = 531, Width = 760, Height = 53, WrapContents = false };
        actions.Controls.Add(DashButton("Reset", (_, _) => SetIpv4Value(0)));
        actions.Controls.Add(DashButton("Set all", (_, _) => SetIpv4Value(65535)));
        actions.Controls.Add(DashButton("Invert", (_, _) => SetIpv4Value((ushort)~GetIpv4Value())));
        actions.Controls.Add(DashButton("Copy binary", (_, _) => Clipboard.SetText(Convert.ToString(GetIpv4Value(), 2).PadLeft(16, '0'))));
        page.Controls.Add(actions);
        decimalInput.TextChanged += (_, _) => SyncIpv4(decimalInput, 10);
        hexInput.TextChanged += (_, _) => SyncIpv4(hexInput, 16);
        binaryInput.TextChanged += (_, _) => SyncIpv4(binaryInput, 2);
        foreach (var bit in tblBits.Controls.OfType<TextBox>()) bit.TextChanged += (_, _) =>
        {
            if (!syncingIpv4) SetIpv4Value(GetIpv4Value());
        };
        SetIpv4Value(0);
        foreach (var oldTitle in page.Controls.OfType<Label>().Where(x => x.Text.Contains("IPv4 / BINARY WORKSPACE"))) oldTitle.Visible = false;
        // Existing controls that sat at fixed offsets shouldn't obscure the new workspace.
        foreach (var b in page.Controls.OfType<Button>().Where(x => x != convertButton && x != convertHexButton && x != convertBinaryButton).ToArray())
            if (b.Top >= 350 && b.Top <= 410) b.Visible = false;
    }
    private ushort GetIpv4Value()
    {
        int v = 0;
        for (int i = 0; i < 16; i++)
            if (tblBits.GetControlFromPosition(i, 1) is TextBox box && box.Text == "1") v |= (1 << (15 - i));
        return (ushort)v;
    }
    private void SyncIpv4(TextBox source, int radix)
    {
        if (syncingIpv4) return;
        string s = source.Text.Trim().Replace(" ", "").Replace("_", "");
        if (radix == 16 && s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s[2..];
        try
        {
            if (s.Length == 0) return;
            int value = Convert.ToInt32(s, radix);
            if (value < 0 || value > ushort.MaxValue) return;
            SetIpv4Value((ushort)value);
        }
        catch (FormatException) { }
        catch (OverflowException) { }
    }
    private void SetIpv4Value(ushort value)
    {
        if (syncingIpv4) return;
        syncingIpv4 = true;
        try
        {
            decimalInput.Text = value.ToString();
            hexInput.Text = value.ToString("X4");
            binaryInput.Text = Convert.ToString(value, 2).PadLeft(16, '0');
            for (int i = 0; i < 16; i++)
                if (tblBits.GetControlFromPosition(i, 1) is TextBox b) b.Text = ((value >> (15 - i)) & 1).ToString();
            resultLabel.Text = $"Decimal {value} | Hex {value:X4}";
            lblBitResult.Text = $"DEC {value}    HEX {value:X4}";
            ipv4ActiveBits.Text = $"ACTIVE BITS  {System.Numerics.BitOperations.PopCount((uint)value)} / 16";
            for (int i = 0; i < ipv4Tiles.Length; i++)
            {
                var tile = ipv4Tiles[i];
                if (tile == null) continue;
                bool active = (value & (1 << (15 - i))) != 0;
                tile.Text = (1 << (15 - i)) + Environment.NewLine + (active ? "1" : "0");
                tile.BackColor = active ? dashboardActive : Color.FromArgb(49, 60, 80);
                tile.ForeColor = active ? Color.White : Color.FromArgb(176, 193, 214);
            }
        }
        finally { syncingIpv4 = false; }
    }
    private void SetupIpv6Calculator()
    {
        var page = new TabPage("IPv6 Subnetting")
        {
            BackColor = Color.FromArgb(29, 35, 45), AutoScroll = true,
            AutoScrollMinSize = new Size(940, 810)
        };
        tabControl1.TabPages.Add(page);

        var heading = DashLabel("IPv6 ADDRESS EXPLORER", 18, true);
        heading.SetBounds(24, 12, 850, 39); page.Controls.Add(heading);
        var caption = DashLabel("Explore all 128 bits • select a hexadecimal block • network bits blue, host bits slate", 9);
        caption.SetBounds(26, 51, 895, 25); page.Controls.Add(caption);

        var controlsCard = DashCard(24, 86, 886, 77);
        page.Controls.Add(controlsCard);
        var prefLabel = DashLabel("PREFIX", 9, true);
        prefLabel.SetBounds(16, 17, 66, 34); controlsCard.Controls.Add(prefLabel);
        ipv6Prefix.Minimum = 0; ipv6Prefix.Maximum = 128; ipv6Prefix.Value = 64;
        ipv6Prefix.SetBounds(84, 17, 78, 33); controlsCard.Controls.Add(ipv6Prefix);
        var groupLabel = DashLabel("EDIT BLOCK", 9, true);
        groupLabel.SetBounds(190, 17, 90, 34); controlsCard.Controls.Add(groupLabel);
        ipv6SelectedGroup.Minimum = 1; ipv6SelectedGroup.Maximum = 8; ipv6SelectedGroup.Value = 1;
        ipv6SelectedGroup.SetBounds(280, 17, 66, 33); controlsCard.Controls.Add(ipv6SelectedGroup);
        var prevGroup = DashButton("◀", (_, _) => ipv6SelectedGroup.Value = ipv6SelectedGroup.Value == 1 ? 8 : ipv6SelectedGroup.Value - 1, 38);
        prevGroup.SetBounds(360, 17, 38, 33); controlsCard.Controls.Add(prevGroup);
        var nextGroup = DashButton("▶", (_, _) => ipv6SelectedGroup.Value = ipv6SelectedGroup.Value == 8 ? 1 : ipv6SelectedGroup.Value + 1, 38);
        nextGroup.SetBounds(405, 17, 38, 33); controlsCard.Controls.Add(nextGroup);
        ipv6Status.SetBounds(473, 18, 390, 32);
        ipv6Status.ForeColor = Color.FromArgb(164, 198, 227); controlsCard.Controls.Add(ipv6Status);

        for (int i = 0; i < 8; i++)
        {
            int groupNumber = i;
            var card = DashCard(24 + (i % 4) * 224, 179 + (i / 4) * 100, 214, 88);
            var label = DashLabel($"BLOCK {i + 1}  ·  BITS {i * 16 + 1}–{(i + 1) * 16}", 8, true);
            label.SetBounds(12, 7, 194, 24); card.Controls.Add(label);
            card.Cursor = Cursors.Hand;
            label.Cursor = Cursors.Hand;
            card.Click += (_, _) => ipv6SelectedGroup.Value = groupNumber + 1;
            label.Click += (_, _) => ipv6SelectedGroup.Value = groupNumber + 1;
            new ToolTip().SetToolTip(card, $"Select block {groupNumber + 1} to edit its bits");
            var input = new TextBox
            {
                MaxLength = 4, Text = "0000", Font = new Font("Consolas", 13, FontStyle.Bold),
                BackColor = Color.FromArgb(58, 75, 96), ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            input.SetBounds(12, 40, 188, 29);
            input.KeyDown += (_, e) =>
            {
                // The blocks are arranged as two rows of four, not a single vertical list.
                int destination = e.KeyCode == Keys.Down ? groupNumber + 4 :
                                  e.KeyCode == Keys.Up ? groupNumber - 4 : -1;
                if (destination >= 0 && destination < ipv6Groups.Length && ipv6Groups[destination] != null)
                {
                    e.SuppressKeyPress = true;
                    ipv6Groups[destination].Focus();
                    ipv6Groups[destination].SelectAll();
                }
            };
            input.Enter += (_, _) => ipv6SelectedGroup.Value = groupNumber + 1;
            input.TextChanged += (_, _) => UpdateIpv6FromGroups();
            card.Controls.Add(input); page.Controls.Add(card);
            ipv6Groups[i] = input; ipv6GroupCards[i] = card;
        }

        var bitCard = DashCard(24, 388, 886, 185); page.Controls.Add(bitCard);
        var bitTitle = DashLabel("16-BIT BLOCK EDITOR", 12, true);
        bitTitle.SetBounds(16, 10, 470, 30); bitCard.Controls.Add(bitTitle);
        ipv6BitLabel.SetBounds(16, 39, 835, 25);
        ipv6BitLabel.ForeColor = dashboardText; bitCard.Controls.Add(ipv6BitLabel);
        for (int i = 0; i < 16; i++)
        {
            int bitIndex = i;
            var bit = DashButton("0", (_, _) => ToggleIpv6Bit(bitIndex), 99);
            bit.SetBounds(13 + (i % 8) * 108, 70 + (i / 8) * 54, 100, 48);
            bit.Margin = Padding.Empty;
            bit.Font = new Font("Consolas", 9, FontStyle.Bold);
            bitCard.Controls.Add(bit);
            ipv6Bits[i] = bit;
        }

        var resultCard = DashCard(24, 587, 886, 165); page.Controls.Add(resultCard);
        int y = 9;
        foreach (var (label, value) in new[]
        {
            ("COMPRESSED", ipv6CompressedLabel), ("EXPANDED", ipv6ExpandedLabel),
            ("NETWORK ADDRESS", ipv6NetworkLabel), ("ADDRESS TYPE", ipv6TypeLabel)
        })
        {
            var labelControl = DashLabel(label, 9, true);
            labelControl.SetBounds(14, y, 165, 29); resultCard.Controls.Add(labelControl);
            value.ForeColor = Color.FromArgb(180, 233, 239);
            value.Font = new Font("Consolas", 10);
            value.SetBounds(180, y, 688, 30); resultCard.Controls.Add(value);
            y += 38;
        }
        var actions = new FlowLayoutPanel { Left = 24, Top = 766, Width = 886, Height = 48, WrapContents = false };
        actions.Controls.Add(DashButton("Copy compressed", (_, _) => Clipboard.SetText(ipv6CompressedLabel.Text), 160));
        actions.Controls.Add(DashButton("Copy expanded", (_, _) => Clipboard.SetText(ipv6ExpandedLabel.Text), 145));
        actions.Controls.Add(DashButton("Copy network", (_, _) => Clipboard.SetText(ipv6NetworkLabel.Text), 145));
        actions.Controls.Add(DashButton("Reset IPv6", (_, _) => SetIpv6Address(IPAddress.Parse("2001:db8:1234:5678::1")), 125));
        page.Controls.Add(actions);
        ipv6Prefix.ValueChanged += (_, _) => RefreshIpv6();
        ipv6SelectedGroup.ValueChanged += (_, _) => RefreshIpv6();
        SetIpv6Address(IPAddress.Parse("2001:db8:1234:5678::1"));
    }
    private bool TryIpv6Bytes(out byte[] data)
    {
        data = new byte[16];
        for (int i = 0; i < 8; i++)
        {
            if (!ushort.TryParse(ipv6Groups[i].Text, System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out ushort val)) return false;
            data[i * 2] = (byte)(val >> 8); data[i * 2 + 1] = (byte)val;
        }
        return true;
    }
    private void SetIpv6Address(IPAddress address)
    {
        syncingIpv6 = true;
        var b = address.GetAddressBytes();
        for (int i = 0; i < 8; i++) ipv6Groups[i].Text = ((b[i * 2] << 8) | b[i * 2 + 1]).ToString("x4");
        syncingIpv6 = false; RefreshIpv6();
    }
    private void UpdateIpv6FromGroups()
    {
        if (!syncingIpv6) RefreshIpv6();
    }
    private void ToggleIpv6Bit(int position)
    {
        int group = (int)ipv6SelectedGroup.Value - 1;
        if (!ushort.TryParse(ipv6Groups[group].Text, System.Globalization.NumberStyles.HexNumber,
            System.Globalization.CultureInfo.InvariantCulture, out ushort val)) return;
        val ^= (ushort)(1 << (15 - position));
        ipv6Groups[group].Text = val.ToString("x4"); RefreshIpv6();
    }
    private void RefreshIpv6()
    {
        if (syncingIpv6 || ipv6Groups.Any(g => g is null)) return;
        if (!TryIpv6Bytes(out var bytes))
        {
            ipv6Status.Text = "Enter 1–4 hexadecimal digits in every group.";
            ipv6CompressedLabel.Text = ipv6ExpandedLabel.Text = ipv6NetworkLabel.Text = ipv6TypeLabel.Text = "Invalid group";
            return;
        }
        int prefix = (int)ipv6Prefix.Value, selected = (int)ipv6SelectedGroup.Value - 1;
        var address = new IPAddress(bytes);
        ipv6CompressedLabel.Text = address.ToString();
        ipv6ExpandedLabel.Text = string.Join(":", Enumerable.Range(0, 8).Select(i => ((bytes[2 * i] << 8) | bytes[2 * i + 1]).ToString("x4")));
        var network = bytes.ToArray();
        for (int bit = prefix; bit < 128; bit++) network[bit / 8] &= (byte)~(1 << (7 - (bit % 8)));
        ipv6NetworkLabel.Text = new IPAddress(network) + "/" + prefix;
        ipv6TypeLabel.Text = address.Equals(IPAddress.IPv6Loopback) ? "Loopback (::1)" :
            address.Equals(IPAddress.IPv6Any) ? "Unspecified (::)" :
            bytes[0] == 0xff ? "Multicast (ff00::/8)" :
            bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80 ? "Link-local (fe80::/10)" :
            (bytes[0] & 0xfe) == 0xfc ? "Unique local (fc00::/7)" :
            (bytes[0] & 0xe0) == 0x20 ? "Global unicast (2000::/3)" : "Other / special-purpose";
        ipv6Status.Text = $"Network: {prefix} bits   |   Interface: {128 - prefix} bits";
        int segmentValue = (bytes[selected * 2] << 8) | bytes[selected * 2 + 1];
        ipv6BitLabel.Text = $"Editing group {selected + 1} of 8: {segmentValue:X4}   •   click a group card above to switch";
        for (int i = 0; i < 8; i++)
        {
            int networkBits = Math.Clamp(prefix - i * 16, 0, 16);
            ipv6GroupCards[i].BackColor = i == selected ? Color.FromArgb(41, 107, 139) :
                networkBits == 16 ? Color.FromArgb(32, 69, 91) :
                networkBits == 0 ? dashboardCard : Color.FromArgb(65, 69, 87);
        }
        for (int i = 0; i < 16; i++)
        {
            bool active = (segmentValue & (1 << (15 - i))) != 0;
            bool netBit = selected * 16 + i < prefix;
            ipv6Bits[i].Text = $"{(1 << (15 - i))}\n{(active ? "1" : "0")}  {(netBit ? "NET" : "HOST")}";
            ipv6Bits[i].BackColor = active ? (netBit ? dashboardActive : Color.FromArgb(101, 88, 137))
                : (netBit ? Color.FromArgb(44, 75, 94) : Color.FromArgb(57, 61, 72));
        }
    }
}
