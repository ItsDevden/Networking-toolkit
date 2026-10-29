using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

namespace NetworkingToolkit;

public partial class Form1
{
    private readonly Color navBackground = Color.FromArgb(27, 33, 44);
    private readonly Color navAccent = Color.FromArgb(77, 158, 190);
    private readonly Color fieldBackground = Color.FromArgb(48, 62, 80);
    private FlowLayoutPanel navigation = null!;
    private Panel sidebar = null!;
    private Label brand = null!;
    private readonly Dictionary<TabPage, Button> navButtons = new();
    private bool sidebarCollapsed;
    private System.Windows.Forms.Timer? sidebarAnimation;
    private bool sidebarAnimating;
    private const int SidebarOpenWidth = 290;
    private const int SidebarClosedWidth = 96;

    private void SetupNavigation()
    {
        // Keep all existing pages and their event wiring. Only the navigation is replaced.
        Controls.Remove(tabControl1);
        var shell = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(29, 35, 45) };
        Controls.Add(shell);
        sidebar = new Panel { Dock = DockStyle.Left, Width = 290, BackColor = navBackground };
        shell.Controls.Add(sidebar);
        tabControl1.Dock = DockStyle.Fill;
        tabControl1.Appearance = TabAppearance.FlatButtons;
        tabControl1.SizeMode = TabSizeMode.Fixed;
        tabControl1.ItemSize = new Size(1, 1);
        tabControl1.Multiline = true;
        shell.Controls.Add(tabControl1);
        tabControl1.BringToFront();
        var sidebarLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
            Margin = Padding.Empty, Padding = Padding.Empty, BackColor = navBackground };
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sidebarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sidebar.Controls.Add(sidebarLayout);
        var top = new Panel { Dock = DockStyle.Fill, BackColor = navBackground, Padding = new Padding(0, 4, 0, 5) };
        sidebarLayout.Controls.Add(top, 0, 0);
        brand = new Label
        {
            Text = "NETWORKING\nTOOLKIT", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.White, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(18, 12, 0, 0)
        };
        var toggle = new Button
        {
            Text = "", Dock = DockStyle.Right, Width = 64, FlatStyle = FlatStyle.Flat,
            BackColor = navBackground, ForeColor = Color.White, Cursor = Cursors.Hand
        };
        toggle.FlatAppearance.BorderSize = 0;
        toggle.ImageAlign = ContentAlignment.MiddleCenter;
        try
        {
            using var iconStream = typeof(Form1).Assembly.GetManifestResourceStream("NetworkingToolkit.DenDev.png") ?? typeof(Form1).Assembly.GetManifestResourceStream("NetworkingToolkit.DenDev.ico");
            if (iconStream != null)
            {
                if (iconStream != null)
                {
                    using var source = Image.FromStream(iconStream);
                    var target = new Bitmap(56, 56);
                    using (var g = Graphics.FromImage(target))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        g.DrawImage(source, new Rectangle(0, 0, 56, 56));
                    }
                    toggle.Image = target;
                }
            }
        }
        catch (Exception) { toggle.Text = "☰"; } // Fallback only when the resource cannot load.
        toggle.AccessibleName = "Expand or collapse sidebar";
        toggle.FlatAppearance.MouseOverBackColor = Color.FromArgb(59, 71, 90);
        toggle.Click += (_, _) => AnimateSidebar();
        top.Controls.Add(brand);
        top.Padding = new Padding(0, 6, 0, 8);
        top.Controls.Add(toggle);
        var navHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 12, 8, 8), BackColor = navBackground };
        navigation = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, Padding = new Padding(0, 0, 0, 0), BackColor = navBackground
        };
        navHost.Controls.Add(navigation);
        sidebarLayout.Controls.Add(navHost, 0, 1);
        var moveBar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 55, BackColor = navBackground, Padding = new Padding(9) };
        var up = new Button { Text = "↑ Move", Width = 99, Height = 34, BackColor = fieldBackground, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        var down = new Button { Text = "↓ Move", Width = 99, Height = 34, BackColor = fieldBackground, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        up.Click += (_, _) => MoveCurrentPage(-1);
        down.Click += (_, _) => MoveCurrentPage(1);
        moveBar.Controls.Add(up); moveBar.Controls.Add(down);
        // Move buttons are intentionally hidden; leave them outside the layout grid.
        sidebar.Controls.Add(moveBar);
        moveBar.Visible = false; // Navigation reordering is not needed in normal use.
        sidebarLayout.BringToFront();
        navigation.Resize += (_, _) => ResizeNavButtons();
        tabControl1.SelectedIndexChanged += (_, _) => HighlightNavigation();
        RebuildNav();
    }

    // A WinForms TableLayoutPanel plus many native buttons repaints badly when
    // its parent width is animated frame-by-frame. Switch only between stable sizes.
    private void AnimateSidebar()
    {
        if (sidebar == null) return;
        sidebarCollapsed = !sidebarCollapsed;
        sidebar.SuspendLayout();
        navigation.SuspendLayout();
        try
        {
            brand.Visible = !sidebarCollapsed;
            sidebar.Width = sidebarCollapsed ? SidebarClosedWidth : SidebarOpenWidth;
            RebuildNav();
        }
        finally
        {
            navigation.ResumeLayout(true);
            sidebar.ResumeLayout(true);
        }
    }

    private void RebuildNav()
    {
        if (navigation == null) return;
        navigation.SuspendLayout();
        navigation.Controls.Clear();
        navButtons.Clear();
        AddNavHeading("TOOLKIT");
        string[] toolkitOrder = { "Subnetting", "IPv6 Subnetting", "VLSM Planner", "FLSM Planner", "IPv6 FLSM", "IP List" };
        foreach (string name in toolkitOrder)
        {
            var page = tabControl1.TabPages.Cast<TabPage>().FirstOrDefault(p => p.Text == name);
            if (page != null) AddNavigationPage(page);
        }
        AddNavHeading("INFO & REFERENCE");
        foreach (var page in tabControl1.TabPages.Cast<TabPage>().Where(p => Equals(p.Tag, "info") && p.Text != "Router / Switch CLI"))
            AddNavigationPage(page);
        var cliPage = tabControl1.TabPages.Cast<TabPage>().FirstOrDefault(p => p.Text == "Router / Switch CLI");
        if (cliPage != null) AddNavigationPage(cliPage);
        navigation.ResumeLayout();
        HighlightNavigation();
    }

    private void AddNavHeading(string heading)
    {
        navigation.Controls.Add(new Label
        {
            Text = sidebarCollapsed ? (heading == "TOOLKIT" ? "⚙" : "ⓘ") : heading, ForeColor = Color.FromArgb(150, 164, 191),
            Height = 34, Width = sidebarCollapsed ? 68 : 257,
            Font = new Font(sidebarCollapsed ? "Segoe UI Symbol" : "Segoe UI", sidebarCollapsed ? 15 : 8, FontStyle.Bold),
            TextAlign = sidebarCollapsed ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft,
            Margin = new Padding(sidebarCollapsed ? 1 : 7, 14, 0, 4)
        });
    }

    private void AddNavigationPage(TabPage page)
    {
        // Compact navigation uses visual glyphs instead of numeric placeholders.
        string shortName = page.Text switch
        {
            "IP List" => "▤", "Subnetting" => "▦", "IPv6 Subnetting" => "⬡", "VLSM Planner" => "◫", "FLSM Planner" => "▤",
            "IPv6 FLSM" => "⬡", "Router / Switch CLI" => "⌘",
            "Cisco command library" => "≡", "Network reference" => "◎",
            "Troubleshooting" => "⚒", _ => "◆"
        };
        var button = new Button
        {
            Text = sidebarCollapsed ? shortName : "  " + page.Text,
            TextAlign = sidebarCollapsed ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft,
            Height = 43, Width = sidebarCollapsed ? 68 : 257, FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(220, 230, 245), BackColor = navBackground,
            Font = new Font(sidebarCollapsed ? "Segoe UI Symbol" : "Segoe UI", sidebarCollapsed ? 19f : 9.3f), Cursor = Cursors.Hand,
            Margin = new Padding(1, 4, 1, 4), Tag = page
        };
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Color.FromArgb(51, 62, 81);
        button.FlatAppearance.MouseOverBackColor = fieldBackground;
        new ToolTip { InitialDelay = 350, ReshowDelay = 150 }.SetToolTip(button, page.Text);
        button.Click += (_, _) => tabControl1.SelectedTab = page;
        navigation.Controls.Add(button);
        navButtons[page] = button;
    }

    private void MoveCurrentPage(int direction)
    {
        var current = tabControl1.SelectedTab;
        if (current == null) return;
        var pages = tabControl1.TabPages.Cast<TabPage>().ToList();
        int from = pages.IndexOf(current), to = from + direction;
        if (to < 0 || to >= pages.Count || !Equals(pages[from].Tag, pages[to].Tag)) return;
        tabControl1.TabPages.Remove(current);
        tabControl1.TabPages.Insert(to, current);
        tabControl1.SelectedTab = current;
        RebuildNav();
    }

    private void ResizeNavButtons()
    {
        foreach (var b in navButtons.Values)
            b.Width = Math.Max(40, navigation.ClientSize.Width - navigation.Padding.Horizontal - 4);
    }

    private void HighlightNavigation()
    {
        foreach (var pair in navButtons)
        {
            bool selected = pair.Key == tabControl1.SelectedTab;
            pair.Value.BackColor = selected ? fieldBackground : navBackground;
            pair.Value.FlatAppearance.BorderColor = selected ? navAccent : Color.FromArgb(51, 62, 81);
            pair.Value.ForeColor = selected ? Color.White : Color.FromArgb(212, 220, 235);
        }
    }

    private void SetupInfoPages()
    {
        MakeInfoPage("Cisco command library", new[]
        {
            ("show ip interface brief", "Interface addresses and up/down status"),
            ("show running-config", "Inspect current configuration"),
            ("show ip route", "Routing table and default route"),
            ("show ip nat translations", "Active NAT translations"),
            ("show access-lists", "ACL entries and hit counters"),
            ("show vlan brief", "VLAN membership (switch)"),
            ("show interfaces trunk", "Trunk status and allowed VLANs"),
            ("show cdp neighbors detail", "Discover directly connected Cisco devices"),
            ("show ip dhcp binding", "DHCP addresses leased"),
            ("show version", "IOS version, uptime and platform"),
            ("copy running-config startup-config", "Save running configuration"),
            ("ping 8.8.8.8", "Test Layer 3 reachability"),
            ("traceroute 8.8.8.8", "Trace the path towards a host"),
            ("terminal length 0", "Disable paginated show output")
        });
        MakeInfoPage("Network reference", new[]
        {
            ("Private IPv4", "10.0.0.0/8  |  172.16.0.0/12  |  192.168.0.0/16"),
            ("Loopback", "127.0.0.0/8 (IPv4)  |  ::1 (IPv6)"),
            ("Link-local", "169.254.0.0/16 (IPv4)  |  fe80::/10 (IPv6)"),
            ("IPv6 ULA", "fc00::/7 (commonly fd00::/8 locally assigned)"),
            ("IPv6 global unicast", "2000::/3"),
            ("Default routes", "0.0.0.0/0  |  ::/0"),
            ("/24", "256 addresses; 254 usable conventionally"),
            ("/30", "4 addresses; 2 usable conventionally"),
            ("/31", "Point-to-point links may use both addresses (RFC 3021)"),
            ("/32", "Single IPv4 host route"),
            ("DHCP", "UDP ports 67 (server), 68 (client)"),
            ("DNS", "UDP/TCP port 53"),
            ("SSH", "TCP port 22"),
            ("HTTP / HTTPS", "TCP ports 80 / 443")
        });
        MakeInfoPage("Troubleshooting", new[]
        {
            ("01  Physical", "Check cables, link LEDs, interface shutdown state and speed/duplex"),
            ("02  Local config", "Check IP, prefix, mask, gateway and DNS addresses"),
            ("03  Loopback", "Ping 127.0.0.1; if this fails, inspect the local TCP/IP stack"),
            ("04  Gateway", "Ping the default gateway to verify LAN reachability"),
            ("05  Routing", "Check show ip route and the next hop in both directions"),
            ("06  Internet by IP", "Ping 8.8.8.8. If it works but names fail, investigate DNS"),
            ("07  DNS", "Use nslookup or Resolve-DnsName; verify DNS server and forwarders"),
            ("08  NAT", "Check inside/outside interfaces, ACL matches and NAT translations"),
            ("09  DHCP", "Verify pool, excluded addresses, gateway and helper addresses"),
            ("10  ACL / firewall", "Inspect inbound/outbound ACLs, host firewall and rule order"),
            ("11  Tracing", "Use tracert / traceroute to locate the failing hop"),
            ("12  Confirm", "Retest from client and save the working configuration")
        });
        RebuildNav();
    }

    // Expandable, searchable command reference: compact until a command is selected.
    private void MakeCompactCommandLibrary(string name, (string title, string detail)[] items)
    {
        var examples = new Dictionary<string, (string category, string example)>
        {
            ["show ip interface brief"] = ("Interfaces", "R1# show ip interface brief\r\nInterface              IP-Address      OK? Method Status                Protocol\r\nGigabitEthernet0/0     10.0.0.1        YES manual up                    up"),
            ["show running-config"] = ("Basics", "R1# show running-config\r\nBuilding configuration...\r\nhostname R1\r\ninterface GigabitEthernet0/0\r\n ip address 10.0.0.1 255.255.255.0"),
            ["show ip route"] = ("Routing", "R1# show ip route\r\nC 10.0.0.0/24 is directly connected, GigabitEthernet0/0\r\nS* 0.0.0.0/0 [1/0] via 10.0.0.254"),
            ["show ip nat translations"] = ("NAT", "R1# show ip nat translations\r\nPro  Inside global       Inside local        Outside local       Outside global"),
            ["show access-lists"] = ("Security", "R1# show access-lists\r\nStandard IP access list 1\r\n 10 permit 10.0.0.0, wildcard bits 0.0.0.255"),
            ["show vlan brief"] = ("Switching", "S1# show vlan brief\r\nVLAN Name                             Status    Ports\r\n1    default                          active    Fa0/1"),
            ["show interfaces trunk"] = ("Switching", "S1# show interfaces trunk\r\nPort        Mode         Encapsulation  Status        Native vlan"),
            ["show cdp neighbors detail"] = ("Discovery", "R1# show cdp neighbors detail\r\nDevice ID: S1\r\nIP address: 10.0.0.2"),
            ["show ip dhcp binding"] = ("DHCP", "R1# show ip dhcp binding\r\nIP address       Client-ID/Hardware address        Lease expiration"),
            ["show version"] = ("Basics", "R1# show version\r\nCisco IOS Software ..."),
            ["copy running-config startup-config"] = ("Basics", "R1# copy running-config startup-config\r\nDestination filename [startup-config]?\r\nBuilding configuration...\r\n[OK]"),
            ["ping 8.8.8.8"] = ("Troubleshooting", "R1# ping 8.8.8.8\r\n!!!!!\r\nSuccess rate is 100 percent (5/5)"),
            ["traceroute 8.8.8.8"] = ("Troubleshooting", "R1# traceroute 8.8.8.8\r\nTracing the route to 8.8.8.8"),
            ["terminal length 0"] = ("Basics", "R1# terminal length 0")
        };
        var page = new TabPage(name) { Tag = "info", BackColor = Color.FromArgb(29, 35, 45), Padding = new Padding(20) };
        tabControl1.TabPages.Add(page);
        var title = new Label { Dock = DockStyle.Top, Height = 48, Text = "Cisco command library", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.White };
        var search = new TextBox { Dock = DockStyle.Top, PlaceholderText = "Search command, category or description...", BackColor = fieldBackground, ForeColor = Color.White };
        var filter = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = fieldBackground, ForeColor = Color.White, Height = 32 };
        StyleCliCombo(filter);
        filter.Items.Add("All categories");
        foreach (var category in examples.Values.Select(x => x.category).Distinct().OrderBy(x => x)) filter.Items.Add(category);
        filter.SelectedIndex = 0;
        var list = new BufferedInfoList { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = false, FlowDirection = FlowDirection.TopDown, Padding = new Padding(0, 8, 14, 0), BackColor = Color.FromArgb(29, 35, 45) };
        page.Controls.Add(list); page.Controls.Add(filter); page.Controls.Add(search); page.Controls.Add(title);
        string? expanded = null;
        void Refresh()
        {
            list.SuspendLayout(); list.Controls.Clear();
            foreach (var item in items)
            {
                var entry = examples.TryGetValue(item.title, out var ex) ? ex : ("Basics", item.title);
                if (filter.SelectedIndex > 0 && filter.SelectedItem?.ToString() != entry.Item1) continue;
                if (!(item.title + item.detail + entry.Item1).Contains(search.Text, StringComparison.OrdinalIgnoreCase)) continue;
                bool open = expanded == item.title;
                int width = Math.Max(520, list.ClientSize.Width - 30);
                var panel = new Panel { Width = width, Height = open ? 154 : 58, Margin = new Padding(0, 0, 0, 5), BackColor = Color.FromArgb(39, 48, 63) };
                var header = new Button { Left = 3, Top = 3, Width = width - 95, Height = 50, Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right, TextAlign = ContentAlignment.MiddleLeft, Text = (open ? "▾  " : "▸  ") + item.title + "     ·     " + item.detail, FlatStyle = FlatStyle.Flat, BackColor = panel.BackColor, ForeColor = Color.White, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 9) };
                header.UseVisualStyleBackColor = false;
                header.FlatAppearance.BorderSize = 0;
                header.Click += (_, _) => { expanded = open ? null : item.title; Refresh(); };
                var copy = new Button { Text = "Copy", Left = width - 82, Top = 12, Width = 72, Height = 31, Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.FromArgb(55, 70, 90), Cursor = Cursors.Hand };
                copy.Click += (_, _) => Clipboard.SetText(item.title);
                panel.Controls.Add(header); panel.Controls.Add(copy);
                if (open)
                {
                    var exampleText = new TextBox { Left = 10, Top = 62, Width = width - 110, Height = 82, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top, Text = entry.Item2, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.FromArgb(30, 39, 52), ForeColor = Color.FromArgb(191, 228, 242), Font = new Font("Consolas", 9) };
                    var copyExample = new Button { Text = "Copy CLI", Left = width - 94, Top = 70, Width = 83, Height = 30, Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(55, 70, 90), ForeColor = Color.White, Cursor = Cursors.Hand };
                    copyExample.Click += (_, _) => Clipboard.SetText(entry.Item2);
                    panel.Controls.Add(exampleText); panel.Controls.Add(copyExample);
                }
                list.Controls.Add(panel);
            }
            list.ResumeLayout();
        }
        search.TextChanged += (_, _) => Refresh();
        filter.SelectedIndexChanged += (_, _) => Refresh();
        list.Resize += (_, _) => { foreach (Control c in list.Controls) c.Width = Math.Max(520, list.ClientSize.Width - 30); };
        Refresh();
    }

    private void MakeInfoPage(string name, (string title, string detail)[] items)
    {
        if (name == "Cisco command library")
        {
            MakeCompactCommandLibrary(name, items);
            return;
        }
        MakeExpandableInfoPage(name, items);

    }

    private void MakeExpandableInfoPage(string name, (string title, string detail)[] items)
    {
        var page = new TabPage(name) { Tag = "info", BackColor = Color.FromArgb(29, 35, 45), Padding = new Padding(20) };
        tabControl1.TabPages.Add(page);
        var title = new Label { Text = name, Dock = DockStyle.Top, Height = 48,
            Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.White };
        var search = new TextBox { Dock = DockStyle.Top, PlaceholderText = "Search topics or explanations...",
            BackColor = fieldBackground, ForeColor = Color.White };
        var list = new BufferedInfoList { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = false,
            FlowDirection = FlowDirection.TopDown, Padding = new Padding(0, 8, 14, 0), BackColor = Color.FromArgb(29, 35, 45) };
        page.Controls.Add(list); page.Controls.Add(search); page.Controls.Add(title);
        string? expanded = null;
        void Refresh()
        {
            int scroll = list.VerticalScroll.Value;
            list.SuspendLayout(); list.Controls.Clear();
            foreach (var item in items)
            {
                if (!(item.title + " " + item.detail).Contains(search.Text, StringComparison.OrdinalIgnoreCase)) continue;
                bool open = expanded == item.title;
                int width = Math.Max(350, list.ClientSize.Width - 30);
                var panel = new Panel { Width = width, Height = open ? 133 : 56,
                    Margin = new Padding(0, 0, 0, 5), BackColor = Color.FromArgb(39, 48, 63) };
                var header = new Button { Left = 3, Top = 3, Width = width - 92, Height = 49,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    TextAlign = ContentAlignment.MiddleLeft, FlatStyle = FlatStyle.Flat,
                    Text = (open ? "▾  " : "▸  ") + item.title,
                    ForeColor = Color.White, BackColor = panel.BackColor,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
                header.UseVisualStyleBackColor = false;
                header.FlatAppearance.BorderSize = 0;
                header.FlatAppearance.MouseOverBackColor = Color.FromArgb(55, 70, 90);
                header.Click += (_, _) => { expanded = open ? null : item.title; Refresh(); };
                panel.Controls.Add(header);
                var copy = new Button { Text = "Copy", Left = width - 82, Top = 12, Width = 72, Height = 30,
                    Anchor = AnchorStyles.Right | AnchorStyles.Top, FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(55, 70, 90), ForeColor = Color.White, Cursor = Cursors.Hand };
                copy.Click += (_, _) => Clipboard.SetText(item.detail);
                panel.Controls.Add(copy);
                if (open)
                {
                    var content = new TextBox { Left = 12, Top = 64, Width = width - 24, Height = 58,
                        Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                        Text = item.detail, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
                        BackColor = Color.FromArgb(30, 39, 52), ForeColor = Color.FromArgb(205, 223, 242),
                        BorderStyle = BorderStyle.None, Font = new Font("Consolas", 10) };
                    panel.Controls.Add(content);
                }
                list.Controls.Add(panel);
            }
            list.ResumeLayout();
            if (scroll > 0 && list.VerticalScroll.Maximum >= scroll) list.AutoScrollPosition = new Point(0, scroll);
        }
        search.TextChanged += (_, _) => Refresh();
        list.Resize += (_, _) => { foreach (Control c in list.Controls) c.Width = Math.Max(350, list.ClientSize.Width - 30); };
        Refresh();
        page.BackColor = Color.FromArgb(29, 35, 45);
        list.BackColor = Color.FromArgb(29, 35, 45);
    }

    private void SetupExportButtons()
    {
        // Distinguish interactive fields and buttons from static text, including dynamically built CLI fields.
        void Style(Control root)
        {
            foreach (Control child in root.Controls)
            {
                if (child is TextBox tb && !tb.ReadOnly)
                {
                    tb.BackColor = fieldBackground; tb.ForeColor = Color.White;
                    tb.BorderStyle = BorderStyle.FixedSingle;
                }
                if (child is NumericUpDown n) { n.BackColor = fieldBackground; n.ForeColor = Color.White; }
                if (child is ComboBox cb) { cb.BackColor = fieldBackground; cb.ForeColor = Color.White; }
                if (child is Button b && !navButtons.Values.Contains(b))
                {
                    b.Cursor = Cursors.Hand;
                    b.FlatStyle = FlatStyle.Flat;
                    b.FlatAppearance.BorderColor = navAccent;
                    b.FlatAppearance.BorderSize = 1;
                    b.FlatAppearance.MouseOverBackColor = fieldBackground;
                }
                Style(child);
            }
        }
        Style(tabControl1);
        Shown += (_, _) => Style(tabControl1);
    }

    private void ExportExcel(DataGridView grid, string fileName, Func<bool>? preflight = null)
    {
        if (preflight != null && !preflight()) return;
        if (grid.Rows.Cast<DataGridViewRow>().All(r => r.IsNewRow)) return;
        using var dialog = new SaveFileDialog { Filter = "Excel workbook (*.xlsx)|*.xlsx", FileName = fileName, DefaultExt = "xlsx", AddExtension = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { ExcelExporter.Write(dialog.FileName, grid); MessageBox.Show(this, "Excel workbook exported successfully.", "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception ex) { MessageBox.Show(this, "Export failed: " + ex.Message, "Export error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}

internal static class ExcelExporter
{
    private static readonly XNamespace S = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static void Entry(ZipArchive zip, string path, XDocument doc)
    {
        using var stream = zip.CreateEntry(path, CompressionLevel.Optimal).Open();
        doc.Save(stream);
    }
    private static string Col(int index)
    {
        var result = "";
        for (int n = index + 1; n > 0; n = (n - 1) / 26) result = (char)('A' + (n - 1) % 26) + result;
        return result;
    }
    internal static void Write(string filePath, DataGridView grid)
    {
        var columns = grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).OrderBy(c => c.DisplayIndex).ToList();
        var rows = grid.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow &&
            (grid.Name != "TopologyIpList" || r.Cells.Cast<DataGridViewCell>().Any(c => !string.IsNullOrWhiteSpace(Convert.ToString(c.Value))))).ToList();
        var all = new List<List<string>> { columns.Select(c => c.HeaderText).ToList() };
        all.AddRange(rows.Select(r => columns.Select(c => Convert.ToString(r.Cells[c.Index].Value) ?? "").ToList()));
        var widths = columns.Select((c, i) => c.HeaderText == "#" ? 7 : Math.Clamp(all.Max(r => r[i].Length) + 3, 11, 42)).ToList();
        var sheetRows = new List<XElement>();
        for (int y = 0; y < all.Count; y++)
        {
            var cells = new List<XElement>();
            for (int x = 0; x < columns.Count; x++)
            {
                string val = all[y][x];
                // Emit blank cells too so each exported table cell receives a visible border.
                string header = columns[x].HeaderText.ToLowerInvariant();
                bool isName = y > 0 && (header.Contains("subnet") && !header.Contains("mask") || header.Contains("lan") || header == "#");
                string style = y == 0 ? "1" : isName ? "2" : "0";
                var cell = new XElement(S + "c", new XAttribute("r", Col(x) + (y + 1)), new XAttribute("s", style));
                // All IP addresses and prefixes are strings, preserving dots, leading zeros and slash notation.
                // Numeric host counts may remain numeric for sorting / formulas.
                if (y > 0 && (header.Contains("hosts") || header == "#") &&
                    double.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double number))
                {
                    cell.Add(new XElement(S + "v", number.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                }
                else
                {
                    cell.Add(new XAttribute("t", "inlineStr"), new XElement(S + "is", new XElement(S + "t", val)));
                }
                cells.Add(cell);
            }
            sheetRows.Add(new XElement(S + "row", new XAttribute("r", y + 1), y == 0 ? new XAttribute("ht", 30) : new XAttribute("ht", 22), new XAttribute("customHeight", 1), cells));
        }
        var sheet = new XDocument(new XElement(S + "worksheet", new XAttribute(XNamespace.Xmlns + "r", R),
            new XElement(S + "sheetViews", new XElement(S + "sheetView", new XAttribute("workbookViewId", 0), new XElement(S + "pane", new XAttribute("ySplit", 1), new XAttribute("topLeftCell", "A2"), new XAttribute("activePane", "bottomLeft"), new XAttribute("state", "frozen")))),
            new XElement(S + "cols", widths.Select((w, i) => new XElement(S + "col", new XAttribute("min", i + 1), new XAttribute("max", i + 1), new XAttribute("width", w), new XAttribute("customWidth", 1)))),
            new XElement(S + "sheetData", sheetRows),
            new XElement(S + "autoFilter", new XAttribute("ref", $"A1:{Col(columns.Count - 1)}{Math.Max(1, all.Count)}"))));
        var styles = new XDocument(new XElement(S + "styleSheet",
            new XElement(S + "fonts", new XAttribute("count", 2),
                new XElement(S + "font", new XElement(S + "sz", new XAttribute("val", 11)), new XElement(S + "name", new XAttribute("val", "Aptos"))),
                new XElement(S + "font", new XElement(S + "b"), new XElement(S + "color", new XAttribute("rgb", "FFFFFFFF")), new XElement(S + "sz", new XAttribute("val", 11)), new XElement(S + "name", new XAttribute("val", "Aptos")))),
            new XElement(S + "fills", new XAttribute("count", 4),
                new XElement(S + "fill", new XElement(S + "patternFill", new XAttribute("patternType", "none"))),
                new XElement(S + "fill", new XElement(S + "patternFill", new XAttribute("patternType", "gray125"))),
                new XElement(S + "fill", new XElement(S + "patternFill", new XAttribute("patternType", "solid"), new XElement(S + "fgColor", new XAttribute("rgb", "FF293C60")), new XElement(S + "bgColor", new XAttribute("indexed", 64)))),
                new XElement(S + "fill", new XElement(S + "patternFill", new XAttribute("patternType", "solid"), new XElement(S + "fgColor", new XAttribute("rgb", "FFDCEAFE")), new XElement(S + "bgColor", new XAttribute("indexed", 64))))),
            new XElement(S + "borders", new XAttribute("count", 2), new XElement(S + "border", new XElement(S + "left"), new XElement(S + "right"), new XElement(S + "top"), new XElement(S + "bottom"), new XElement(S + "diagonal")), new XElement(S + "border", new XElement(S + "left", new XAttribute("style", "thin"), new XElement(S + "color", new XAttribute("rgb", "FFD0D7E2"))), new XElement(S + "right", new XAttribute("style", "thin"), new XElement(S + "color", new XAttribute("rgb", "FFD0D7E2"))), new XElement(S + "top", new XAttribute("style", "thin"), new XElement(S + "color", new XAttribute("rgb", "FFD0D7E2"))), new XElement(S + "bottom", new XAttribute("style", "thin"), new XElement(S + "color", new XAttribute("rgb", "FFD0D7E2"))), new XElement(S + "diagonal"))),
            new XElement(S + "cellStyleXfs", new XAttribute("count", 1), new XElement(S + "xf", new XAttribute("numFmtId", 0), new XAttribute("fontId", 0), new XAttribute("fillId", 0), new XAttribute("borderId", 0))),
            new XElement(S + "cellXfs", new XAttribute("count", 3),
                new XElement(S + "xf", new XAttribute("numFmtId", 0), new XAttribute("fontId", 0), new XAttribute("fillId", 0), new XAttribute("borderId", 1), new XAttribute("xfId", 0), new XAttribute("applyBorder", 1)),
                new XElement(S + "xf", new XAttribute("numFmtId", 0), new XAttribute("fontId", 1), new XAttribute("fillId", 2), new XAttribute("borderId", 1), new XAttribute("xfId", 0), new XAttribute("applyBorder", 1), new XAttribute("applyFill", 1)),
                new XElement(S + "xf", new XAttribute("numFmtId", 0), new XAttribute("fontId", 0), new XAttribute("fillId", 3), new XAttribute("borderId", 1), new XAttribute("xfId", 0), new XAttribute("applyBorder", 1), new XAttribute("applyFill", 1)))));
        using var zip = ZipFile.Open(filePath, ZipArchiveMode.Create);
        Entry(zip, "[Content_Types].xml", new XDocument(new XElement(XNamespace.Get("http://schemas.openxmlformats.org/package/2006/content-types") + "Types",
            new XElement(XNamespace.Get("http://schemas.openxmlformats.org/package/2006/content-types") + "Default", new XAttribute("Extension", "rels"), new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
            new XElement(XNamespace.Get("http://schemas.openxmlformats.org/package/2006/content-types") + "Default", new XAttribute("Extension", "xml"), new XAttribute("ContentType", "application/xml")),
            new XElement(XNamespace.Get("http://schemas.openxmlformats.org/package/2006/content-types") + "Override", new XAttribute("PartName", "/xl/workbook.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
            new XElement(XNamespace.Get("http://schemas.openxmlformats.org/package/2006/content-types") + "Override", new XAttribute("PartName", "/xl/worksheets/sheet1.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")),
            new XElement(XNamespace.Get("http://schemas.openxmlformats.org/package/2006/content-types") + "Override", new XAttribute("PartName", "/xl/styles.xml"), new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml")))));
        var relNs = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/relationships");
        Entry(zip, "_rels/.rels", new XDocument(new XElement(relNs + "Relationships", new XElement(relNs + "Relationship", new XAttribute("Id", "rId1"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"), new XAttribute("Target", "xl/workbook.xml")))));
        Entry(zip, "xl/workbook.xml", new XDocument(new XElement(S + "workbook", new XAttribute(XNamespace.Xmlns + "r", R), new XElement(S + "sheets", new XElement(S + "sheet", new XAttribute("name", "Network Plan"), new XAttribute("sheetId", 1), new XAttribute(R + "id", "rId1"))))));
        Entry(zip, "xl/_rels/workbook.xml.rels", new XDocument(new XElement(relNs + "Relationships",
            new XElement(relNs + "Relationship", new XAttribute("Id", "rId1"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"), new XAttribute("Target", "worksheets/sheet1.xml")),
            new XElement(relNs + "Relationship", new XAttribute("Id", "rId2"), new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"), new XAttribute("Target", "styles.xml")))));
        Entry(zip, "xl/worksheets/sheet1.xml", sheet);
        Entry(zip, "xl/styles.xml", styles);
    }
}

// Buffered list surfaces limit background erasing when expanding info cards.
internal sealed class BufferedInfoList : FlowLayoutPanel
{
    public BufferedInfoList()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        UpdateStyles();
    }
}
