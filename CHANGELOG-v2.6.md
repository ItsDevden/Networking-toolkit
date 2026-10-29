# Networking Toolkit v2.6 - Info pages & sidebar

- Network Reference and Troubleshooting now share the Cisco Command Library's compact expandable-card design, with search and copy actions.
- Sidebar expands and collapses over ~180ms with an ease-out curve instead of changing width instantly. Navigation controls are recreated only at the start/end, not on every animation tick.
- Existing Cisco Command Library, CLI generator, calculators, FLSM/VLSM planners, and exports retained from v2.5.

Notes:
- The Router/Switch CLI generator retains its split input/preview form to avoid changing configuration-generation behavior. It does not yet use accordion sections.
- Not compiled on Windows in this environment. Open NetworkingToolkit.slnx and build in Visual Studio.
