---
version: alpha
name: Competency Management Service
description: Design system for the corporate competency-matrix and promotion service (web SPA, React + antd 6, offline corporate network). Palette derived from the corporate REC/EXIAR presentation template.
provenance: original
colors:
  # --- action / brand ---
  primary: "#2B42E5"           # primary buttons, links, active menu item, focus ring, selected controls (brand blue)
  primary-hover: "#2338C2"     # hover of primary button/link
  primary-active: "#182CC2"    # pressed state of primary button/link
  primary-subtle: "#EBF2FD"    # selected table row, hovered menu item, selected-card background, light highlight
  accent: "#FBAE40"            # brand highlight FILLS only (progress, badge fill) and the keyboard focus ring on the dark header; never text on light surfaces
  # --- text ---
  text-primary: "#292929"      # body text, headings, table cell content
  text-secondary: "#4B4B4C"    # supporting text, form labels, table header text
  text-muted: "#5F6670"        # hints, captions, placeholders, metadata (AA on every light surface)
  text-disabled: "#A3A9B1"     # disabled control text only; always paired with a visible reason (WCAG exempt)
  text-inverse: "#FFFFFF"      # text/icons on primary, danger, success, info, warning fills and on the dark header
  text-inverse-muted: "#9BA8F9" # secondary text and icons on the dark navy surface (header menu items, captions)
  # --- surfaces / borders ---
  surface-page: "#F5F6F8"      # application background behind panels and tables
  surface-panel: "#FFFFFF"     # cards, tables, forms, modals, popovers, dropdowns
  surface-muted: "#EEEEEE"     # table header, disabled input fill, skeletons, neutral chips
  surface-inverse: "#081240"   # application header with its navigation menu, tooltips, dark banners (brand navy)
  border-subtle: "#DDE1E6"     # dividers, card and table hairlines (decorative, not required to identify controls)
  border-control: "#7D8590"    # input, select, checkbox, radio, default-button outlines (>= 3:1 non-text contrast)
  overlay-scrim: "#08124073"   # modal and drawer backdrop (brand navy at 45% opacity)
  overlay-inverse-hover: "#FFFFFF1F"    # hover fill of a button on surface-inverse, e.g. the header user button (text-inverse at 12% opacity)
  overlay-inverse-pressed: "#FFFFFF33"  # pressed fill of a button on surface-inverse (text-inverse at 20% opacity)
  # --- semantic status ---
  success: "#197A4A"           # positive status: requirement met, saved, approved
  success-bg: "#E6F4EC"        # background of success alert, chip, row highlight
  warning: "#A85800"           # attention status: below target level, deadline near, incomplete data
  warning-bg: "#FFF4DF"        # background of warning alert, chip, row highlight
  danger: "#C62832"            # destructive actions, errors, critical competency gap, blocking validation
  danger-hover: "#B3202A"      # hover/pressed of destructive button
  danger-bg: "#FDECEC"         # background of error alert, chip, invalid row highlight
  info: "#1B64C8"              # neutral informational status: hints, in-progress, notices
  info-bg: "#EBF2FD"           # background of info alert, chip
  # --- state semantic: competency rating level (scale 0-4, sequential ramp of brand blue) ---
  rating-level-0: "#EEEEEE"    # level 0 "not confirmed" cell in matrices, cards and rating chips; text navy
  rating-level-1: "#C7D0FB"    # level 1 "works with support"; text navy
  rating-level-2: "#9BA8F9"    # level 2 "independent on typical tasks"; text navy
  rating-level-3: "#2B42E5"    # level 3 "independent on complex tasks"; text white
  rating-level-4: "#081240"    # level 4 "sets standards, develops others"; text white
typography:
  # Weights: only 400 and 700 exist in the PT families; never 500/600. Sizes: 12 / 14 / 16 / 20 / 24 / 30 (+13 code).
  heading-1:
    fontFamily: PT Sans, Segoe UI, Roboto, Arial, sans-serif   # page title (one per screen), empty-state title on full pages
    fontSize: 30px
    fontWeight: 700
    lineHeight: 38px
  heading-2:
    fontFamily: PT Sans, Segoe UI, Roboto, Arial, sans-serif   # section title inside a page, modal title, candidate card name
    fontSize: 24px
    fontWeight: 700
    lineHeight: 32px
  heading-3:
    fontFamily: PT Sans, Segoe UI, Roboto, Arial, sans-serif   # panel / card title, drawer title, wizard step title
    fontSize: 20px
    fontWeight: 700
    lineHeight: 28px
  heading-4:
    fontFamily: PT Sans, Segoe UI, Roboto, Arial, sans-serif   # sub-panel title, descriptions group title, statistic title
    fontSize: 16px
    fontWeight: 700
    lineHeight: 24px
  heading-5:
    fontFamily: PT Sans, Segoe UI, Roboto, Arial, sans-serif   # smallest heading: form-section title, list group title (same size as body, bold)
    fontSize: 14px
    fontWeight: 700
    lineHeight: 22px
  body-lg:
    fontFamily: PT Sans, Segoe UI, Roboto, Arial, sans-serif   # long-form reading: competency descriptions, behavioral indicators, commission comments
    fontSize: 16px
    fontWeight: 400
    lineHeight: 24px
  body-md:
    fontFamily: PT Sans, Segoe UI, Roboto, Arial, sans-serif   # default: forms, buttons, menus, table cells, alerts, tooltips
    fontSize: 14px
    fontWeight: 400
    lineHeight: 22px
  body-strong:
    fontFamily: PT Sans, Segoe UI, Roboto, Arial, sans-serif   # emphasis in running text, table header, button label, form label, active tab
    fontSize: 14px
    fontWeight: 700
    lineHeight: 22px
  body-sm:
    fontFamily: PT Sans, Segoe UI, Roboto, Arial, sans-serif   # captions, helper and validation text, tags, metadata; never the only carrier of critical data
    fontSize: 12px
    fontWeight: 400
    lineHeight: 20px
  data-numeric:
    fontFamily: PT Sans, Segoe UI, Roboto, Arial, sans-serif   # numbers that are compared or summed in columns: scores, weights, rating, percentages
    fontSize: 14px
    fontWeight: 400
    lineHeight: 22px
    fontFeature: '"tnum" 1'
  data-column-header:
    fontFamily: PT Sans Narrow, Arial Narrow, Segoe UI, Arial, sans-serif   # competency-matrix column headers and other long Russian labels in narrow columns
    fontSize: 14px
    fontWeight: 700
    lineHeight: 20px
  code:
    fontFamily: PT Mono, Consolas, Courier New, monospace   # identifiers, codes (level code, wave id), versions, JSON/audit values
    fontSize: 13px
    fontWeight: 400
    lineHeight: 20px
rounded:
  # --- corner radii ---
  rounded-xs: 2px              # small controls (24px), tags, status chips, table sort marks, checkbox
  rounded-md: 4px              # default radius: buttons, inputs, selects, rating chips, menu items
  rounded-lg: 8px              # containers: cards, panels, modals, drawers, dropdown and popover surfaces
  rounded-full: 9999px         # avatars, count badges, radio buttons, switches
  # --- border and focus geometry (not radii; grouped here because the spec has no border key) ---
  border-width: 1px            # outlines of controls, cards, tables and dividers
  focus-ring-width: 2px        # keyboard focus ring thickness on every interactive element
  focus-ring-offset: 2px       # gap between the element edge and the focus ring
spacing:
  # --- scale, 4px base grid ---
  space-xxs: 4px               # icon-to-text micro gap, tag inner gap, tight inline groups
  space-xs: 8px                # gap between related controls, icon-to-label gap, dense cell padding, label-to-input gap
  space-sm: 12px               # table cell block padding, small card body padding, gap between inline fields
  space-md: 16px               # default gap inside cards and forms, grid gutter, table cell inline padding
  space-lg: 20px               # medium panel padding, gap between a section heading and its content
  space-xl: 24px               # card body and modal padding, form item spacing, page gutter, gap between cards
  space-xxl: 32px              # gap between page sections, large empty-state padding
  space-xxxl: 48px             # page top/bottom breathing room, gap before page footer, matrix row height
  # --- page frame ---
  page-gutter: 24px            # left/right/top padding of the content area at desktop widths (>= 992px)
  page-gutter-narrow: 16px     # content-area padding at tablet and mobile widths (< 992px)
  content-max-width: 1440px    # max width of card/form/detail pages; matrix and table pages stay fluid
  reading-max-width: 720px     # max width of forms and long-form text (about 75 characters of body-lg)
  header-height: 56px          # application header (surface-inverse) height
  # --- controls and targets ---
  control-height-sm: 24px      # compact in-table icon actions only; equals the WCAG 2.2 AA minimum target
  control-height-md: 32px      # default height of buttons, inputs, selects, date pickers (desktop)
  control-height-lg: 40px      # primary page action and every control at tablet widths (touch-friendly)
  target-min: 24px             # smallest allowed clickable area incl. icon-only buttons (WCAG 2.5.8); gap to neighbors >= space-xs
  # --- competency matrix ---
  matrix-row-height: 48px      # minimum height of a matrix row (32px rating chip + space-xs above and below)
  matrix-cell-min-width: 72px  # minimum width of a rating/rater column cell
  matrix-name-column-width: 280px  # sticky first column with the competency name (wraps to two lines)
  # --- breakpoints (desktop-first, aligned to antd screen tokens) ---
  breakpoint-md: 768px         # below: mobile adaptive (single column; the header keeps its horizontal menu)
  breakpoint-lg: 992px         # below: tablet (user name leaves the header, page-gutter-narrow, control-height-lg)
  breakpoint-xl: 1200px        # from here: full desktop layout
  breakpoint-xxl: 1600px       # from here: wide screens; content-max-width pages center, tables stay fluid
