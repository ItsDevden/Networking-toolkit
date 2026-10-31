<div align="center">

Networking Toolkit
An offline-first Windows toolbox for subnetting, IP planning, and Cisco networking.

Built with C# and Windows Forms • Designed for hands-on networking practice and troubleshooting

## 🌐 Networking Toolkit Web

Access the Networking Toolkit directly from your browser, on desktop or mobile. No installation required.

[![Launch Web Toolkit](https://img.shields.io/badge/Launch_Web_Toolkit-238636?style=for-the-badge&logo=githubpages&logoColor=white)](https://itsdevden.github.io/Networking-Toolkit-Web/)

If you insist on the exe
Windows · .NET 10 · No internet required for calculations
</div>

[![Download Latest Version](https://img.shields.io/badge/DOWNLOAD-LATEST_VERSION-22c55e?style=for-the-badge&logo=github&logoColor=white)](https://github.com/ItsDevden/Networking-toolkit/releases/latest/download/NetworkingToolkit.zip)

What's inside?
Tool	What it does
Subnetting	Interactive IPv4 decimal, binary and hexadecimal conversion, with a clickable 16-bit visualizer and bit controls.
IPv6 Subnetting	Explore all eight hexadecimal groups, view expanded/compressed notation, network prefix, and network vs. interface bits.
VLSM Planner	Plan variable-size IPv4 subnets according to host requirements and inspect network IDs, usable ranges, masks and broadcast addresses.
FLSM Planner	Split an IPv4 parent network into equal-size subnets and view the resulting addressing table.
IPv6 FLSM	Divide IPv6 networks into equal-prefix subnet ranges with a configurable number of displayed results.
IP List	Keep a topology/interface inventory with device, interface, address, mask/prefix, VLAN, gateway, role and notes. Flags duplicate or invalid addresses.
Cisco Command Library	Search Cisco IOS commands and generate configuration examples for routers, switches, interfaces, routes, VLANs, SSH, DHCP, NAT, ACLs and show commands.
Network references	Built-in networking reference material and step-by-step troubleshooting guidance.


Workflow features
- Dark-themed interface with expandable/resizable tables and organized navigation.
- Copyable results and CSV / Excel export where offered by each tool.
- FLSM and IPv6 FLSM: press Enter to add one result row, Backspace to remove the last (when the results table has focus).
- IP List: one automatic trailing empty row; type to edit a selected cell, or press Backspace/Delete to clear its contents.
- IPv6 Subnetting: arrow-key navigation between corresponding hexadecimal input fields.
- Offline Cisco configuration preview. Nothing is automatically sent to a router or switch.
Download and run
1. Open Latest Release.
2. Under Assets, download the provided Windows app or ZIP.
3. If it's a ZIP, extract it first. Run the application from the extracted folder.
4. If Windows blocks the application, check the publisher and source before deciding whether to run it. Managed school/work computers may restrict unsigned applications.
Whether you need to install the .NET runtime depends on how the release was published. A self-contained Windows build includes the required runtime; a framework-dependent build does not.

Build from source
Requirements:
- Windows
- Visual Studio 2022 (with support for the target SDK) or a compatible newer version
- .NET 10 SDK and the .NET desktop development workload
Open NetworkingToolkit.slnx in Visual Studio, then build and run the NetworkingToolkit project.

To create a self-contained, single-file Windows x64 build from a terminal:
dotnet publish NetworkingToolkit/NetworkingToolkit.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
Current version
v3.5.2 adds keyboard and usability consistency improvements across FLSM, IPv6 FLSM, IP List and IPv6 Subnetting. For detailed development history, see the versioned CHANGELOG-*.md files.
Notes
- This is a local Windows desktop application, not a web app (yet).
- IP planning and Cisco command generation are for review and learning. Always verify commands and addressing before applying them to real equipment.
- The IP List is not a database: export your entries before closing the app if you want to keep them.
<div align="center">

Made for people who would rather troubleshoot a network than fight a spreadsheet.
</div>
