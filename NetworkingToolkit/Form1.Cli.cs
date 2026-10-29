using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace NetworkingToolkit
{
    public partial class Form1
    {
        private TabPage cliPage = null!;
        private ComboBox cliDevice = null!;
        private ComboBox cliTask = null!;
        private FlowLayoutPanel cliFields = null!;
        private RichTextBox cliPreview = null!;
        private Label cliMessage = null!;
        private readonly Dictionary<string, TextBox> cliInputs = new();
        private Size compactClientSize;
        private bool cliSetupComplete;
        private FlowLayoutPanel cliCategoryPanel = null!;
        private string cliCategory = "";
        private bool cliSyncingSelection;
        private readonly Dictionary<string, Button> cliCategoryButtons = new();

        // Each configuration template appears in one menu group (except device-specific
        // basic setup, which intentionally supports both router and switch identities).
        private static readonly Dictionary<string, (string? device, string[] tasks)> CliCategoryMap = new()
        {
            ["Router"] = ("Router", new[] { "Basic setup" }),
            ["Switch"] = ("Switch", new[] { "Basic setup", "Management SVI" }),
            ["Interfaces"] = (null, new[] { "Interface IP" }),
            ["Routing"] = ("Router", new[] { "Static route", "Default route" }),
            ["VLANs"] = ("Switch", new[] { "Create VLAN", "Access VLAN port" }),
            ["SSH"] = (null, new[] { "SSH access" }),
            ["DHCP"] = ("Router", new[] { "DHCP pool" }),
            ["NAT"] = ("Router", new[] { "NAT overload" }),
            ["ACL"] = ("Router", new[] { "Standard ACL", "Extended ACL" }),
            ["Show"] = (null, new[] { "Show commands" })
        };

        private static readonly string[] RouterTasks =
        {
            "Basic setup", "Interface IP", "Static route", "Default route", "DHCP pool",
            "NAT overload", "SSH access", "Standard ACL", "Extended ACL", "Show commands"
        };
        private static readonly string[] SwitchTasks =
        {
            "Basic setup", "Management SVI", "Access VLAN port", "Create VLAN",
            "SSH access", "Show commands"
        };

        private void SetupCliGenerator()
        {
            compactClientSize = ClientSize;
            cliPage = new TabPage("Router / Switch CLI") { BackColor = Color.FromArgb(27, 33, 44) };
            tabControl1.TabPages.Add(cliPage);
            var heading = new Label
            {
                Text = "Offline Cisco IOS command generator  |  Preview only - nothing is sent to a device",
                Dock = DockStyle.Top, Height = 48, Padding = new Padding(18, 12, 0, 0),
                ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1,
                BackColor = Color.FromArgb(27, 33, 44), SplitterWidth = 6
            };
            cliPage.Controls.Add(split);
            cliPage.Controls.Add(heading);
            split.SizeChanged += (_, _) =>
            {
                if (split.Width < 560) return;
                int desired = Math.Min(475, Math.Max(300, (int)(split.Width * .47)));
                int max = split.Width - 255;
                if (max >= 300) split.SplitterDistance = Math.Clamp(desired, 300, max);
            };

            // Left side: accordion navigation above a scrollable editor for the chosen task.
            var left = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 7, 8, 10),
                BackColor = Color.FromArgb(27, 33, 44) };
            split.Panel1.Controls.Add(left);
            var categoryTitle = new Label { Text = "CLI CATEGORIES", Dock = DockStyle.Top,
                Height = 31, ForeColor = Color.FromArgb(166, 189, 215),
                Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            var editorHost = new Panel { Dock = DockStyle.Bottom, Height = 236,
                BackColor = Color.FromArgb(33, 42, 56), Padding = new Padding(12, 8, 8, 8) };
            var editorTitle = new Label { Text = "BASIC SETUP  •  SETTINGS", Dock = DockStyle.Top,
                Height = 25, ForeColor = Color.FromArgb(187, 212, 237),
                Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            cliFields = new FlowLayoutPanel { Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true,
                Padding = new Padding(0, 4, 10, 6), BackColor = editorHost.BackColor };
            editorHost.Controls.Add(cliFields);
            editorHost.Controls.Add(editorTitle);
            cliCategoryPanel = new FlowLayoutPanel { Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true,
                Padding = new Padding(0, 3, 10, 0), BackColor = left.BackColor };
            left.Controls.Add(cliCategoryPanel);
            left.Controls.Add(editorHost);
            left.Controls.Add(categoryTitle);
            left.Resize += (_, _) => { ResizeCliFields(); ResizeCliCategoryCards(); };
            cliFields.Resize += (_, _) => ResizeCliFields();
            cliTask = new ComboBox { Visible = false };
            cliDevice = new ComboBox { Visible = false };
            cliDevice.Items.AddRange(new object[] { "Router", "Switch" });
            // Hidden state controls keep the existing generator's command and validation logic.
            BuildCliCategoryButtons();

            var right = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 11, 12),
                BackColor = Color.FromArgb(27, 33, 44) };
            split.Panel2.Controls.Add(right);
            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                BackColor = right.BackColor };
            top.Controls.Add(CliButton("Copy commands", () =>
            {
                if (!string.IsNullOrWhiteSpace(cliPreview.Text)) Clipboard.SetText(cliPreview.Text);
            }));
            top.Controls.Add(CliButton("Save .txt", () =>
            {
                using var dialog = new SaveFileDialog { Filter = "Text file (*.txt)|*.txt", FileName = "cisco-config.txt" };
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    try { System.IO.File.WriteAllText(dialog.FileName, cliPreview.Text); }
                    catch (Exception ex) { MessageBox.Show(this, ex.Message, "Save failed"); }
                }
            }));
            cliMessage = new Label
            {
                Text = "Edit values on the left. Review before pasting onto a device.",
                Dock = DockStyle.Bottom, Height = 60, ForeColor = Color.FromArgb(184, 207, 227),
                Padding = new Padding(3, 9, 2, 2)
            };
            cliPreview = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true,
                WordWrap = false, BackColor = Color.FromArgb(10, 17, 29),
                ForeColor = Color.FromArgb(218, 233, 245),
                Font = new Font("Consolas", 11), BorderStyle = BorderStyle.FixedSingle };
            cliPreview.TextChanged += (_, _) => HighlightCiscoPreview();
            right.Controls.Add(cliPreview);
            right.Controls.Add(cliMessage);
            right.Controls.Add(top);
            cliTask.SelectedIndexChanged += (_, _) =>
            {
                editorTitle.Text = (cliTask.SelectedItem?.ToString() ?? "CONFIGURATION").ToUpperInvariant() + "  •  SETTINGS";
                BuildCliInputs();
                HighlightCliCategorySelection();
            };
            cliSetupComplete = true;
            cliDevice.SelectedIndex = 0;
            cliTask.Items.AddRange(CliCategoryMap["Router"].tasks.Cast<object>().ToArray());
            cliTask.SelectedItem = "Basic setup";
            // Keep all category accordions closed on first launch.
            cliCategory = "";
            foreach (var card in cliCategoryCards.Values) card.Height = 47;
            foreach (var buttons in cliTaskButtons.Values)
                foreach (var button in buttons) button.Visible = false;
        }

        private Label CliLabel(string title) => new()
        {
            Text = title, AutoSize = false, Width = 350, Height = 24,
            ForeColor = ink, Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };

        private Button CliButton(string title, Action click)
        {
            var btn = new Button { Text = title, Width = 142, Height = 34, Margin = new Padding(0,0,9,0),
                BackColor = Color.FromArgb(65,78,86), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btn.Click += (s,e) => click();
            return btn;
        }

        private void ResizeCliFields()
        {
            if (cliFields == null) return;
            foreach (Control c in cliFields.Controls)
                c.Width = Math.Max(140, cliFields.ClientSize.Width - 25);
        }

        private void BuildCliInputs()
        {
            if (!cliSetupComplete) return;
            cliFields.SuspendLayout();
            cliFields.Controls.Clear();
            cliInputs.Clear();
            var task = cliTask.SelectedItem?.ToString() ?? "";
            var device = cliDevice.SelectedItem?.ToString() ?? "Router";
            void Add(string key, string label, string example)
            {
                var panel = new Panel { Height = 71, Margin = new Padding(0,0,0,8) };
                var title = CliLabel(label); title.Dock = DockStyle.Top;
                var box = new TextBox { Text = example, Dock = DockStyle.Bottom, Height = 30,
                    BackColor = Color.FromArgb(48, 62, 80), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
                cliInputs[key] = box;
                box.TextChanged += (s,e) => UpdateCliPreview();
                panel.Controls.Add(box); panel.Controls.Add(title);
                cliFields.Controls.Add(panel);
            }
            switch (task)
            {
                case "Basic setup":
                    Add("host", "Hostname", device == "Router" ? "R1" : "S1");
                    Add("banner", "Login banner", "Authorized access only");
                    Add("user", "Admin username", "admin");
                    break;
                case "Interface IP":
                    Add("interface", "Interface", "GigabitEthernet0/0");
                    Add("ip", "Interface IPv4", "192.168.1.1");
                    Add("mask", "Subnet mask", "255.255.255.0");
                    Add("description", "Description", "LAN gateway");
                    break;
                case "Static route":
                    Add("network", "Destination network ID", "192.168.2.0");
                    Add("mask", "Destination subnet mask", "255.255.255.0");
                    Add("next", "Next-hop IPv4", "10.0.0.2");
                    break;
                case "Default route": Add("next", "Next-hop IPv4", "10.0.0.1"); break;
                case "DHCP pool":
                    Add("pool", "DHCP pool name", "LAN");
                    Add("network", "Network ID", "192.168.1.0");
                    Add("mask", "Subnet mask", "255.255.255.0");
                    Add("gateway", "Default gateway", "192.168.1.1");
                    Add("dns", "DNS server", "8.8.8.8");
                    Add("excludedstart", "Exclude from", "192.168.1.1");
                    Add("excludedend", "Exclude through", "192.168.1.10");
                    break;
                case "NAT overload":
                    Add("network", "Inside network ID", "192.168.1.0");
                    Add("wildcard", "Inside wildcard mask", "0.0.0.255");
                    Add("inside", "Inside interface", "GigabitEthernet0/0");
                    Add("outside", "Outside interface", "GigabitEthernet0/1");
                    break;
                case "SSH access":
                    Add("host", "Hostname", device == "Router" ? "R1" : "S1");
                    Add("domain", "Domain name", "lab.local");
                    Add("user", "Admin username", "admin");
                    break;
                case "Management SVI":
                    Add("vlan", "Management VLAN number", "1");
                    Add("ip", "Management IPv4", "192.168.1.2");
                    Add("mask", "Subnet mask", "255.255.255.0");
                    Add("gateway", "Switch default gateway", "192.168.1.1");
                    break;
                case "Create VLAN":
                    Add("vlan", "VLAN number", "10"); Add("name", "VLAN name", "USERS"); break;
                case "Access VLAN port":
                    Add("interface", "Switchport interface", "FastEthernet0/1");
                    Add("vlan", "Access VLAN number", "10"); break;
                case "Standard ACL":
                    Add("acl", "ACL number (1-99)", "10");
                    Add("network", "Permitted network", "192.168.1.0");
                    Add("wildcard", "Wildcard mask", "0.0.0.255"); break;
                case "Extended ACL":
                    Add("acl", "ACL number (100-199)", "110");
                    Add("network", "Source network", "192.168.1.0");
                    Add("wildcard", "Source wildcard", "0.0.0.255");
                    Add("destination", "Destination host IPv4", "192.168.2.10");
                    Add("port", "Destination TCP port", "443"); break;
            }
            cliFields.ResumeLayout();
            ResizeCliFields();
            UpdateCliPreview();
        }

        private static readonly string[] CliCategories =
            { "Router", "Switch", "Interfaces", "Routing", "VLANs", "SSH", "DHCP", "NAT", "ACL", "Show" };
        private readonly Dictionary<string, Panel> cliCategoryCards = new();
        private readonly Dictionary<string, List<Button>> cliTaskButtons = new();

        private static string CliIcon(string category) => category switch
        {
            "Router" => "◉", "Switch" => "▣", "Interfaces" => "◎", "Routing" => "⇢", "VLANs" => "▦", "SSH" => "♙",
            "DHCP" => "▤", "NAT" => "⇄", "ACL" => "⬡", "Show" => "⌕", _ => "▸"
        };

        private void BuildCliCategoryButtons()
        {
            cliCategoryPanel.SuspendLayout();
            cliCategoryPanel.Controls.Clear();
            cliCategoryButtons.Clear();
            cliCategoryCards.Clear();
            cliTaskButtons.Clear();
            foreach (string category in CliCategories)
            {
                var card = new Panel { Height = 47, Width = 400,
                    Margin = new Padding(0, 0, 0, 5),
                    BackColor = Color.FromArgb(35, 45, 61) };
                var header = new Button
                {
                    Text = $"  {CliIcon(category)}      {category}                         ▾",
                    Bounds = new Rectangle(0, 0, 400, 45),
                    Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                    FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft,
                    BackColor = Color.FromArgb(35, 45, 61), ForeColor = Color.White,
                    Cursor = Cursors.Hand, Font = new Font("Segoe UI", 10, FontStyle.Bold)
                };
                header.FlatAppearance.BorderColor = Color.FromArgb(89, 113, 145);
                header.FlatAppearance.MouseOverBackColor = Color.FromArgb(53, 72, 98);
                header.Click += (_, _) => ApplyCliCategory(category);
                card.Controls.Add(header);
                cliCategoryButtons[category] = header;
                cliCategoryCards[category] = card;
                var children = new List<Button>();
                foreach (string task in CliCategoryMap[category].tasks)
                {
                    string capturedTask = task;
                    var child = new Button { Text = "    ▸  " + task,
                        Height = 31, Width = 390, Left = 5, Top = 47 + children.Count * 33,
                        FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft,
                        ForeColor = Color.FromArgb(215, 227, 242),
                        BackColor = Color.FromArgb(29, 40, 57), Cursor = Cursors.Hand,
                        Font = new Font("Segoe UI", 9) };
                    child.FlatAppearance.BorderSize = 0;
                    child.FlatAppearance.MouseOverBackColor = Color.FromArgb(52, 73, 101);
                    child.Click += (_, _) =>
                    {
                        if (cliCategory != category) ApplyCliCategory(category);
                        cliTask.SelectedItem = capturedTask;
                        HighlightCliCategorySelection();
                    };
                    child.Visible = false;
                    card.Controls.Add(child);
                    children.Add(child);
                }
                cliTaskButtons[category] = children;
                cliCategoryPanel.Controls.Add(card);
            }
            cliCategoryPanel.ResumeLayout();
            ResizeCliCategoryCards();
        }

        private void ResizeCliCategoryCards()
        {
            if (cliCategoryPanel == null) return;
            int width = Math.Max(140, cliCategoryPanel.ClientSize.Width - cliCategoryPanel.Padding.Horizontal - 4);
            foreach (var card in cliCategoryCards.Values)
            {
                card.Width = width;
                foreach (Control control in card.Controls)
                    control.Width = control == cliCategoryButtons[cliCategoryCards.First(x => x.Value == card).Key] ? width : width - 10;
            }
        }

        private void ApplyCliCategory(string category)
        {
            if (!CliCategoryMap.TryGetValue(category, out var map)) return;
            bool wasOpen = cliCategory == category && cliCategoryCards.TryGetValue(category, out var oldCard)
                           && oldCard.Height > 47;
            // Clicking an open group closes it, but keeps its last selected task active.
            cliCategory = wasOpen ? "" : category;
            cliSyncingSelection = true;
            try
            {
                if (!wasOpen)
                {
                    if (map.device != null) cliDevice.SelectedItem = map.device;
                    string previous = cliTask.SelectedItem?.ToString() ?? "";
                    cliTask.Items.Clear();
                    cliTask.Items.AddRange(map.tasks.Cast<object>().ToArray());
                    cliTask.SelectedItem = cliTask.Items.Contains(previous) ? previous : map.tasks[0];
                }
            }
            finally { cliSyncingSelection = false; }
            cliCategoryPanel.SuspendLayout();
            foreach (string key in CliCategories)
            {
                bool open = key == cliCategory;
                var card = cliCategoryCards[key];
                var children = cliTaskButtons[key];
                card.Height = open ? 49 + children.Count * 33 : 47;
                foreach (var child in children) child.Visible = open;
                var header = cliCategoryButtons[key];
                header.Text = $"  {CliIcon(key)}      {key}                         {(open ? "▴" : "▾")}";
                header.BackColor = open ? Color.FromArgb(48, 70, 96) : Color.FromArgb(35, 45, 61);
            }
            cliCategoryPanel.ResumeLayout(true);
            HighlightCliCategorySelection();
            if (!wasOpen) BuildCliInputs();
        }

        private void HighlightCliCategorySelection()
        {
            string selected = cliTask?.SelectedItem?.ToString() ?? "";
            foreach (var pair in cliTaskButtons)
                foreach (var item in pair.Value)
                {
                    bool active = pair.Key == cliCategory && item.Text.EndsWith("  " + selected, StringComparison.Ordinal);
                    item.BackColor = active ? Color.FromArgb(52, 85, 120) : Color.FromArgb(29, 40, 57);
                    item.ForeColor = active ? Color.White : Color.FromArgb(215, 227, 242);
                }
        }

        // Native DropDownList controls draw the closed selection using Windows colors unless owner drawn.
        private static void StyleCliCombo(ComboBox combo)
        {
            combo.DrawMode = DrawMode.OwnerDrawFixed;
            combo.ItemHeight = 23;
            combo.BackColor = Color.FromArgb(48, 62, 80);
            combo.ForeColor = Color.FromArgb(221, 234, 247);
            combo.FlatStyle = FlatStyle.Flat;
            combo.DrawItem += (_, e) =>
            {
                Rectangle bounds = e.Bounds;
                if (bounds.Width <= 0 || bounds.Height <= 0) return;
                bool selected = (e.State & DrawItemState.Selected) != 0;
                using var bg = new SolidBrush(selected ? Color.FromArgb(61, 85, 110) : Color.FromArgb(48, 62, 80));
                e.Graphics.FillRectangle(bg, bounds);
                string text = e.Index >= 0 ? combo.Items[e.Index]?.ToString() ?? string.Empty : combo.Text;
                TextRenderer.DrawText(e.Graphics, text, combo.Font,
                    new Rectangle(bounds.Left + 4, bounds.Top, Math.Max(0, bounds.Width - 8), bounds.Height),
                    Color.FromArgb(235, 244, 251),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                if (e.Index >= 0) e.DrawFocusRectangle();
            };
        }

        private bool cliHighlighting;
        private void HighlightCiscoPreview()
        {
            if (cliHighlighting || cliPreview == null) return;
            cliHighlighting = true;
            int caret = cliPreview.SelectionStart;
            int length = cliPreview.SelectionLength;
            try
            {
                cliPreview.SuspendLayout();
                cliPreview.SelectAll();
                cliPreview.SelectionColor = Color.FromArgb(225, 235, 245);
                int offset = 0;
                foreach (string line in cliPreview.Lines)
                {
                    string trimmed = line.TrimStart();
                    Color color = trimmed.StartsWith("!") ? Color.FromArgb(255, 190, 110) :
                        trimmed.StartsWith("show ") ? Color.FromArgb(108, 202, 255) :
                        trimmed.StartsWith("ip ") || trimmed.StartsWith("interface ") || trimmed.StartsWith("switchport ") || trimmed.StartsWith("access-list ") ? Color.FromArgb(92, 214, 255) :
                        trimmed.StartsWith("hostname ") || trimmed.StartsWith("username ") || trimmed.StartsWith("banner ") || trimmed.StartsWith("crypto ") ? Color.FromArgb(225, 151, 255) :
                        trimmed is "enable" or "configure terminal" or "end" or "exit" or "no shutdown" ? Color.FromArgb(123, 232, 149) :
                        Color.FromArgb(222, 235, 246);
                    cliPreview.Select(offset, line.Length);
                    cliPreview.SelectionColor = color;
                    offset += line.Length + 1;
                }
            }
            finally
            {
                cliPreview.Select(Math.Min(caret, cliPreview.TextLength), Math.Min(length, Math.Max(0, cliPreview.TextLength-caret)));
                cliPreview.ResumeLayout();
                cliHighlighting = false;
            }
        }

        private void UpdateCliPreview()
        {
            if (cliPreview == null || cliTask?.SelectedItem == null) return;
            string task = cliTask.SelectedItem.ToString()!;
            string V(string key) => cliInputs.TryGetValue(key, out var box) ? box.Text.Trim() : "";
            bool Ipv4(string value) => IPAddress.TryParse(value, out var address) && address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
            bool IosToken(string value) => value.Length > 0 && value.All(c => char.IsAsciiLetterOrDigit(c) || c == '_' || c == '-' || c == '.');
            bool Interface(string value) => value.Length > 0 && value.All(c => char.IsAsciiLetterOrDigit(c) || c == '/' || c == '.' || c == '-');
            bool Number(string value, int min, int max) => int.TryParse(value, out int n) && n >= min && n <= max;
            bool Mask(string value) => Ipv4(value) && IsValidMask(value);
            string error = "";
            var lines = new List<string> { "enable", "configure terminal" };
            switch (task)
            {
                case "Basic setup":
                    if (!IosToken(V("host")) || !IosToken(V("user")) || V("banner").Contains('^') || V("banner").Contains('\n')) error = "Use a simple hostname/username and a one-line banner without ^.";
                    lines.AddRange(new[] { $"hostname {V("host")}", $"banner motd ^{V("banner")}^", "service password-encryption", $"username {V("user")} privilege 15 secret <SET-STRONG-PASSWORD>", "enable secret <SET-ENABLE-SECRET>" });
                    break;
                case "Interface IP":
                    if (!Interface(V("interface")) || !Ipv4(V("ip")) || !Mask(V("mask")) || V("description").Contains('\n')) error = "Check the interface, IPv4 and contiguous subnet mask.";
                    lines.AddRange(new[] { $"interface {V("interface")}", $"description {V("description")}", $"ip address {V("ip")} {V("mask")}", "no shutdown", "exit" }); break;
                case "Static route":
                    if (!Ipv4(V("network")) || !Mask(V("mask")) || !Ipv4(V("next"))) error = "Enter valid destination, subnet mask and next-hop IPv4 addresses.";
                    else if (!IsNetworkId(V("network"), V("mask"))) error = "Destination must be the network ID, not a host address.";
                    lines.Add($"ip route {V("network")} {V("mask")} {V("next")}"); break;
                case "Default route":
                    if (!Ipv4(V("next"))) error = "Enter a valid IPv4 next hop.";
                    lines.Add($"ip route 0.0.0.0 0.0.0.0 {V("next")}"); break;
                case "DHCP pool":
                    if (!IosToken(V("pool")) || !Ipv4(V("network")) || !Mask(V("mask")) || !Ipv4(V("gateway")) || !Ipv4(V("dns")) || !Ipv4(V("excludedstart")) || !Ipv4(V("excludedend"))) error = "Check pool name and all IPv4 fields.";
                    else if (!IsNetworkId(V("network"), V("mask"))) error = "Use a network ID for the DHCP network.";
                    lines.AddRange(new[] { $"ip dhcp excluded-address {V("excludedstart")} {V("excludedend")}", $"ip dhcp pool {V("pool")}", $"network {V("network")} {V("mask")}", $"default-router {V("gateway")}", $"dns-server {V("dns")}", "exit" }); break;
                case "NAT overload":
                    if (!Ipv4(V("network")) || !Ipv4(V("wildcard")) || !Interface(V("inside")) || !Interface(V("outside")) || V("inside") == V("outside")) error = "Check inside network, wildcard and distinct interface names.";
                    lines.AddRange(new[] { $"access-list 1 permit {V("network")} {V("wildcard")}", $"interface {V("inside")}", "ip nat inside", "exit", $"interface {V("outside")}", "ip nat outside", "exit", $"ip nat inside source list 1 interface {V("outside")} overload" }); break;
                case "SSH access":
                    if (!IosToken(V("host")) || !IosToken(V("domain")) || !IosToken(V("user"))) error = "Hostname, domain and username must be valid IOS tokens.";
                    lines.AddRange(new[] { $"hostname {V("host")}", $"ip domain-name {V("domain")}", $"username {V("user")} privilege 15 secret <SET-STRONG-PASSWORD>", "crypto key generate rsa modulus 2048", "ip ssh version 2", "line vty 0 4", "login local", "transport input ssh", "exit" }); break;
                case "Management SVI":
                    if (!Number(V("vlan"), 1, 4094) || !Ipv4(V("ip")) || !Mask(V("mask")) || !Ipv4(V("gateway"))) error = "Check management VLAN (1-4094), IPv4, mask, and gateway.";
                    lines.AddRange(new[] { $"interface vlan {V("vlan")}", $"ip address {V("ip")} {V("mask")}", "no shutdown", "exit", $"ip default-gateway {V("gateway")}" }); break;
                case "Create VLAN":
                    if (!Number(V("vlan"), 1, 4094) || !IosToken(V("name"))) error = "VLAN must be 1-4094; use a simple VLAN name.";
                    lines.AddRange(new[] { $"vlan {V("vlan")}", $"name {V("name")}", "exit" }); break;
                case "Access VLAN port":
                    if (!Interface(V("interface")) || !Number(V("vlan"), 1, 4094)) error = "Check interface and VLAN (1-4094).";
                    lines.AddRange(new[] { $"interface {V("interface")}", "switchport mode access", $"switchport access vlan {V("vlan")}", "no shutdown", "exit" }); break;
                case "Standard ACL":
                    if (!Number(V("acl"), 1, 99) || !Ipv4(V("network")) || !Ipv4(V("wildcard"))) error = "Enter ACL 1-99, a source network and wildcard mask.";
                    lines.Add($"access-list {V("acl")} permit {V("network")} {V("wildcard")}"); break;
                case "Extended ACL":
                    if (!Number(V("acl"), 100, 199) || !Ipv4(V("network")) || !Ipv4(V("wildcard")) || !Ipv4(V("destination")) || !Number(V("port"), 1, 65535)) error = "Check ACL 100-199, source, wildcard, destination and TCP port.";
                    lines.Add($"access-list {V("acl")} permit tcp {V("network")} {V("wildcard")} host {V("destination")} eq {V("port")}"); break;
                case "Show commands":
                    cliPreview.Text = "show ip interface brief\nshow running-config\nshow startup-config\nshow version\nshow ip route\nshow interfaces status\nshow vlan brief\nshow access-lists\nshow ip nat translations\nshow ip dhcp binding";
                    cliMessage.Text = "EXEC-mode inspection commands. Availability varies by IOS platform."; return;
            }
            lines.AddRange(new[] { "end", "copy running-config startup-config" });
            if (error.Length > 0)
            {
                cliPreview.Text = "! Fix inputs before copying commands.\n! " + error;
                cliMessage.Text = error;
            }
            else
            {
                cliPreview.Text = string.Join(Environment.NewLine, lines);
                cliMessage.Text = "Generic Cisco IOS template. Check interface names, topology and platform before applying. Replace password placeholders. Some commands may require device-specific adjustments.";
            }
        }

        private static bool IsValidMask(string input)
        {
            var bytes = IPAddress.Parse(input).GetAddressBytes();
            uint mask = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
            uint complement = ~mask;
            return (complement & (complement + 1)) == 0;
        }
        private static bool IsNetworkId(string ip, string mask)
        {
            byte[] a = IPAddress.Parse(ip).GetAddressBytes();
            byte[] b = IPAddress.Parse(mask).GetAddressBytes();
            for (int i = 0; i < 4; i++) if ((a[i] & b[i]) != a[i]) return false;
            return true;
        }

        private void CliTabResizing()
        {
            if (!cliSetupComplete || WindowState == FormWindowState.Maximized || WindowState == FormWindowState.Minimized) return;
            bool expanded = tabControl1.SelectedTab == cliPage;
            int requestedHeight = expanded ? 860 : compactClientSize.Height;
            var area = Screen.FromControl(this).WorkingArea;
            var desired = new Size(Math.Min(compactClientSize.Width, area.Width), Math.Min(requestedHeight, area.Height - (Height - ClientSize.Height)));
            if (desired.Height < 450) desired.Height = Math.Min(450, area.Height);
            int centerX = Left + Width / 2;
            int centerY = Top + Height / 2;
            ClientSize = desired;
            Left = Math.Clamp(centerX - Width / 2, area.Left, Math.Max(area.Left, area.Right - Width));
            Top = Math.Clamp(centerY - Height / 2, area.Top, Math.Max(area.Top, area.Bottom - Height));
        }
    }
}
