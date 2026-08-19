# CyberPulse — setup on a new machine

Two apps. **The Portal does not need a database** — it reads JSON files and lists uploaded
files straight from disk. Only the admin app talks to SQL Server. So pick the path you need:

| I want to… | I need |
|---|---|
| Just view the employee Portal | Node.js. Nothing else. |
| Create/manage content too | Node.js **and** Visual Studio + SQL Server |

The two folders **must stay side by side**. The Portal resolves
`../CyberPulseAdministration/Uploads` and `../CyberPulseAdministration/JsonOutput`
relative to itself. Move either one and the Portal goes blank.

```
CyberPulse/
├── CyberPulseAdministration/
└── CyberPulsePortal/
```

---

## Path A — Portal only (5 minutes, no database)

Prerequisite: Node.js (any recent LTS) from https://nodejs.org

```bash
cd CyberPulsePortal
npm install
npm start
```

Open <http://localhost:8001>.

If the zip included `CyberPulseAdministration/Uploads/` and `JsonOutput/`, the site is fully
populated — announcements, news, HR documents, photos/videos, quality docs, health tip.
If those folders are absent, the Portal still runs but every section shows empty. That is
expected, not a broken setup.

---

## Path B — Admin app as well

### Prerequisites

- **Visual Studio 2019 or 2022** with the *ASP.NET and web development* workload
  (this is a .NET Framework 4.7.2 project — `dotnet run` will not work on it)
- **.NET Framework 4.7.2 Developer Pack** (Visual Studio Installer → Individual components)
- **SQL Server** — Express edition is fine: https://www.microsoft.com/sql-server/sql-server-downloads

### Steps

1. Open `CyberPulseAdministration/CyberPulseAdministration.sln` in Visual Studio.

2. **Restore NuGet packages.** The `packages/` folder is not in the zip. Right-click the
   solution → *Restore NuGet Packages*, then Build. If the build complains that packages are
   missing, restore again and reload the solution — the project imports a `.props` file from
   `packages/` and needs it present before it will build.

3. **Check the connection string** in `CyberPulseAdministration/Web.config`:

   ```xml
   <add name="CyberPulseDb"
        connectionString="Server=localhost\SQLEXPRESS;Database=CyberPulse;Integrated Security=True;" />
   ```

   Change `localhost\SQLEXPRESS` if your SQL Server instance is named differently
   (a default instance is just `localhost`). Everything else can stay.

4. **Create the database.** In SSMS or `sqlcmd`, run once:

   ```sql
   CREATE DATABASE CyberPulse;
   ```

   You do **not** need to create any tables. `DatabaseInitializer.InitializeDatabase()` runs
   on application start and creates every table and stored procedure it needs.

5. **Run** (F5 / IIS Express). Log in with:

   ```
   username: admin
   password: admin
   ```

   That account is seeded automatically on first run. **Change it before this is used for
   anything real** — passwords are currently stored in plain text.

6. Start the Portal as in Path A, in a second terminal.

### How content reaches the Portal

Editing an announcement does **not** change what employees see. The admin app writes a
snapshot only when you press **Generate JSON**:

1. Create/edit announcements in the admin grid
2. Tick the checkboxes for the ones that should be live
3. Press **Generate JSON**

Uploaded files (HR documents, quality docs, health tip, news images) appear on the Portal
immediately — they are read from disk, no publish step.

---

## Troubleshooting

**Portal starts but every section is empty.** Either the folders are missing, or the two app
folders are not siblings. Confirm `CyberPulseAdministration/JsonOutput/*.json` exists next to
`CyberPulsePortal/`.

**Port 8001 already in use.** `set PORT=8080 && npm start` (cmd) or
`$env:PORT=8080; npm start` (PowerShell).

**Admin app: "cannot open database CyberPulse".** You skipped step 4, or the instance name in
`Web.config` is wrong.

**Admin app builds but pages 500 on load.** Usually the connection string. The initializer
swallows its errors and only writes to Debug output, so the app starts fine and fails later.

**A note on the announcements table.** On a fresh database the initializer creates a table
named `Announcement` (singular) and matching stored procedures — self-consistent, works fine.
If you instead restore a database copied from an older CyberPulse install, its procedures may
point at `Announcements` (plural) and the singular table will sit there empty. If announcements
appear missing in SQL but fine in the app, check which of the two tables actually holds rows.
