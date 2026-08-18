# CyberPulse

CyberPulse is an internal employee portal built as two cooperating applications:

| | **CyberPulseAdministration** | **CyberPulsePortal** |
|---|---|---|
| Who uses it | Staff/admins, behind a login | All employees, no login |
| What it does | Create and manage all content | Read and download that content |
| Stack | ASP.NET MVC 5, .NET Framework 4.7.2, SQL Server | Node.js + Express, static HTML/CSS/JS |
| Writes | SQL Server, `JsonOutput/`, `Uploads/` | Nothing — it is read-only |

The two apps share **no API and no database connection**. The admin app writes JSON feeds and uploaded files to folders on disk; the Portal reads those same folders as a sibling directory and serves them. That is the entire integration contract:

```
CyberPulseAdministration/                    CyberPulsePortal/
  ├── JsonOutput/*.json      ──────────────►   server.js  ──►  /api/*      ──►  public/*.html
  └── Uploads/**             ──────────────►   server.js  ──►  /Uploads/*
```

## Repository layout

```
CyberPulse/
├── CyberPulseAdministration/   # ASP.NET MVC admin app (.NET Framework 4.7.2)
│   ├── Controllers/            # Account, Home, Announcement, HrAnnouncement, QualityAnnouncement,
│   │                           #   NewsArticle, Hr, Quality, HealthTip
│   ├── DataAccess/             # ADO.NET repositories + DatabaseInitializer
│   ├── Models/                 # Announcement, HrAnnouncement, QualityAnnouncement, NewsArticle,
│   │                           #   HrFile, LoginViewModel
│   ├── Views/                  # Razor views, shared _Layout.cshtml
│   ├── Content/site.css        # Admin theme
│   ├── JsonOutput/             # Generated JSON feeds consumed by the Portal (gitignored)
│   └── Uploads/                # Uploaded HR files, quality docs, health tips, news images (gitignored)
│
├── CyberPulsePortal/           # Node.js/Express read-only front-end
│   ├── server.js               # Static file server + JSON/file-listing API
│   └── public/                 # HTML pages, css/style.css, js/ controllers
│
└── Photo_Video/                # Local staging folder for source photo/video assets (gitignored)
```

---

## CyberPulseAdministration (admin app)

**Auth.** Forms Authentication. `AccountController` validates credentials against the `usp_ValidateUser` stored procedure. Every other controller is `[Authorize]`. `DatabaseInitializer` seeds a default `admin` / `admin` account on first run — change it before any real deployment.

**Startup.** `Global.asax.cs` calls `DatabaseInitializer.InitializeDatabase()` on `Application_Start`, which creates every table and stored procedure it needs if they don't already exist. There is no migration tool and no manual SQL script to run — booting the app provisions the schema.

### Features

**Announcements (three separate feeds).** `AnnouncementController` (company-wide), `HrAnnouncementController` (HR) and `QualityAnnouncementController` (Quality) are three parallel implementations of the same idea. Each announcement has a title, date, short description, a rich `Description` body, a page title and an `IsActive` flag.

The publish workflow is deliberate rather than automatic:

1. Create/edit announcements in the admin grid.
2. Tick the checkboxes for the ones that should be live (`BulkToggleActive`).
3. Press **Generate JSON** — only the active records are serialized to `JsonOutput/<feed>.json`.

Until step 3 runs, the Portal keeps showing the previous snapshot. That makes publishing an explicit act.

**News & Articles.** `NewsArticleController` manages articles with a type, description, external URL and an uploaded image (stored in `Uploads/NewsArticles/`). Same active-toggle + Generate JSON publishing flow, written to `newsarticles.json`.

**HR Portal.** `HrController` handles employee document, photo and video uploads. Files are auto-categorized by extension into `Uploads/HrPortal/{pdf,word,excel,txt,photo,video}/`, and a matching row is written to the `HrFiles` table — HR files are the only uploads tracked in the database. Limits: 5MB per file, 25MB for video. Unsupported extensions are rejected per file, and the response reports how many succeeded alongside each failure.

