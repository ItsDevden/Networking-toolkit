# Networking Toolkit v3.3: CLI Accordions

- CLI left pane now uses expanding Router, Switch, IP, SSH, DHCP, NAT, ACL and Show category cards.
- Selecting a category reveals its specific commands inside that category; selecting a command updates the editable settings and terminal preview.
- Added compact symbolic category icons and selected-command highlighting.
- Retained the existing validation and Cisco configuration generation logic.
- Kept syntax-colored terminal preview and Copy / Save actions.
- Changed global sidebar to a fixed-header/two-row layout to prevent the first Subnetting navigation button being obscured.
- Preserved the IPv6 dashboard, subnet planners and Excel exports from v3.2.

## Known test limitations
- Windows WinForms cannot be compiled in the current environment. Build and visually test in Visual Studio.
- Category icon glyphs depend on Segoe UI Symbol availability. They are not pixel-art PNGs.