components:
  # Schema limits: no border-color or multi-value padding property. Strokes are modeled as 1-purpose stroke components; padding holds the inline value (block value is in the body).
  # --- strokes and primitives ---
  control-border:               # 1px outline of input, select, checkbox, radio, default button (stroke color; width = rounded.border-width)
    backgroundColor: "{colors.border-control}"
    height: "{rounded.border-width}"
  divider:                      # hairline between sections, list items, card header and body (stroke color)
    backgroundColor: "{colors.border-subtle}"
    height: "{rounded.border-width}"
  focus-ring:                   # keyboard focus ring on light surfaces (stroke color; width focus-ring-width, offset focus-ring-offset)
    backgroundColor: "{colors.primary}"
    height: "{rounded.focus-ring-width}"
  focus-ring-inverse:           # keyboard focus ring on the dark header (menu items, user button)
    backgroundColor: "{colors.accent}"
    height: "{rounded.focus-ring-width}"
  scrim:                        # backdrop behind modal and drawer
    backgroundColor: "{colors.overlay-scrim}"
  skeleton:                     # loading placeholder block for text, cards and table rows (static under reduced motion)
    backgroundColor: "{colors.surface-muted}"
    rounded: "{rounded.rounded-md}"
  # --- app shell ---
  app-content:                  # content area behind all pages
    backgroundColor: "{colors.surface-page}"
    textColor: "{colors.text-primary}"
    typography: "{typography.body-md}"
    padding: "{spacing.page-gutter}"
  app-header:                   # top bar: brand, horizontal navigation menu, user button
    backgroundColor: "{colors.surface-inverse}"
    textColor: "{colors.text-inverse}"
    typography: "{typography.body-md}"
    height: "{spacing.header-height}"
    padding: "{spacing.space-xl}"
  nav-item:                     # header menu item, default
    backgroundColor: "{colors.surface-inverse}"
    textColor: "{colors.text-inverse-muted}"
    typography: "{typography.body-md}"
    height: "{spacing.control-height-lg}"
    rounded: "{rounded.rounded-md}"
    padding: "{spacing.space-md}"
  nav-item-hover:               # header menu item under pointer: no fill, text brightens to text-inverse
    backgroundColor: "{colors.surface-inverse}"
    textColor: "{colors.text-inverse}"
  nav-item-active:              # header menu item of the current section: filled with primary
    backgroundColor: "{colors.primary}"
    textColor: "{colors.text-inverse}"
  header-button-hover:          # button on the dark header (user button) under pointer: translucent white fill, white text
    backgroundColor: "{colors.overlay-inverse-hover}"
    textColor: "{colors.text-inverse}"
  header-button-pressed:        # the same button while pressed: stronger translucent white fill, white text
    backgroundColor: "{colors.overlay-inverse-pressed}"
    textColor: "{colors.text-inverse}"
  # --- buttons ---
  button-primary:               # the single main action of a page or dialog
    backgroundColor: "{colors.primary}"
    textColor: "{colors.text-inverse}"
    typography: "{typography.body-strong}"
    rounded: "{rounded.rounded-md}"
    height: "{spacing.control-height-md}"
    padding: "{spacing.space-md}"
  button-primary-hover:
    backgroundColor: "{colors.primary-hover}"
    textColor: "{colors.text-inverse}"
  button-primary-pressed:
    backgroundColor: "{colors.primary-active}"
    textColor: "{colors.text-inverse}"
  button-default:               # secondary actions (cancel, back, export)
    backgroundColor: "{colors.surface-panel}"
    textColor: "{colors.text-primary}"
    typography: "{typography.body-strong}"
    rounded: "{rounded.rounded-md}"
    height: "{spacing.control-height-md}"
    padding: "{spacing.space-md}"
  button-default-hover:
    backgroundColor: "{colors.primary-subtle}"
    textColor: "{colors.primary}"
  button-default-pressed:
    backgroundColor: "{colors.primary-subtle}"
    textColor: "{colors.primary-active}"
  button-danger:                # destructive or irreversible action (delete, withdraw, discard)
    backgroundColor: "{colors.danger}"
    textColor: "{colors.text-inverse}"
    typography: "{typography.body-strong}"
    rounded: "{rounded.rounded-md}"
    height: "{spacing.control-height-md}"
    padding: "{spacing.space-md}"
  button-danger-hover:          # hover and pressed state of the destructive button
    backgroundColor: "{colors.danger-hover}"
    textColor: "{colors.text-inverse}"
  button-link:                  # inline and table-row actions that look like links
    backgroundColor: "{colors.surface-panel}"
    textColor: "{colors.primary}"
    typography: "{typography.body-md}"
    height: "{spacing.control-height-sm}"
  button-link-hover:
    backgroundColor: "{colors.surface-panel}"
    textColor: "{colors.primary-hover}"
  button-disabled:              # any button that cannot be used now; the reason is shown next to it
    backgroundColor: "{colors.surface-muted}"
    textColor: "{colors.text-disabled}"
    typography: "{typography.body-strong}"
    rounded: "{rounded.rounded-md}"
    height: "{spacing.control-height-md}"
  # --- form fields ---
  input:                        # text input, select, date picker, textarea (outline = control-border)
    backgroundColor: "{colors.surface-panel}"
    textColor: "{colors.text-primary}"
    typography: "{typography.body-md}"
    rounded: "{rounded.rounded-md}"
    height: "{spacing.control-height-md}"
    padding: "{spacing.space-sm}"
  input-placeholder:            # placeholder and example text inside an empty field
    backgroundColor: "{colors.surface-panel}"
    textColor: "{colors.text-muted}"
    typography: "{typography.body-md}"
  input-disabled:               # field that cannot be edited now; reason shown as help text
    backgroundColor: "{colors.surface-muted}"
    textColor: "{colors.text-disabled}"
    typography: "{typography.body-md}"
    rounded: "{rounded.rounded-md}"
    height: "{spacing.control-height-md}"
  field-label:                  # label above a field
    backgroundColor: "{colors.surface-panel}"
    textColor: "{colors.text-secondary}"
    typography: "{typography.body-strong}"
  field-help:                   # hint under a field (format, limit, reason a field is disabled)
    backgroundColor: "{colors.surface-panel}"
    textColor: "{colors.text-muted}"
    typography: "{typography.body-sm}"
  field-error:                  # validation message under an invalid field (with an error icon)
    backgroundColor: "{colors.surface-panel}"
    textColor: "{colors.danger}"
    typography: "{typography.body-sm}"
  selection-control-checked:    # checked checkbox, selected radio, switch on (unchecked uses control-border)
    backgroundColor: "{colors.primary}"
    textColor: "{colors.text-inverse}"
    rounded: "{rounded.rounded-xs}"
    size: "{spacing.space-md}"
  # --- table ---
  table-header:                 # header row of data tables
    backgroundColor: "{colors.surface-muted}"
    textColor: "{colors.text-secondary}"
    typography: "{typography.body-strong}"
    padding: "{spacing.space-md}"
  table-row:                    # body row of data tables
    backgroundColor: "{colors.surface-panel}"
    textColor: "{colors.text-primary}"
    typography: "{typography.body-md}"
    padding: "{spacing.space-md}"
  table-row-hover:
    backgroundColor: "{colors.surface-page}"
    textColor: "{colors.text-primary}"
  table-row-selected:           # selected row (checkbox or current record)
    backgroundColor: "{colors.primary-subtle}"
    textColor: "{colors.text-primary}"
  table-cell-numeric:           # right-aligned numeric column (scores, weights, percentages)
    backgroundColor: "{colors.surface-panel}"
    textColor: "{colors.text-primary}"
    typography: "{typography.data-numeric}"
  pagination-item:              # page number button under a table
    backgroundColor: "{colors.surface-panel}"
    textColor: "{colors.text-primary}"
    typography: "{typography.body-md}"
    rounded: "{rounded.rounded-md}"
    height: "{spacing.control-height-md}"
  pagination-item-hover:
    backgroundColor: "{colors.primary-subtle}"
    textColor: "{colors.primary}"
  pagination-item-active:       # current page (primary border and text)
    backgroundColor: "{colors.surface-panel}"
    textColor: "{colors.primary}"
    typography: "{typography.body-strong}"
  # --- competency matrix ---
  matrix-header:                # sticky header cell of a matrix column (level, position or rater)
    backgroundColor: "{colors.surface-muted}"
    textColor: "{colors.text-primary}"
    typography: "{typography.data-column-header}"
    width: "{spacing.matrix-cell-min-width}"
    padding: "{spacing.space-xs}"
  matrix-name-cell:             # sticky first column cell with the competency name
    backgroundColor: "{colors.surface-panel}"
    textColor: "{colors.text-primary}"
    typography: "{typography.body-md}"
    width: "{spacing.matrix-name-column-width}"
    height: "{spacing.matrix-row-height}"
    padding: "{spacing.space-sm}"
  matrix-cell:                  # data cell hosting a rating chip, centered
    backgroundColor: "{colors.surface-panel}"
    textColor: "{colors.text-primary}"
    typography: "{typography.body-md}"
    width: "{spacing.matrix-cell-min-width}"
    height: "{spacing.matrix-row-height}"
    padding: "{spacing.space-xs}"
  rating-chip-0:                # level 0 chip: competency not confirmed
    backgroundColor: "{colors.rating-level-0}"
    textColor: "{colors.surface-inverse}"
    typography: "{typography.body-strong}"
    rounded: "{rounded.rounded-md}"
    size: "{spacing.control-height-md}"
  rating-chip-1:                # level 1 chip: works with support
    backgroundColor: "{colors.rating-level-1}"
    textColor: "{colors.surface-inverse}"
    typography: "{typography.body-strong}"
    rounded: "{rounded.rounded-md}"
    size: "{spacing.control-height-md}"
  rating-chip-2:                # level 2 chip: independent on typical tasks
    backgroundColor: "{colors.rating-level-2}"
    textColor: "{colors.surface-inverse}"
    typography: "{typography.body-strong}"
    rounded: "{rounded.rounded-md}"
    size: "{spacing.control-height-md}"
  rating-chip-3:                # level 3 chip: independent on complex tasks
    backgroundColor: "{colors.rating-level-3}"
    textColor: "{colors.text-inverse}"
    typography: "{typography.body-strong}"
    rounded: "{rounded.rounded-md}"
    size: "{spacing.control-height-md}"
  rating-chip-4:                # level 4 chip: sets standards, develops others
    backgroundColor: "{colors.rating-level-4}"
    textColor: "{colors.text-inverse}"
    typography: "{typography.body-strong}"
    rounded: "{rounded.rounded-md}"
    size: "{spacing.control-height-md}"
  # --- candidate card ---
  candidate-card:               # single card of a candidate: summary, ratings, evidence (commission, manager, HR)
    backgroundColor: "{colors.surface-panel}"
    textColor: "{colors.text-primary}"
    typography: "{typography.heading-2}"
    rounded: "{rounded.rounded-lg}"
    padding: "{spacing.space-xl}"
  candidate-card-selected:      # card chosen in a list for comparison
    backgroundColor: "{colors.primary-subtle}"
    textColor: "{colors.text-primary}"
  candidate-card-meta:          # position, block, wave, dates on the card
    backgroundColor: "{colors.surface-panel}"
    textColor: "{colors.text-secondary}"
    typography: "{typography.body-md}"
  # --- status tag ---
  tag-success:                  # positive state: approved, ready, requirement met, delivered
    backgroundColor: "{colors.success-bg}"
    textColor: "{colors.success}"
    typography: "{typography.body-sm}"
    rounded: "{rounded.rounded-xs}"
    padding: "{spacing.space-xs}"
  tag-warning:                  # attention state: below target, evidence requested, deadline near
    backgroundColor: "{colors.warning-bg}"
    textColor: "{colors.warning}"
    typography: "{typography.body-sm}"
    rounded: "{rounded.rounded-xs}"
    padding: "{spacing.space-xs}"
  tag-danger:                   # blocking state: critical competency gap, rejected, cancelled
    backgroundColor: "{colors.danger-bg}"
    textColor: "{colors.danger}"
    typography: "{typography.body-sm}"
    rounded: "{rounded.rounded-xs}"
    padding: "{spacing.space-xs}"
  tag-info:                     # in-progress state: under review, commission, collecting evidence
    backgroundColor: "{colors.info-bg}"
    textColor: "{colors.info}"
    typography: "{typography.body-sm}"
    rounded: "{rounded.rounded-xs}"
    padding: "{spacing.space-xs}"
  tag-neutral:                  # passive state: draft, archived, closed
    backgroundColor: "{colors.surface-muted}"
    textColor: "{colors.text-secondary}"
    typography: "{typography.body-sm}"
    rounded: "{rounded.rounded-xs}"
    padding: "{spacing.space-xs}"
  # --- navigation inside a page ---
  tab:                          # tab label, default
    backgroundColor: "{colors.surface-panel}"
    textColor: "{colors.text-secondary}"
    typography: "{typography.body-md}"
    height: "{spacing.control-height-lg}"
    padding: "{spacing.space-md}"
  tab-hover:
    backgroundColor: "{colors.surface-panel}"
    textColor: "{colors.text-primary}"
  tab-active:                   # current tab (primary text and 2px primary indicator line)
    backgroundColor: "{colors.surface-panel}"
    textColor: "{colors.primary}"
  step-current:                 # wave stage in progress
    backgroundColor: "{colors.primary}"
    textColor: "{colors.text-inverse}"
    typography: "{typography.body-strong}"
    rounded: "{rounded.rounded-full}"
    size: "{spacing.control-height-md}"
  step-done:                    # completed wave stage (with a check icon)
    backgroundColor: "{colors.primary-subtle}"
    textColor: "{colors.primary}"
    typography: "{typography.body-strong}"
    rounded: "{rounded.rounded-full}"
    size: "{spacing.control-height-md}"
  step-upcoming:                # stage not reached yet
    backgroundColor: "{colors.surface-muted}"
    textColor: "{colors.text-secondary}"
    typography: "{typography.body-strong}"
    rounded: "{rounded.rounded-full}"
    size: "{spacing.control-height-md}"
  breadcrumb:                   # path above the page title
    backgroundColor: "{colors.surface-page}"
    textColor: "{colors.text-secondary}"
    typography: "{typography.body-md}"
  breadcrumb-current:           # last breadcrumb: the current page (not a link)
    backgroundColor: "{colors.surface-page}"
    textColor: "{colors.text-primary}"
    typography: "{typography.body-md}"
  # --- overlays ---
  modal:                        # dialog for focused tasks and confirmations; sits on scrim
    backgroundColor: "{colors.surface-panel}"
    textColor: "{colors.text-primary}"
    typography: "{typography.heading-2}"
    rounded: "{rounded.rounded-lg}"
    padding: "{spacing.space-xl}"
  drawer:                       # side panel: record details, filters, evidence preview
    backgroundColor: "{colors.surface-panel}"
    textColor: "{colors.text-primary}"
    typography: "{typography.heading-3}"
    rounded: "{rounded.rounded-lg}"
    padding: "{spacing.space-xl}"
  dropdown-item:                # option in select, date-picker popup, user menu, row-action menu
    backgroundColor: "{colors.surface-panel}"
    textColor: "{colors.text-primary}"
    typography: "{typography.body-md}"
    rounded: "{rounded.rounded-md}"
    height: "{spacing.control-height-md}"
    padding: "{spacing.space-sm}"
  dropdown-item-hover:
    backgroundColor: "{colors.surface-page}"
    textColor: "{colors.text-primary}"
  dropdown-item-selected:       # chosen option (also check icon)
    backgroundColor: "{colors.primary-subtle}"
    textColor: "{colors.text-primary}"
    typography: "{typography.body-strong}"
  tooltip:                      # short hint on hover; never the only carrier of information
    backgroundColor: "{colors.surface-inverse}"
    textColor: "{colors.text-inverse}"
    typography: "{typography.body-sm}"
    rounded: "{rounded.rounded-md}"
    padding: "{spacing.space-xs}"
  toast:                        # transient notification for completed actions (save, send, export)
    backgroundColor: "{colors.surface-panel}"
    textColor: "{colors.text-primary}"
    typography: "{typography.body-md}"
    rounded: "{rounded.rounded-lg}"
    padding: "{spacing.space-md}"
  # --- inline feedback ---
  alert-info:                   # neutral notice inside a page or form
    backgroundColor: "{colors.info-bg}"
    textColor: "{colors.text-primary}"
    typography: "{typography.body-md}"
    rounded: "{rounded.rounded-md}"
    padding: "{spacing.space-sm}"
  alert-success:                # confirmation that stays on the page (wave published, decision recorded)
    backgroundColor: "{colors.success-bg}"
    textColor: "{colors.text-primary}"
    typography: "{typography.body-md}"
    rounded: "{rounded.rounded-md}"
    padding: "{spacing.space-sm}"
  alert-warning:                # risk to user's work (incomplete ratings, deadline near, matrix not published)
    backgroundColor: "{colors.warning-bg}"
    textColor: "{colors.text-primary}"
    typography: "{typography.body-md}"
    rounded: "{rounded.rounded-md}"
    padding: "{spacing.space-sm}"
  alert-danger:                 # blocking error or irreversible-action warning
    backgroundColor: "{colors.danger-bg}"
    textColor: "{colors.text-primary}"
    typography: "{typography.body-md}"
    rounded: "{rounded.rounded-md}"
    padding: "{spacing.space-sm}"
  empty-state:                  # list, matrix or search with no data: what, why, what next
    backgroundColor: "{colors.surface-panel}"
    textColor: "{colors.text-secondary}"
    typography: "{typography.body-md}"
    padding: "{spacing.space-xxl}"
  avatar:                       # initials of an employee in cards, lists and the header user menu
    backgroundColor: "{colors.primary-subtle}"
    textColor: "{colors.primary}"
    typography: "{typography.body-strong}"
    rounded: "{rounded.rounded-full}"
    size: "{spacing.control-height-md}"
