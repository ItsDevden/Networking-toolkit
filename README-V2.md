# Networking Toolkit v2 (source preview)

C# WinForms targeting **.NET 10, Windows**. This is an updated version of the original project, preserving original converter, VLSM/FLSM, and CLI generator code.

## Run

1. Open `NetworkingToolkit.slnx` in Visual Studio with the **.NET desktop development** workload and .NET 10 SDK installed.
2. Build and run (F5). The default startup project is NetworkingToolkit.
3. For a portable Windows build, run:

   `dotnet publish NetworkingToolkit/NetworkingToolkit.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableWindowsTargeting=true`

## Changes

- Sidebar with **TOOLKIT** and **INFO & REFERENCE** sections, collapsible mode, selected-page highlighting, and arrows to reorder pages within their section.
- Original converters, VLSM planner, FLSM planner, and Cisco router/switch CLI generator retained.
- IPv6 FLSM planner using BigInteger calculations.
- Static route planner generating Cisco IOS commands with input checks.
- Searchable Cisco command library, network reference, and layered troubleshooting checklist, with copy shortcuts.
- Excel `.xlsx` export on VLSM, FLSM, IPv6 FLSM, and static-route planners. Workbook includes auto-sized columns, frozen first row, filter controls, contrasting headers, selective network labels, and otherwise uncolored data.
- More prominent interactive input/control styling.

## Notes

This environment does not have the Windows .NET SDK / WinForms runtime, so the project has been edited and inspected, **not built or Windows-tested** here. Keep your original ZIP as a rollback copy. The `.xlsx` files are created using .NET's built-in ZIP/XML APIs and do not require Excel or third-party packages to be installed.
