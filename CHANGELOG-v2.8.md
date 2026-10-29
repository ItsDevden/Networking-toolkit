# v2.8 Info rendering hotfix

- Prevents the legacy grey theme pass from overriding the custom colors of Info pages during startup.
- Removes whole-window WS_EX_COMPOSITED, which can produce large white redraw flashes when WinForms child windows resize/repaint.
- Adds double buffering to the scrollable Info list panels.
- Existing pages, calculators and CLI logic are retained.

This is an uncompiled Windows source update. Test the initial card colors and repeated expand/collapse operations in Visual Studio. If flicker persists, the card rebuilding logic should be refactored to retain controls rather than clearing and recreating them per click.
