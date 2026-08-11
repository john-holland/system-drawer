# Shared cron humanizer

- JS: [`cron-humanize.js`](cron-humanize.js) — global `CronHumanize` (alias `PayrollCronHumanize`)
- CSS: [`cron-humanize.css`](cron-humanize.css) — `.cron-human` / `.cron-human-inline`
- Unity: `Assets/locomotion/pathing/persona/CronHumanize.cs` + `[CronExpr]` + Editor `CronExprDrawer`

Use `CronHumanize.bindInput(input, labelEl)` under cron fields, or `CronHumanize.describe(expr)` in lists.