**Quality Insights.** `QualityController` — see [its own section](#quality-insights) below.

**Health Tip.** `HealthTipController` maintains a single rotating tip in `Uploads/HealthTip/`. Uploading a new one deletes everything already in the folder, so exactly one tip exists at a time. Accepts images (`.jpg/.jpeg/.png/.gif/.webp`) and documents (`.doc/.docx/.txt`); Excel and other formats are blocked. For `.docx`, the text is extracted by opening the file as a zip and reading `word/document.xml`, so the Portal can render the tip as text rather than a download link.

### Database

`DatabaseInitializer` creates six tables — `Announcement`, `HrAnnouncement`, `QualityAnnouncement`, `NewsArticle`, `HrFiles`, `Users` — and the 33 `usp_*` stored procedures behind them (get / get-by-id / get-active / insert / update / delete / toggle-active per feature, plus `usp_ValidateUser`).

Data access is plain ADO.NET: one repository per feature in `DataAccess/`, each calling stored procedures directly. No ORM.

Note that **quality documents are not in the database at all** — they are listed straight from disk by directory walk. Only HR files carry DB rows.

---

## Quality Insights

The most involved feature, and the one with its own conventions. It lives in a single view (`Views/Quality/Index.cshtml`) with three main tabs:

| Tab | Route | Contents |
|---|---|---|
| **Documents** | `/Quality/Documents` | Four sub-tabs of quality documentation |
| **Announcements** | `/Quality/Announcements` | The Quality announcement grid (`QualityAnnouncementController`) |
| **Quality Certificate** | `/Quality/Certificate` | Certificates, PDF/image only |

Upload, file grid and delete are shared across every category. There is no edit/update — a document is replaced by deleting and re-uploading.

**Storage layout.** Every category folder nests under a single `QualityDocuments` parent. `QualityController.GetUploadPath()` is the only place that builds this path, so upload, listing, delete and download all follow it automatically:

```
Uploads/QualityInside/QualityDocuments/
├── QualityManual/
├── QualitySystemProcedure/
├── QualityStandardGuidelines/{QualityStandard,Guidelines}/
├── TemplateFormChecklist/{Template,Form,Checklist}/
└── QualityCertificate/
```

**File rules.** 5MB cap everywhere. `QualityCertificate` additionally accepts only `.pdf`, `.jpg`, `.jpeg` and `.png`, enforced twice: client-side by the file input's `accept` filter plus a pre-submit check, and server-side by an extension check that fails the whole batch with a message naming the rejected files.

**On the Portal**, `quality.html` shows the announcements carousel on the left, and on the right the Quality Core pyramid — whose four tiers link to the per-category pages — with the Quality Certificate card stacked beneath it. Certificates are listed inline on that card so they're readable without leaving the page; the card's arrow opens the full `quality/quality-certificate.html` listing.

---

## CyberPulsePortal (employee-facing site)

Express is the only runtime dependency. No build step, no database, no login.

```bash
cd CyberPulsePortal
npm install
npm start          # http://localhost:8001 (or $PORT)
```

### API endpoints

All read-only, all backed by files under `CyberPulseAdministration/`.

| Endpoint | Source | Description |
|---|---|---|
| `GET /api/announcements` | `JsonOutput/announcements.json` | Company-wide announcements |
| `GET /api/hrannouncements` | `JsonOutput/hrannouncements.json` | HR announcements |
| `GET /api/qualityannouncements` | `JsonOutput/qualityannouncements.json` | Quality announcements |
| `GET /api/newsarticles` | `JsonOutput/newsarticles.json` | News articles (with `ImagePath` under `Uploads/NewsArticles/`) |
| `GET /api/hrfiles` | `Uploads/HrPortal/*` | All HR documents/photos/videos, listed from disk, newest first |
| `GET /api/qualityfiles?tab=<Category>` | `Uploads/QualityInside/QualityDocuments/<Category>` | Files for a quality category, walked recursively |
| `GET /api/healthtip` | `Uploads/HealthTip/` | The most recently modified Health Tip image/text |
| `GET /Uploads/*` | `Uploads/` (static) | Direct file downloads/streaming for everything above |

Missing folders return an empty list rather than an error, so the Portal runs fine against a fresh checkout with nothing uploaded yet.

### Pages

| Page | What it shows |
|---|---|
| `index.html` | Dashboard: announcements carousel, photo/video gallery with a media viewer overlay, HR documents, news, Health Tip |
| `hr-connect.html` | HR announcements carousel + HR document list |
| `quality.html` | Quality announcements, Quality Core pyramid, Quality Certificate list |
| `quality/quality-*.html` | One page per quality category: manual, system procedure, standard & guidelines, template/form/checklist, certificate |
| `all.html` | Full paginated announcement listing |
| `all-documents.html` | Full HR file listing, split into Documents / Media tabs with an inline viewer |
| `all-news.html` | Full news & articles listing |
| `details.html` | A single announcement; the rich `Description` body renders inside an iframe |

`all.html` and `details.html` are each reused for all three announcement feeds via a `?source=` parameter (`quality`, `hr`, or omitted for company-wide), which swaps the endpoint, heading and back-link. `server.js` also mounts `public/` a second time under `/quality`, so those shared pages resolve when reached from the Quality section without duplicating any files.

### Shared shell

`js/portal-shell.js` renders the top navigation and page banner for every page from one definition — pages just drop in mount points:

```html
<nav data-portal-nav data-active="quality"></nav>
<div data-portal-banner data-title="Quality Insights" data-tagline="Excellence Through Quality Insights"></div>
```

It also measures the sticky header into a `--portal-header-height` CSS variable so the nav tabs stay docked directly beneath it while scrolling, at whatever height the header renders.

---

## Conventions & gotchas

- **GUID-prefixed filenames.** Every uploaded file is stored as `<guid>_<original filename>` to avoid collisions. Both apps strip the 36-character prefix for display and for download filenames — if you add a new listing surface, strip it there too.
- **Sibling folders are required.** The Portal resolves `../CyberPulseAdministration/Uploads` and `../CyberPulseAdministration/JsonOutput` relative to itself. Move either app and the Portal goes blank.
- **`Uploads/`, `JsonOutput/` and `Photo_Video/` are gitignored.** They're runtime data. A fresh clone has no content until the admin app creates it; that's expected, not a broken setup.
- **Publishing is explicit.** Editing an announcement does not change what employees see. Generate JSON does.
- **`server.js` sets `NODE_TLS_REJECT_UNAUTHORIZED = '0'`** for local server-to-server calls against self-signed dev certificates. Revisit before deploying anywhere real.
- **Cache-busting is manual.** Portal assets are referenced with `?v=N` (e.g. `style.css?v=5`). Bump the number across `public/**/*.html` when you change a shared asset.

## Prerequisites

- **CyberPulseAdministration**: Windows, .NET Framework 4.7.2, IIS Express or IIS, SQL Server (Express is fine) reachable via the `CyberPulseDb` connection string in `Web.config` (defaults to `Server=localhost\SQLEXPRESS`). Tables and stored procedures are created on first run.
- **CyberPulsePortal**: Node.js (any recent LTS) and npm.

Run the admin app first so the database and content folders exist, then start the Portal.
