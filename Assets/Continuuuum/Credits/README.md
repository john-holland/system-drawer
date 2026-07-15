# Continuuuum Credits

End-credits lists stored in `continuuuum.db`, edited on the web, played in Unity via a screen-space quad tree.

## Web

Open `/credits` on the Drawer API (port 5050).

- Sidebar defaults to **New** (blank title / id); selecting a list fills title, id, and project id and switches **Create** → **Save**
- Bind an episode/project id on the list meta form
- **Create entry** types: Manual, From work orders, From HR employees
- **Update list** — refresh from work-order assignees (`work_orders`), Resaurce HR employees, or both
- Edit show full name / nickname, quote, company, rights, years, scroll speeds, source kind
- **Speed preview** scrolls only visible entries (hidden when both show flags are off)
- SQL Viewer recipes: `credits_list_entries`, `credits_warehouse_history`, `credits_hidden_entries`

## API

| Method | Path |
|--------|------|
| GET/POST | `/api/credits/lists` |
| GET/PATCH/DELETE | `/api/credits/lists/<id>` |
| POST | `/api/credits/lists/<id>/update-list` |
| GET | `/api/credits/lists/<id>/history` |

Playback GETs omit entries where `showFullName` and `showNickname` are both false (`?includeHidden=1` for the editor).

## Unity

Add `CreditsQuadTreeUI` + `CreditsApiClient` to a Canvas. Set `listId`. Sections map to `quadrantPath` leaves; `isSpecialUi` sections use `CreditsSpecialUiLeaf` (no scroll). Entry views skip non-visible rows.

## Warehouse

`credits_warehouse_history` is append-only for guild review of update-list and visibility changes.