---

# Competency Management Service - Design System

## Overview

The service is an internal corporate platform for versioned competency matrices and the promotion process: self-assessment, peer review, commission, ranking, decision and feedback. The interface is corporate, calm, trustworthy and transparent. It supports decisions that must be reproducible and evidence-based, and it handles confidential personnel data, so it is restrained: flat surfaces, squared corners, no decoration, no motion for its own sake. Readability of data and of each candidate's state comes before atmosphere (ux-principles 1).

Identity comes from the corporate REC/EXIAR presentation template: brand blue for action, brand navy for the shell, brand orange as a rare accent fill, charcoal text on light surfaces. Typography is the PT family, chosen as a license-suitable replacement for the template fonts. Hierarchy follows decision importance (ux-principles 2); every alert, error and empty state says what, why, what next (ux-principles 3).

Technical base: React with antd 6, driven from these tokens through `ConfigProvider`. The service runs in an isolated corporate network, so fonts and icons ship inside the app image and nothing is loaded from the internet.

Platform: web SPA, desktop-first (responsive down to tablet; mobile adaptive only). Density: comfortable (antd default size, not compact). Light theme only; dark mode is an open question.

## Colors

Palette is taken from the corporate REC/EXIAR presentation template (dominant fill blue `2B42E5`, brand navy `081240`, brand accent orange `FBAE40`, charcoal text `292929`, neutral `EEEEEE`). Office-default colors found in the template (`FF0000`, `FFC000`, `00B050`, `00B0F0`, `0070C0`) and its theme accents are NOT brand and are not used; semantic success, warning, danger and info are derived to pass WCAG AA on this palette.

