# nexa .33 appearance

Appearance keeps its .32 physical ScrollViewer, fixed header/navigation/footer, bottom clearance and proportional input service. Only this page uses a hidden vertical scrollbar, so no empty scrollbar column is reserved. Other menus retain visible draggable scrollbars. Seasonal controls are placed in the same page owner rather than a competing nested ScrollViewer.

Exactly four labeled application swatches are selectable: **nexa, Violet, Aurora, Ember**. Their original accent/pixel pairs remain `#5B7CFF/#8B5CF6`, `#A78BFA/#7C3AED`, `#38BDF8/#14B8A6`, `#FB7185/#F59E0B`. No application accent HEX/custom picker is exposed. Existing per-Space/background color tools are separate. Unpublished .32 Amber/Green enum values remain readable for compatibility but map explicitly to Ember/Aurora; only the original four are offered.

During nexaween, the choices remain interactive and the exact secondary message **Nexaween overrides the theme color.** appears beneath them. Choosing a palette saves the Classic selection without recoloring active seasonal resources. Seasonal mode hides Classic Home/Space wallpaper layers while retaining their saved choices; returning to Classic immediately restores them. Existing seasonal artwork and Home-only flicker remain intact. Background preset provenance remains recorded in the .32 audit.

WPF tests cover normal, small, maximized and fullscreen hosts, both seasonal states, bottom/footer reachability, dynamic extents, keyboard/scrollbar input and simulated layout scales. Physical precision-trackpad feel and a full matrix of actual monitor DPI/driver configurations remain manual acceptance; synthetic deltas and LayoutTransform are not physical certification.
