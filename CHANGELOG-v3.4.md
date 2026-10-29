# Networking Toolkit v3.4: IP Inventory and sidebar stability

- Added **IP List** in Toolkit: editable topology inventory with Device, Location, Interface / Port, IP Address, Prefix / Mask, VLAN, Default Gateway, Role and Notes.
- Add/delete rows, tab-separated Copy table, native Excel export, duplicate-address and invalid-address highlights.
- CLI accordion categories now start closed; click one to expand subcommands.
- Collapsed app sidebar widened from 68 to 96 px to provide more space for navigation icons and DenDev button.
- Removed the frame-by-frame sidebar width animation that caused severe WinForms flicker. Collapse/expand now snaps between stable widths without rebuilding navigation on each frame.

## Test notes
- Build in Visual Studio on Windows; this package has not been compiled here.
- The IP List is session-only until you export it; importing/saving a project file is not yet implemented.
- Test duplicate IP highlighting and Excel output, along with clicking and collapsing CLI categories.