Token layers: the hex value itself is the primitive layer; every token above is semantic (purpose-named) or state-semantic (`rating-level-*`). Component tokens reference them in the Components section.

### Roles

| Token | Role | antd 6 token |
|---|---|---|
| `primary`, `primary-hover`, `primary-active` | primary action and link | `colorPrimary`, `colorPrimaryHover`, `colorPrimaryActive`, `colorLink` |
| `primary-subtle` | selected / hovered background | `colorPrimaryBg` |
| `text-primary` | base text | `colorTextBase` (and `colorText`) |
| `text-secondary` | secondary text | `colorTextSecondary` |
| `text-muted` | hints, placeholders | `colorTextTertiary`, `colorTextPlaceholder` |
| `text-disabled` | disabled text | `colorTextDisabled`, `colorTextQuaternary` |
| `text-inverse` | text on filled backgrounds | `colorTextLightSolid` |
| `surface-page` | page background | `colorBgLayout` |
| `surface-panel` | panels, popups | `colorBgContainer`, `colorBgElevated` |
| `surface-muted` | neutral fill | `colorFillTertiary`, `colorBgContainerDisabled`, `Table.headerBg` |
| `border-control` | control outlines | `colorBorder` |
| `border-subtle` | dividers | `colorBorderSecondary`, `colorSplit` |
| `overlay-scrim` | modal mask | `colorBgMask` |
| `overlay-inverse-hover`, `overlay-inverse-pressed` | hover / pressed fill of the header user button | `.header-user-button` sets `--ant-btn-bg-color-hover` / `--ant-btn-bg-color-active` (`color-mix` of `colorTextLightSolid` at 12% / 20%; the same values) |
| `success`, `warning`, `danger`, `info` | status | `colorSuccess`, `colorWarning`, `colorError`, `colorInfo` |
| `*-bg` | status backgrounds | `colorSuccessBg`, `colorWarningBg`, `colorErrorBg`, `colorInfoBg` |
| `danger-hover` | destructive hover | `colorErrorHover` |
| `surface-inverse`, `text-inverse-muted` | dark header and its horizontal menu | `Layout.headerBg`, `Menu.darkItemBg`, `Menu.darkItemColor` |
| `accent`, `rating-level-*` | no antd seed token | custom components via CSS variables |

antd mapping rules:

- Set every text token explicitly. antd derives `colorTextSecondary`, `colorTextTertiary`, `colorTextQuaternary` and the placeholder color as alpha tints of `colorTextBase` (65% / 45% / 25%), which fall below 4.5:1 on white. Leave none of them to the algorithm.
- Use antd's default (light) algorithm, not `compactAlgorithm`.
- Set `colorPrimaryBorder` = `primary` explicitly: antd draws the focus ring with it (see Shapes, focus ring). Other hover/border tints not listed here (`colorPrimaryBgHover`, status border/hover tints) may be left to the antd algorithm: they are decorative or used only as non-text fills.

### Contrast (WCAG 2.x relative luminance, computed from the hex values; accuracy about +/-0.05)

AA thresholds: 4.5:1 normal text, 3:1 large text (>= 24px, or >= 18.66px bold) and non-text UI components.

| Foreground on background | Ratio | Result |
|---|---|---|
| `text-primary` on `surface-panel` / `surface-page` / `surface-muted` / `primary-subtle` | 14.6 / 13.4 / 12.6 / 12.9 | AA, AAA |
| `text-secondary` on `surface-panel` / `surface-page` / `surface-muted` | 8.7 / 8.1 / 7.5 | AA, AAA |
| `text-muted` on `surface-panel` / `surface-page` / `surface-muted` / `primary-subtle` | 5.8 / 5.4 / 5.0 / 5.2 | AA |
| `text-inverse` on `primary` / `primary-hover` / `primary-active` | 7.0 / 8.7 / 9.6 | AA, AAA at base and hover |
| `primary` (link) on `surface-panel` / `surface-page` / `primary-subtle` | 7.0 / 6.5 / 6.2 | AA |
| `text-inverse` on `surface-inverse` | 17.9 | AAA |
| `text-inverse-muted` on `surface-inverse` | 8.0 | AAA |
| `text-inverse` on `surface-inverse` under `overlay-inverse-hover` / `overlay-inverse-pressed` (composite fill about `#262E57` / `#394166`) | 13.0 / 9.9 | AAA |
| `text-inverse` on `success` / `warning` / `danger` / `danger-hover` / `info` | 5.4 / 5.2 / 5.6 / 6.7 / 5.7 | AA |
| `success` on `surface-panel` / `success-bg` | 5.4 / 4.7 | AA |
| `warning` on `surface-panel` / `warning-bg` | 5.2 / 4.7 | AA |
| `danger` on `surface-panel` / `danger-bg` | 5.6 / 4.9 | AA |
| `info` on `surface-panel` / `info-bg` | 5.7 / 5.0 | AA |
| `surface-inverse` on `rating-level-0` / `-1` / `-2` | 15.5 / 11.8 / 8.0 | AA, AAA |
| `text-inverse` on `rating-level-3` / `-4` | 7.0 / 17.9 | AA, AAA |
| `surface-inverse` on `accent` | 9.6 | AA, AAA |
| `text-primary` on `accent` | 7.8 | AA, AAA |
| `border-control` on `surface-panel` / `surface-page` (non-text) | 3.7 / 3.5 | AA non-text (>= 3:1) |
| `primary` focus ring on `surface-panel`; `accent` focus ring on `surface-inverse` | 7.0 / 9.6 | AA non-text |
| `accent` as TEXT or icon on `surface-panel` / `surface-page` | 1.9 / 1.7 | FAIL: forbidden |
| `text-inverse` on `accent` | 1.9 | FAIL: forbidden |
| `text-inverse-muted` on `primary` | 3.1 | FAIL for text: use `text-inverse` |
| `text-disabled` on `surface-panel` | 2.4 | below AA, permitted only for disabled controls (WCAG 1.4.3 exception) |

### Usage rules

- `accent` (`FBAE40`) never carries text or a status meaning on a light surface. It may be a fill (progress bar, badge, marker) only with `surface-inverse` or `text-primary` text on it (>= 7.8:1), or a mark on `surface-inverse` (9.6:1). It is brand decoration, not a status: warning is `warning`, so brand orange and warning never share a meaning.
- Color never carries meaning alone (WCAG 1.4.1): every status chip, matrix cell and gap marker also shows an icon or text; every `rating-level-*` cell shows its digit.
- `rating-level-0..4` is a sequential ramp (lightness falls as level rises), so it stays distinguishable for color-vision deficiencies. Text on levels 0-2 is `surface-inverse`; on levels 3-4 it is `text-inverse`. Comparison against the target level uses status tokens (`success` met, `warning` below target, `danger` critical gap), never the ramp.
- Critical alerts use `danger` rarely (irreversible or blocking events only), per notification levels: `info` = `info`, `positive` = `success`, `warning` = `warning`, `critical` = `danger`.
- Focus ring: `primary` on light surfaces, `accent` on `surface-inverse`; width and offset are set in the Shapes/Components sections.
- Light theme only. These tokens are not remapped by any future theme (design-system rule 9): status colors, hit targets and spacing stay fixed.

