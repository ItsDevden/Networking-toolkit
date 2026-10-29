# v2.9 Unified palette test

- Replaces legacy grey theme values with slate palette across original WinForms controls and planner tables.
- Applies consistent palette to the dynamically configured Router / Switch CLI page.
- Keeps Info cards out of the old recursive theme pass and forces their button colors before display.
- Adjusts sidebar header spacing and first-item padding.
- Keeps WS_EX_COMPOSITED disabled to avoid white flashes; scrollable Info lists remain buffered.

Not compiled or visually verified in Windows. Extract to a separate folder and build in Visual Studio.
