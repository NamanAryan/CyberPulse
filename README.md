# CyberPulse

CyberPulse is an internal employee portal made up of two cooperating applications:

- **CyberPulseAdministration** — an ASP.NET MVC (.NET Framework 4.7.2) back-office app where staff log in to create and manage announcements, news articles, HR documents, and quality documentation.
- **CyberPulsePortal** — a lightweight Node.js/Express front-end that employees browse to read that content. It has no database or login of its own; it reads whatever the Administration app has published.

The two apps are connected only through the filesystem, not a shared API or database connection: the Administration app writes JSON exports and uploaded files to shared folders on disk, and the Portal serves them.

```
CyberPulse/
├── CyberPulseAdministration/   # ASP.NET MVC admin app (.NET Framework 4.7.2)
│   ├── Controllers/            # Announcement, HrAnnouncement, QualityAnnouncement, NewsArticle, Hr, HealthTip, Account, Home
│   ├── DataAccess/             # SQL Server repositories (ADO.NET, stored procedures)
│   ├── Models/
│   ├── Views/
│   ├── JsonOutput/             # Generated JSON feeds consumed by the Portal (gitignored)
│   └── Uploads/                # Uploaded HR files, quality docs, health tips, news images (gitignored)
│
├── CyberPulsePortal/           # Node.js/Express read-only front-end
│   ├── server.js               # Static file server + JSON/file-listing API
│   └── public/                 # HTML/CSS/JS served to employees
│
└── Photo_Video/                # Local staging folder for source photo/video assets (gitignored)
```

## How it fits together

1. An admin logs into **CyberPulseAdministration** (Forms Authentication, credentials validated against a `usp_ValidateUser` stored procedure in SQL Server) and creates/edits announcements, news articles, or uploads HR/quality files.
2. On each change, the relevant controller (`AnnouncementController`, `HrAnnouncementController`, `QualityAnnouncementController`, `NewsArticleController`) re-serializes the active records to JSON and writes them to `CyberPulseAdministration/JsonOutput/`. Uploaded files (photos, videos, PDFs, Office docs) are saved under `CyberPulseAdministration/Uploads/`, organized by feature and file type, with each stored file prefixed with a GUID to avoid name collisions.
3. **CyberPulsePortal**'s Express server (`server.js`) reads those same `JsonOutput/` and `Uploads/` directories directly off disk (`CyberPulseAdministration` is a sibling folder one level up from `CyberPulsePortal`) and exposes them over a small read-only JSON API, plus serves the uploaded files as static assets under `/Uploads/*`.
4. The Portal's static pages (`public/*.html` + `public/js/*.js`) call that API and render announcements, a photo/video gallery, HR documents, quality documentation, news articles, and a "Health Tip" widget.

## CyberPulseAdministration (admin app)

- **Stack**: ASP.NET MVC 5, .NET Framework 4.7.2, ADO.NET + stored procedures against SQL Server (`Web.config` connection string `CyberPulseDb`, default `Server=localhost\SQLEXPRESS`).
- **Auth**: Forms Authentication; login/logout handled by `AccountController`.
- **Controllers**:
  - `AnnouncementController` — general company announcements
  - `HrAnnouncementController` — HR-specific announcements
  - `QualityAnnouncementController` — Quality-team announcements
  - `NewsArticleController` — news articles with an associated image
  - `HrController` — HR document/photo/video uploads (`Uploads/HrPortal/{pdf,word,excel,txt,photo,video}`)
  - `QualityController` — quality manuals/procedures/templates/checklists (`Uploads/QualityInside/...`, nested by category)
  - `HealthTipController` — a single rotating "Health Tip" image or text file (`Uploads/HealthTip/`)
  - `HomeController` — the authenticated dashboard/landing page
- **DataAccess**: one repository per feature (`AnnouncementRepository`, `HrAnnouncementRepository`, `HrFileRepository`, `NewsArticleRepository`, `QualityAnnouncementRepository`) plus `DatabaseInitializer`, which creates required tables/stored procedures on startup if they don't already exist.

## CyberPulsePortal (employee-facing site)

- **Stack**: Node.js + Express (`express` is the only runtime dependency). No build step, no database — `server.js` serves static files and a handful of read-only JSON endpoints.
- **Run it**:
  ```bash
  cd CyberPulsePortal
  npm install
  npm start          # http://localhost:8001 (or $PORT)
  ```
- **API endpoints** (all read-only, backed by files under `CyberPulseAdministration/`):

  | Endpoint | Source | Description |
  |---|---|---|
  | `GET /api/announcements` | `JsonOutput/announcements.json` | Company-wide announcements |
  | `GET /api/hrannouncements` | `JsonOutput/hrannouncements.json` | HR announcements |
  | `GET /api/qualityannouncements` | `JsonOutput/qualityannouncements.json` | Quality announcements |
  | `GET /api/newsarticles` | `JsonOutput/newsarticles.json` | News articles (with `ImagePath` under `Uploads/NewsArticles/`) |
  | `GET /api/hrfiles` | `Uploads/HrPortal/*` | All HR documents/photos/videos, listed from disk, newest first |
  | `GET /api/qualityfiles?tab=<Category>` | `Uploads/QualityInside/<Category>` | Files for a quality category, walked recursively |
  | `GET /api/healthtip` | `Uploads/HealthTip/` | The most recently modified Health Tip image/text |
  | `GET /Uploads/*` | `Uploads/` (static) | Direct file downloads/streaming for everything above |

- **Pages** (`public/`): `index.html` (dashboard: announcements, photo/video gallery, quality & HR summaries), `all.html` / `all-documents.html` (full document/media listings), `all-news.html`, `quality.html` and per-category quality pages, `details.html` (single announcement view).

## Data & media folders (not tracked in git)

`CyberPulseAdministration/Uploads/`, `CyberPulseAdministration/JsonOutput/`, and `Photo_Video/` are runtime/user-generated data — uploaded documents, photos, videos, and generated JSON feeds — and are excluded via `.gitignore` to keep the repository small. They must exist on disk (created automatically by the Administration app as content is added) for the Portal to have anything to display.

## Prerequisites

- **CyberPulseAdministration**: Windows, .NET Framework 4.7.2, IIS Express or IIS, SQL Server (Express is fine) with a `CyberPulse` database reachable via the connection string in `Web.config`.
- **CyberPulsePortal**: Node.js (any recent LTS) and npm.

Both apps are meant to be checked out as sibling folders (as they are in this repo) so the Portal's relative path resolution to `../CyberPulseAdministration/Uploads` and `../CyberPulseAdministration/JsonOutput` works out of the box.