## Typography

### Decision record

The user chose the PT family (ParaType) as a license-suitable replacement for the template fonts Circe and Akrobat (Inter and Montserrat from the template are not used). Base size is 14px, the antd default, because the product is data-heavy (matrices, candidate tables): antd control metrics (`controlHeight` 32, paddings) are built around 14px, and 16px would cut visible table rows by about 15% without a density gain. Comfort comes from the 22px line-height and generous cell padding (Layout section). Long-form text (competency descriptions, comments) uses `body-lg` 16px. PT Sans has open apertures and a large x-height and was designed for on-screen text, so 14px stays readable in Cyrillic.

### Families and roles

| Family | Weights bundled | Role |
|---|---|---|
| PT Sans | 400, 700 | UI and body text, headings, forms, tables, numbers (`heading-*`, `body-*`, `data-numeric`) |
| PT Sans Narrow | 700 | competency-matrix column headers and long labels in narrow columns (`data-column-header`) |
| PT Mono | 400 | identifiers, codes, versions (`code`); never bold or italic |

- PT Sans Caption is not used: it is wider than PT Sans, which costs width in dense tables, and 12px text is already served by PT Sans. Headings use PT Sans bold (not Narrow) because antd has no heading-specific font token, so this keeps the antd mapping free of CSS overrides.
- Only weights 400 and 700 exist. Never specify 500 or 600 (antd's default `fontWeightStrong` is 600 and must be overridden to 700). Italics are not bundled and are not used in the UI.
- Fallback stacks (system fonts present on corporate Windows/Linux desktops, all with Cyrillic): `PT Sans, Segoe UI, Roboto, Arial, sans-serif`; `PT Sans Narrow, Arial Narrow, Segoe UI, Arial, sans-serif`; `PT Mono, Consolas, Courier New, monospace`.

### Scale

| Token | Size / line-height | Weight | Use |
|---|---|---|---|
| `heading-1` | 30 / 38 | 700 | page title |
| `heading-2` | 24 / 32 | 700 | section, modal, card name |
| `heading-3` | 20 / 28 | 700 | panel and card title |
| `heading-4` | 16 / 24 | 700 | sub-panel title |
| `heading-5` | 14 / 22 | 700 | form-section and group title |
| `body-lg` | 16 / 24 | 400 | long-form reading |
| `body-md` | 14 / 22 | 400 | default text |
| `body-strong` | 14 / 22 | 700 | emphasis, table header, form label |
| `body-sm` | 12 / 20 | 400 | captions, helper text, tags |
| `data-numeric` | 14 / 22 | 400, tabular figures | compared numbers |
| `data-column-header` | 14 / 20 | 700, Narrow | matrix column headers |
| `code` | 13 / 20 | 400, Mono | identifiers and codes |

### Rules

- Readability (ux-principles 1): 12px (`body-sm`) is the floor and is for secondary metadata only. Critical data, errors and the state of a candidate are 14px or larger. Hierarchy follows importance (ux-principles 2): size and weight mark decision-critical content, never decoration.
- Numbers that are compared or summed down a column (scores, weights, ratings, percentages) use `data-numeric` and are right-aligned. A single digit in a `rating-level-*` cell uses `body-strong`.
- No uppercase transforms and no letter-spacing on Cyrillic labels (hurts word-shape recognition); `letterSpacing` stays at the font default.
- Cap running text at about 75 characters per line.
- Text colors come from the Colors section; a size or weight never replaces the contrast requirement.

### Fonts: license, coverage, delivery

- License: PT Sans, PT Sans Narrow and PT Mono are distributed by ParaType under the PT Free Font License v1.3, and on Google Fonts under SIL OFL 1.1 (reserved names "PT Sans" and "ParaType"). Both allow use, embedding and bundling with commercial products at no charge. Conditions: do not sell the fonts by themselves; ship the license text and copyright notice together with the font files; a modified version must not keep the original name without written permission from ParaType. Sources: https://spdx.org/licenses/ParaType-Free-Font-1.3.html, https://github.com/google/fonts/tree/main/ofl/ptsans.
- Coverage: the Google Fonts metadata for PT Sans, PT Sans Narrow and PT Mono lists `cyrillic`, `cyrillic-ext`, `latin`, `latin-ext`. PT Sans ships 400, 700 and their italics; PT Sans Narrow ships 400 and 700; PT Mono ships 400 only.
- Delivery (offline network, no CDN, no web-font service): the font files are self-hosted by the app itself and included in the app image: PT Sans Regular and Bold, PT Sans Narrow Bold, PT Mono Regular (4 files), WOFF2, same origin, with the license text and copyright notice next to them. Do not subset the glyph sets: subsetting counts as modification and would raise the reserved-name question; the full files are small. The fallback stacks above are used only if loading fails.
- Not verified yet (spike before implementation, on the files actually shipped): whether the free PT Sans build provides the `tnum` feature (`data-numeric` degrades to proportional digits, and ranking or score columns would then use PT Mono), and presence of glyphs such as `№`, `≥`, `—`, `«»` and the ruble sign.

### antd 6 mapping

| DESIGN.md | antd token |
|---|---|
| `body-md` family stack | `fontFamily` |
| `code` family stack | `fontFamilyCode` |
| `body-md` size 14 | `fontSize` |
| `body-sm` size 12 / `body-lg` size 16 / `heading-3` size 20 | `fontSizeSM` / `fontSizeLG` / `fontSizeXL` |
| `heading-1` ... `heading-5` size | `fontSizeHeading1` 30, `fontSizeHeading2` 24, `fontSizeHeading3` 20, `fontSizeHeading4` 16, `fontSizeHeading5` 14 |
| line-heights 22/14, 24/16, 20/12 | `lineHeight` 1.5714, `lineHeightLG` 1.5, `lineHeightSM` 1.6667 |
| line-heights 38/30, 32/24, 28/20, 24/16, 22/14 | `lineHeightHeading1` 1.2667, `lineHeightHeading2` 1.3333, `lineHeightHeading3` 1.4, `lineHeightHeading4` 1.5, `lineHeightHeading5` 1.5714 |
| bold weight 700 | `fontWeightStrong` (override the default 600) |
| `data-column-header`, `data-numeric` | no antd token: custom column-header class and `font-variant-numeric: tabular-nums` on numeric columns |

antd's default algorithm already yields 14 / 12 / 16 / 20 for `fontSize` / `fontSizeSM` / `fontSizeLG` / `fontSizeXL`; the heading sizes differ from its defaults (38 / 30 / 24 / 20 / 16) and must be set explicitly. Do not use `compactAlgorithm`.

## Layout

Comfortable density (antd default size, not compact). The base grid is 4px; every spacing, size and offset is a multiple of 4. Proximity carries meaning (ux-principles 2): related items sit closer than unrelated ones, using the scale steps below.

### Spacing rules

| Situation | Step |
|---|---|
| icon to text, tag inner gap | `space-xxs` / `space-xs` |
| label to its input, controls in one toolbar row | `space-xs` |
| inside a card or form block | `space-md` |
| form item to form item, card body and modal padding | `space-xl` |
| section heading to its content | `space-lg` |
| card to card, grid gutter between columns | `space-md` (grid) / `space-xl` (stacked cards) |
| page section to page section | `space-xxl` |

### Page frame

- Application shell: header `header-height` (`surface-inverse`) holding the brand, the horizontal navigation menu and the user button; there is no sider. Content area on `surface-page` with `page-gutter` padding.
- Card, form and detail pages are capped at `content-max-width` and centered; forms and long text are capped at `reading-max-width`. Competency matrices, candidate lists and other tables are fluid to the full content width.
- Grid: antd 24-column Row/Col with a `space-md` gutter in both directions.

### Breakpoints (desktop-first)

| Range | Layout |
|---|---|
| >= `breakpoint-xl` (1200px) | full desktop layout |
| `breakpoint-lg` to `breakpoint-xl` (992-1199px) | desktop; content-area columns reflow, no loss of function |
| `breakpoint-md` to `breakpoint-lg` (768-991px) | tablet: the header keeps its horizontal menu but the user name leaves it (avatar only), `page-gutter-narrow`, controls at `control-height-lg`, two-column forms become one column |
| < `breakpoint-md` (768px) | mobile, adaptive only: single column, the header keeps its horizontal menu (the brand name shrinks first), tables and matrices scroll horizontally with the first column sticky; no function is removed, authoring-heavy screens may be marked desktop-recommended |

### Controls and touch targets

- Default control height is `control-height-md` (32px); the primary action of a page and every control at tablet widths use `control-height-lg` (40px). `control-height-sm` (24px) is only for icon actions inside table rows.
- No clickable area is smaller than `target-min` (24x24, WCAG 2.5.8 AA), and neighboring targets are at least `space-xs` apart. Icon-only buttons are at least 32x32 on desktop.
- A disabled control still keeps its size and shows its reason (design-system rule 7).

### Competency matrix

- Row height is at least `matrix-row-height` (48px): a 32px rating chip with `space-xs` above and below. Cell padding is `space-xs` block and `space-sm` inline. There is one density: no compact matrix variant.
- First column holds the competency name, is sticky, is `matrix-name-column-width` wide and wraps to two lines before it is truncated; truncation always exposes the full name in a tooltip and never hides the critical-competency marker.
- Rating and rater columns are at least `matrix-cell-min-width` (72px) wide and never shrink below it; the matrix scrolls horizontally instead. The header row is sticky and holds `data-column-header` text that wraps to at most three lines (60px of text plus `space-xs` padding).
- A rating cell shows the digit in `body-strong` centered in its `rating-level-*` chip; a clickable cell is the whole 72x48 area.

### antd 6 mapping

| DESIGN.md | antd token |
|---|---|
| `space-xxs`, `space-xs`, `space-sm`, `space-md` | `paddingXXS` 4, `paddingXS` 8, `paddingSM` 12, `padding` 16; same values for `marginXXS`, `marginXS`, `marginSM`, `margin` |
| `space-lg`, `space-xl`, `space-xxl` | `paddingMD`/`marginMD` 20, `paddingLG`/`marginLG` 24, `paddingXL`/`marginXL` 32 |
| `space-xxxl` | `marginXXL` 48 |
| `control-height-sm` / `-md` / `-lg` | `controlHeightSM` 24 / `controlHeight` 32 / `controlHeightLG` 40 |
| 4px grid | `sizeUnit` 4, `sizeStep` 4 (antd defaults) |
| `header-height` | `Layout.headerHeight` 56, `Layout.headerPadding` `0 24px` |
| `page-gutter` | content-area padding (custom CSS variable, not an antd token) |
| `breakpoint-md` ... `-xxl` | `screenMD` 768, `screenLG` 992, `screenXL` 1200, `screenXXL` 1600 |
| table cell | `Table.cellPaddingBlock` 12 (`space-sm`), `Table.cellPaddingInline` 16 (`space-md`); `size="middle"` (`cellPaddingBlockMD` 8, `cellPaddingInlineMD` 12) only for secondary nested tables |
| form | `Form.itemMarginBottom` 24 (`space-xl`), `Form.verticalLabelPadding` `0 0 8px` (`space-xs`), vertical layout by default |
| card | `Card.bodyPadding` 24, `Card.bodyPaddingSM` 12, `Card.headerPadding` 24 |
| modal and drawer | content padding 24 (`paddingLG`, `Modal.contentPadding`, `Drawer` `styles.body`) |
| `Space` / `Row` gutter | `size` small 8 / middle 16 / large 24; `gutter` 16 |
| menu | `Menu.itemHeight` 40 (antd default, = `control-height-lg`) |

`matrix-*` tokens have no antd counterpart: they are applied as custom column widths and a custom cell component on top of `Table`. antd defaults not listed here stay at the default (comfortable) size; do not use `compactAlgorithm`.

## Elevation & Depth

Flat, restrained enterprise style: hierarchy comes from surface layering (`surface-page` behind `surface-panel`) and 1px `border-subtle` borders, not from shadows. Shadows exist only for things that float above the page. The spec has no shadow key, so the values are defined here (DESIGN.md primitive layer); they use the brand navy `081240` as shadow color, never black or a status color.

| Token | Value | Use |
|---|---|---|
| `elevation-none` | none | cards, panels, tables, forms, header: flat, bounded by `border-subtle` |
| `elevation-sticky` | `0 1px 2px rgba(8, 18, 64, 0.08)` | edge of the sticky matrix header and sticky first column, only while content scrolls under it |
| `elevation-popup` | `0 4px 12px rgba(8, 18, 64, 0.12)` | dropdown, select and date-picker popups, popover, tooltip, context menu |
| `elevation-modal` | `0 8px 24px rgba(8, 18, 64, 0.16)` | modal, drawer, notification; always above `overlay-scrim` |

Rules:

- Three levels above flat, no more. A shadow is never the only signal of a boundary or of state (ux-principles 1): floating surfaces also keep their `border-subtle` border.
- Static content never gets a shadow, and hover does not raise a card (hover is a border or background change).
- Drawer shadow falls toward the content it overlays; the value is the same as `elevation-modal`.
- Stacking order (z-index) is left to antd defaults.

antd 6 mapping: `boxShadow` = `elevation-modal`, `boxShadowSecondary` = `elevation-popup`, `boxShadowTertiary` = `elevation-sticky`; component shadows `Button.defaultShadow`, `Button.primaryShadow`, `Button.dangerShadow` = `none` (flat buttons). Drawer directional shadows (`boxShadowDrawerLeft` etc.) follow `elevation-modal`. Verify at implementation which components read which token.

## Shapes

Restrained, slightly squared corners: 4px for controls, 8px for containers, 2px for small elements. No fully rounded buttons and no decorative frames.

### Radii

| Token | Value | Use |
|---|---|---|
| `rounded-xs` | 2px | small controls, tags, status chips, checkbox |
| `rounded-md` | 4px | buttons, inputs, selects, rating chips, menu items |
| `rounded-lg` | 8px | cards, panels, modals, drawers, popups |
| `rounded-full` | 9999px | avatars, count badges, radio, switch |

Matrix and table cells are square (edge to edge); only the chip inside a cell is rounded. A theme never changes radii (design-system rule 9).

### Borders

- Border width is `border-width` (1px) everywhere. Control outlines (input, select, checkbox, radio, default button) use `border-control` (3.7:1 on `surface-panel`, WCAG 1.4.11). Containers and dividers use `border-subtle` (decorative; the container is also identified by its content and `surface-panel` fill).
- States change color, not width: hover `primary-hover` border, selected/focused `primary`, invalid `danger` together with an icon and message (never color alone), disabled `border-subtle` with the reason shown.

### Focus ring (WCAG 2.4.7, 1.4.11)

- Every interactive element shows a ring on keyboard focus (`:focus-visible`): `focus-ring-width` (2px) solid `primary`, `focus-ring-offset` (2px) from the element edge, following the element's radius. On `surface-inverse` the ring is `accent`. Contrast of the ring: 7.0:1 on `surface-panel`, 6.5:1 on `surface-page`, 6.2:1 on `primary-subtle`, 9.6:1 (`accent`) on `surface-inverse`; also meets the 2px / 3:1 geometry of 2.4.13.
- The ring is never removed without an equal replacement. Inputs additionally switch their border to `primary` on focus. The offset keeps the ring outside filled elements, so `primary`, `rating-level-3` and `rating-level-4` fills do not hide it.

### antd 6 mapping

| DESIGN.md | antd token |
|---|---|
| `rounded-md` 4 | `borderRadius` |
| `rounded-xs` 2 | `borderRadiusSM`, `borderRadiusXS` |
| `rounded-lg` 8 | `borderRadiusLG` (cards, modals, popups) |
| `border-width` 1 | `lineWidth` |
| `focus-ring-width` 2 | `lineWidthFocus`, `controlOutlineWidth` |
| `focus-ring-offset` 2 | `controlOutline` offset is not tokenized: set `outline-offset` in the global `:focus-visible` rule |
| focus color `primary` | `colorPrimaryBorder` = `primary` (Colors, antd mapping rules) |
| `border-control`, `border-subtle` | `colorBorder`, `colorBorderSecondary` (Colors section) |

`controlOutline` (the soft glow around focused inputs) may stay derived because the `primary` border carries the 3:1 requirement. Where an antd component still draws focus with a derived color, a global `:focus-visible` rule built from the tokens above overrides it.

## Motion

Motion only explains a state change or where something came from (a popup opening from its trigger, a drawer entering from the edge). It is never decoration. The spec has no motion section, so the values are defined here.

| Token | Value | Use |
|---|---|---|
| `motion-fast` | 100ms | hover and focus color changes, checkbox/radio/switch toggle, button press |
| `motion-mid` | 200ms | dropdown, select, popover and tooltip open/close, collapse/expand, tab indicator |
| `motion-slow` | 300ms | modal and drawer enter/exit; the longest allowed UI transition |
| `motion-ease-out` | `cubic-bezier(0.215, 0.61, 0.355, 1)` | elements entering |
| `motion-ease-in` | `cubic-bezier(0.55, 0.055, 0.675, 0.19)` | elements leaving |
| `motion-ease-standard` | `cubic-bezier(0.645, 0.045, 0.355, 1)` | in-place changes (color, size, position) |

Rules:

- No transition longer than `motion-slow`. No bounce or overshoot easing, no parallax, no page-transition animation (route changes are instant), no looping decoration. The only continuous animation is a loading indicator, which signals progress and is essential.
- Data never animates: matrix cells, ratings and rankings update in place without count-up or flash; a change worth noticing is marked with an icon or text, not motion.
- Click ripple (antd wave) is decorative and is always disabled.
- Reduced motion: under `prefers-reduced-motion: reduce` all transitions are removed (opacity-only fades of at most `motion-fast` are allowed) and skeleton shimmer becomes a static placeholder. Nothing relies on motion to convey information.

antd 6 mapping: `motionUnit` 0.1, `motionDurationFast` 0.1s, `motionDurationMid` 0.2s, `motionDurationSlow` 0.3s (antd defaults, derived from `motionUnit`); `motionEaseOut`, `motionEaseIn` and `motionEaseInOut` set to the three curves above (`motion-ease-standard` = `motionEaseInOut`); `motionEaseOutBack` and `motionEaseInBack` are not used. Reduced motion: when `prefers-reduced-motion: reduce` matches, the app sets the antd `motion` token to `false`. ConfigProvider `wave={{ disabled: true }}` always.

## Components

Components are the ones the concept needs (an internal HR and competency-matrix SPA on antd 6): shell, buttons, form fields, tables, competency matrix, candidate card, status tags, tabs and steps, overlays, inline feedback. Each is built only from tokens above. Component tokens reference colors, typography, spacing and rounded tokens; text and background of every pair pass WCAG AA (ratios from the Colors section) except disabled states.

### Cross-component rules

- **States.** Every interactive component has default, hover, focus, pressed and disabled (selected / danger where relevant). Focus is always the `focus-ring` (`focus-ring-inverse` on the dark shell), never a component-specific style. Where a state has no separate token (pressed of `button-danger` = `button-danger-hover`; hover and pressed of selection controls), it uses the nearest listed token.
- **Disabled shows its reason** (design-system rule 7). The reason is visible text next to the control (`field-help`, or a line beside the button, for example "Need 5 ratings, have 2"), never a tooltip alone. Disabled uses `text-disabled` on `surface-muted`, which is below 4.5:1 and allowed only by the WCAG 1.4.3 exception for inactive components; the reason text itself uses `text-muted` (5.0:1 or more).
- **Strokes.** The schema has no border-color or multi-value padding property, so outlines are the stroke components `control-border` (3.7:1), `divider`, `focus-ring`, `focus-ring-inverse`. A `padding` token holds the inline value; the block value is stated here.
- **Status is never color alone.** Every tag, alert, matrix gap and error carries an icon or text label in addition to color.
- **Messages answer what / why / what next** (ux-principles 3) for every alert, error, empty state and confirm dialog.

### Shell and navigation

| Component (tokens) | antd | Variants and states | Notes |
|---|---|---|---|
| App content (`app-content`) | `Layout.Content` | one | `page-gutter` padding; page background behind panels |
| Header (`app-header`) | `Layout.Header` | one | `header-height` 56; brand (logo on a light round plate, product name), horizontal navigation menu, user button with `avatar`; text 17.9:1 |
| Header navigation (`nav-item`, `-hover`, `-active`) | `Menu` theme dark, `mode="horizontal"`, inside `Layout.Header` | default / hover / active / focus | the only navigation; there is no sider. Text 8.0:1 default (`text-inverse-muted`), hover brightens the text to `text-inverse` (17.9:1) with no fill, current section is filled with `primary` and white text (7.0:1); items are `control-height-lg`; the fill is the cue of the current section. Focus is `focus-ring-inverse` |
| Header user button (`header-button-hover`, `-pressed`) | `Button type="text"` on the dark header | default / hover / pressed / focus | label and `avatar` in `text-inverse`; hover fill `overlay-inverse-hover` (12% white, 13.0:1), pressed `overlay-inverse-pressed` (20% white, 9.9:1); opens the user menu (`dropdown-item`) |
| Breadcrumb (`breadcrumb`, `-current`) | `Breadcrumb` | link / current | 8.1:1 and 13.4:1 on `surface-page`; current page is plain text, not a link |
| Tabs (`tab`, `-hover`, `-active`) | `Tabs` | default / hover / active / focus / disabled | 8.7:1, 14.6:1, 7.0:1; active adds a 2px `primary` indicator line |
| Steps (`step-current`, `-done`, `-upcoming`) | `Steps` | current / done / upcoming | wave stages (12 statuses): vertical on detail pages, titles always visible, done = check icon, current = filled number; contrast 7.0:1, 6.2:1, 7.5:1 |

### Buttons

| Component (tokens) | antd | Variants and states | Notes |
|---|---|---|---|
| Primary (`button-primary`, `-hover`, `-pressed`) | `Button type="primary"` | default / hover / pressed / focus / disabled | one per page or dialog; label `body-strong` (antd `Button.fontWeight` 700); 7.0 / 8.7 / 9.6:1 |
| Default (`button-default`, `-hover`, `-pressed`) | `Button` | same | white fill, `control-border` outline; hover/pressed use `primary-subtle` with `primary` / `primary-active` text (6.2 / 8.6:1); 14.6:1 at rest |
| Danger (`button-danger`, `-hover`) | `Button danger type="primary"` | same | only for destructive or irreversible actions; label is a verb plus object, not "OK"; 5.6 / 6.7:1 |
| Link (`button-link`, `-hover`) | `Button type="link"` | same | inline and row actions, `control-height-sm` only inside table rows; 7.0 / 8.7:1; target never below `target-min` |
| Disabled (`button-disabled`) | any `Button disabled` | disabled | reason beside it; see Cross-component rules |

Sizes: medium `control-height-md` (default), large `control-height-lg` (page primary action, tablet), small `control-height-sm` (row actions only). antd: `colorErrorActive` = `danger-hover`, `Button.defaultShadow`/`primaryShadow`/`dangerShadow` = none, `Button.defaultHoverBg`/`defaultActiveBg` = `primary-subtle`, `defaultHoverColor` = `primary`, `defaultActiveColor` = `primary-active`, `defaultHoverBorderColor` = `primary`. A button that starts an async action shows a loading state, stays the same size and is not clickable twice.

### Form fields

| Component (tokens) | antd | Variants and states | Notes |
|---|---|---|---|
| Field (`input`, `input-placeholder`, `input-disabled`) | `Input`, `InputNumber`, `Select`, `DatePicker`, `Input.TextArea` | default / hover (`primary-hover` border) / focus (`primary` border + ring) / error / disabled | `control-height-md`, `control-border` outline; 14.6:1 text, 5.8:1 placeholder (antd derives placeholder at 25% alpha: set `colorTextPlaceholder` = `text-muted`) |
| Label, help, error (`field-label`, `field-help`, `field-error`) | `Form.Item` | label above field (vertical layout) | label `body-strong` 8.7:1; help `body-sm` 5.8:1; error `body-sm` 5.6:1 with an error icon and the invalid border in `danger`; the error says what is wrong and how to fix it |
| Selection control (`selection-control-checked`) | `Checkbox`, `Radio`, `Switch` | unchecked (`control-border`) / checked / focus / disabled | 16px box, `rounded-xs`; label always clickable; checked mark 7.0:1 |

Required fields are marked with text ("required"), not only an asterisk color.
### Table, pagination

| Component (tokens) | antd | Variants and states | Notes |
|---|---|---|---|
| Table (`table-header`, `table-row`, `-hover`, `-selected`, `table-cell-numeric`) | `Table` | default / hover / selected / loading (skeleton) / empty | header `body-strong`, 7.5:1; rows 14.6:1, hover 13.4:1, selected 12.9:1; cell padding block 12 (`space-sm`), inline 16 (`space-md`); numeric columns `data-numeric`, right-aligned; header and first column may be sticky; row separators `divider` |
| Row actions | `Button type="link"`, `Dropdown` | at most two visible links; more go into a `Dropdown` | link-style actions at `control-height-sm` |
| Pagination (`pagination-item`, `-hover`, `-active`) | `Pagination` | default / hover / active / focus / disabled | 14.6:1, 6.2:1, 7.0:1; active page = `primary` border and text; page size selector in `Select` |

antd Table component tokens: `headerBg` = `surface-muted`, `headerColor` = `text-secondary`, `rowHoverBg` = `surface-page`, `rowSelectedBg` = `rowSelectedHoverBg` = `primary-subtle`, `borderColor` = `border-subtle`, `cellPaddingBlock` 12, `cellPaddingInline` 16.

### Competency matrix

| Component (tokens) | antd | Variants and states | Notes |
|---|---|---|---|
| Matrix header (`matrix-header`) | `Table` column title, sticky header | default / sorted | `data-column-header` (Narrow, bold), wraps to at most three lines; 12.6:1; sticky edge uses `elevation-sticky` while scrolled |
| Name cell (`matrix-name-cell`) | `Table` fixed column | default / focus | sticky first column, 280px, wraps to two lines before truncating; truncation exposes the full name; shows the critical-competency marker (icon + label) next to the name; 14.6:1 |
| Cell (`matrix-cell`) | `Table` cell (custom render) | default / hover / focus / selected / disabled (not rated, reason shown) | 72x48 minimum, whole area clickable when editable; hosts one rating chip, centered |
| Rating chip (`rating-chip-0` ... `-4`) | custom (`Tag`-like span) | level 0-4, plus empty ("not rated") | 32x32, `rounded-md`, the digit is always shown in `body-strong`; text 15.5, 11.8, 8.0 (on `surface-inverse` text), 7.0, 17.9:1 (white text); meaning of a level shown in a tooltip and the legend |

Comparison to the target level is shown next to the chip by a status tag or icon (`tag-success` met, `tag-warning` below target, `tag-danger` critical gap), never by recoloring the chip. A matrix always has a legend of levels 0-4 (digit, color, meaning).
### Candidate card and status tags

| Component (tokens) | antd | Variants and states | Notes |
|---|---|---|---|
| Candidate card (`candidate-card`, `-selected`, `-meta`) | `Card` | default / hover (border only) / selected / focus | candidate name `heading-2`, meta `body-md` in `text-secondary` (8.7:1), content grouped by `Descriptions`; 24px padding; sections: summary, ratings (matrix excerpt), evidence, decision |
| Status tag (`tag-success`, `-warning`, `-danger`, `-info`, `-neutral`) | `Tag` | one per state | text + icon + color: contrast 4.7, 4.7, 4.9, 5.0, 7.5:1; `body-sm`, `rounded-xs`; mapping of wave and application statuses: success = approved / ready / feedback delivered; info = in progress (review, evidence collection, commission, ranking); warning = evidence requested, below target; danger = rejected, cancelled, critical gap; neutral = draft, closed, archived |

Tags are labels, not buttons. The text of a tag always names the state in words.

### Overlays and feedback

| Component (tokens) | antd | Variants and states | Notes |
|---|---|---|---|
| Modal (`modal`, `scrim`) | `Modal` | default / confirm / danger confirm | title `heading-2`, 24px padding; scrim is `overlay-scrim` |
| Confirm dialog (destructive) | `Modal.confirm` via `App.useApp()` | danger | title states the action and object, body states the consequence and whether it can be undone, primary button is `button-danger` with a verb label, the safe action is `button-default` (Cancel) |
| Drawer (`drawer`, `scrim`) | `Drawer` | right-side, 480px to 720px wide | record details, filters, evidence preview; title `heading-3` |
| Dropdown item (`dropdown-item`, `-hover`, `-selected`) | `Dropdown`, `Select` popup, `DatePicker` popup | default / hover / selected / disabled | 14.6:1 and 12.9:1; selected also shows a check icon; popup uses `elevation-popup` |
| Tooltip (`tooltip`) | `Tooltip` | hover | 17.9:1; short text only; never the only carrier of a label or disabled reason; antd `colorBgSpotlight` = `surface-inverse` (default is a translucent black) |
| Toast (`toast`) | `App.useApp().message` / `notification` | success / info / warning / error (icon colored by status) | `elevation-modal`, text 14.6:1 |
| Alert (`alert-info`, `-success`, `-warning`, `-danger`) | `Alert` | four levels, optional action and close | text `text-primary` on tinted bg (12.7 to 13.3:1), icon in the status color (at least 4.7:1 on its tint); `danger` is reserved for blocking or irreversible cases |
| Empty state (`empty-state`) | `Empty` | no data / no result / no access | title `heading-3`, then reason, then next step with a `button-primary` or link; 8.7:1; simple icon, no illustration |
| Skeleton, spinner (`skeleton`) | `Skeleton`, `Spin` | loading | skeleton replaces content shape in tables, cards and the matrix; shimmer off under reduced motion; spinner uses `primary` |
| Avatar (`avatar`) | `Avatar` | initials only | `primary` on `primary-subtle`, 6.2:1; no photos required; always next to the name as text |

### Token consumption

| Token | Consumed by |
|---|---|
| `primary` | button-primary, link, focus-ring, nav-item-active, tab-active, pagination-item-active, selection-control-checked, step-current, rating-chip-3 |
| `primary-hover`, `primary-active` | button-primary-hover/-pressed, button-default-pressed, button-link-hover |
| `primary-subtle` | button-default-hover, table-row-selected, candidate-card-selected, dropdown-item-selected, pagination-item-hover, step-done, avatar |
| `accent` | focus-ring-inverse only (never text) |
| `overlay-scrim` | scrim (modal, drawer) |
| `overlay-inverse-hover`, `overlay-inverse-pressed` | header-button-hover, header-button-pressed |
| `border-control` | control-border |
| `border-subtle` | divider |
| `text-disabled` | button-disabled, input-disabled |
| `surface-inverse` | app-header, nav-item, nav-item-hover, tooltip, text on rating-chip-0..2 |
| `surface-muted` | table-header, matrix-header, tag-neutral, step-upcoming, skeleton, disabled controls |
| `surface-page` | app-content, breadcrumb, table-row-hover, dropdown-item-hover |
| `rating-level-0..4` | rating-chip-0..4 |
| `success`, `warning`, `danger`, `info` (+ `-bg`) | tag-*, alert-* (bg), field-error and button-danger (`danger`) |

### antd 6 mapping (components)

Tabs `itemColor` = `text-secondary`, `itemHoverColor` = `text-primary`, `itemSelectedColor`/`inkBarColor` = `primary`. Modal `titleFontSize` 24, `titleLineHeight` 1.3333; Drawer title 20. Alert `defaultPadding` 12. Avatar background/color tokens (`avatarBg`, `avatarColor`) = `primary-subtle` / `primary`. Steps icon colors follow `colorPrimary` and the `primary-subtle` finish tint. Names of component-level tokens are verified in the implementation spike against antd 6.6.5; where a token is missing, the same value is applied through a `classNames` / `styles` override, not through a DOM-selector hack.

## Do's and Don'ts

Each rule restates a decision made in the sections above; none is new.

### Do

- Build UI from tokens only (`ConfigProvider` driven from this file); reference component tokens first, semantic colors second.
- Use `primary` for the one main action of a page or dialog; use `button-danger` only for destructive or irreversible actions, with a verb plus object label.
- Pair every status color with an icon or a text label, and show the digit in every rating chip (WCAG 1.4.1).
- Show the reason next to anything disabled, as visible text (for example "Need 5 ratings, have 2").
- Use `accent` only as a fill with `surface-inverse` or `text-primary` text on it, or as a mark on `surface-inverse`.
- Set every antd text token explicitly (secondary, tertiary, quaternary, placeholder) and `colorPrimaryBorder` = `primary`.
- Keep weights to 400 and 700; use `data-numeric` and right alignment for compared numbers.
- Keep the 4px grid and the default (comfortable) antd size; use `control-height-lg` at tablet widths.
- Keep the focus ring (`focus-ring`, `focus-ring-inverse` on the dark shell).
- Mark the current state in words, not only by color: tags, steps, active tab; the current header menu item is a filled item.
- Let critical alerts stay rare: `danger` for blocking or irreversible events only.
- Ship PT fonts inside the app image as self-hosted files with their license text.

### Don't

- Don't use raw hex, px or font-family in UI code or mockups; don't add a token that duplicates an existing one.
- Don't put `accent` (1.9:1) or white text on `accent`, and don't use `accent` for warning or any status.
- Don't use weights 500 or 600, italics, uppercase transforms or letter-spacing on Cyrillic labels, or `compactAlgorithm`.
- Don't rely on color alone, on a tooltip alone, or on a shadow alone to carry meaning or a boundary.
- Don't show a disabled control without a reason, and don't remove the focus ring without an equal replacement.
- Don't add shadows to static panels, tables or cards, and don't raise cards on hover.
- Don't animate data (matrix cells, ratings, rankings), use transitions longer than 300ms, bounce easing, click ripple or looping decoration; honor `prefers-reduced-motion`.
- Don't recolor a rating chip to show the target-level comparison; use a status tag or icon beside it.
- Don't load fonts, icons or scripts from a CDN or any external host; don't use `createFromIconfontCN`.
- Don't author a dark theme from these tokens, and don't let any theme change status colors, hit targets or spacing.
- Don't truncate a competency name without exposing the full text, or hide the critical-competency marker.

### Lint exclusions

Warnings accepted by the user, with rationale (design-system rule 11). `designmd-lint` has no exclusion mechanism; this block is the record.

| Rule | Path | Decision | Rationale |
|---|---|---|---|
| `contrast-ratio` | `components.button-disabled` | excluded (approved by the user) | `text-disabled` on `surface-muted` is 2.04:1. WCAG 1.4.3 exempts text of inactive user-interface components. The reason a control is disabled is always shown as adjacent text (`text-muted`, at least 5.0:1), never by this text color alone. |
| `contrast-ratio` | `components.input-disabled` | excluded (approved by the user) | Same exception and same rule: the reason is shown as `field-help` text next to the field. |
| `contrast-ratio` | `components.header-button-hover` | excluded (approved by the user) | The linter ignores the alpha channel of `overlay-inverse-hover` (reports 1.00:1). Real contrast of `text-inverse` on the composite over `surface-inverse` is 13.0:1. |
| `contrast-ratio` | `components.header-button-pressed` | excluded (approved by the user) | The linter ignores the alpha channel of `overlay-inverse-pressed` (reports 1.00:1). Real contrast of `text-inverse` on the composite over `surface-inverse` is 9.9:1. |
