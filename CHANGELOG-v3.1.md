# Networking Toolkit v3.1

## Changes
- Reworked **Router / Switch CLI** into a category-driven layout with a left-side menu:
  - Router
  - Switch
  - IP
  - SSH
  - DHCP
  - NAT
  - ACL
  - Show
- Improved CLI preview styling to feel more like a terminal and strengthened syntax coloring.
- Fixed sidebar layout structure so the first navigation button no longer sits behind the branding header.
- Adjusted IPv4 dashboard vertical spacing so the bottom action buttons sit higher in the default view.
- Improved combo-box rendering so themed dropdowns draw more consistently in the closed state.

## Notes
- This package is source-first and should be tested in Visual Studio on Windows.
- The CLI category redesign reuses the existing command generation logic and narrows the visible configuration list based on category.
