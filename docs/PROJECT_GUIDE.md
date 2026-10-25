# Movies Center — Project Guide

> **Single source of truth.** Updated whenever a decision is made or a feature is completed.
> Last updated: 2026-10-24

---

## Table of Contents

1. [Overview](#1-overview)
2. [Stack & Versions](#2-stack--versions)
3. [Project Structure](#3-project-structure)
4. [Features: Guest vs Login](#4-features-guest-vs-login)
5. [Architecture & Data Flow](#5-architecture--data-flow)
6. [Database Schema](#6-database-schema)
7. [API Surface](#7-api-surface)
8. [Security](#8-security)
9. [Development Process](#9-development-process)
10. [Build Order & Phase Plan](#10-build-order--phase-plan)
11. [Coding Rules](#11-coding-rules)
12. [Design Rules](#12-design-rules)
13. [Testing Checklist](#13-testing-checklist)
14. [Install, Run, Build & Test](#14-install-run-build--test)
15. [Environment Variables](#15-environment-variables)
16. [Decision Log](#16-decision-log)
17. [Open Questions](#17-open-questions)

---

## 1. Overview

**Movies Center** is a professional streaming-catalog website. Users explore movies and series (metadata from TMDB), watch videos from third-party embeds or admin-uploaded assets, manage watchlists, rate titles, and receive notifications. The site works for guests; login unlocks syncing, ratings, and notifications. A hidden admin panel manages content, sources, and analytics.

The name "Movies Center" is stored in one config constant per app so it can be changed globally.

---

## 2. Stack & Versions

| Layer | Technology | Version |
|---|---|---|
| **Frontend framework** | Angular (SSR, standalone components, signals) | 22.x |
| **TypeScript** | TypeScript | 6.0.x |
| **Node.js** | Node.js LTS | 22.x (^22.22.0) |
| **UI components** | Angular Material / CDK (fully restyled) | 22.x |
| **Icons** | Lucide Angular | latest |
| **Fonts** | Inter (UI) + Oswald (logo/hero titles) | Google Fonts |
| **Backend framework** | ASP.NET Core | 10.0 LTS |
| **ORM** | Entity Framework Core | 10.0.x |
| **Database** | SQL Server 2022 Developer (Docker) | 2022-latest |
| **Cache / Rate limit store** | Redis | 7.x (Docker) |
| **Background jobs** | Hangfire (SQL Server storage) | latest stable |
| **Auth** | ASP.NET Core Identity + JWT + refresh tokens | built-in |
| **Validation** | FluentValidation | latest stable |
| **CQRS** | MediatR | latest stable |
| **Resilience** | Polly | latest stable |
| **API client gen** | ng-openapi-gen | latest stable |
| **Email** | Postmark | API |
| **Web push** | Standard Web Push with VAPID keys | built-in |
| **Analytics** | PostHog | cloud or self-hosted |
| **Bot protection** | hCaptcha | free tier |
| **Video uploads** | Bunny Stream | API |
| **CI** | GitHub Actions | — |
| **Containerization** | Docker + Docker Compose (local dev) | — |

---

## 3. Project Structure

```
movies-center/
├── backend/
│   ├── MoviesCenter.sln
│   ├── src/
│   │   ├── MoviesCenter.Api/                  # ASP.NET Core host, controllers, middleware, OpenAPI
│   │   ├── MoviesCenter.Application/          # MediatR handlers, DTOs, validators, interfaces
│   │   ├── MoviesCenter.Domain/               # Entities, enums, value objects, domain events
│   │   └── MoviesCenter.Infrastructure/       # EF Core, TMDB client, Hangfire jobs, email, push, external services
│   └── tests/
│       ├── MoviesCenter.UnitTests/
│       └── MoviesCenter.IntegrationTests/
├── frontend/
│   ├── angular.json
│   ├── package.json
│   ├── tsconfig.json
│   └── src/
│       ├── app/
│       │   ├── core/                          # Services, guards, interceptors, models, constants
│       │   ├── shared/                        # Shared components, directives, pipes
│       │   ├── features/
│       │   │   ├── home/
│       │   │   ├── browse/                    # Movies & Series pages
│       │   │   ├── search/
│       │   │   ├── title-detail/
│       │   │   ├── player/
│       │   │   ├── auth/
│       │   │   ├── watchlist/
│       │   │   ├── profile/
│       │   │   ├── notifications/
│       │   │   └── admin/                     # Lazy-loaded, non-obvious route
│       │   ├── layout/                        # Shell, nav, footer, mobile tab bar
│       │   └── app.routes.ts
│       ├── assets/
│       ├── styles/                            # Design system tokens, global styles
│       ├── environments/
│       └── generated/                         # Auto-generated API client (ng-openapi-gen)
├── docs/
│   ├── PROJECT_GUIDE.md                       # This file
│   └── REFERENCE_NOTES.md                     # Layout reference study
├── docker-compose.yml                         # SQL Server + Redis for local dev
├── .github/
│   └── workflows/
│       ├── backend-ci.yml
│       └── frontend-ci.yml
├── .env.example                               # Template for secrets
├── .gitignore
└── README.md
```

### Backend Modules (within Application/Infrastructure)

Modules are organized as namespaces/folders, not separate projects. Each module owns its handlers, validators, DTOs, and infrastructure implementations. Modules communicate through interfaces, never direct references.

| Module | Responsibility |
|---|---|
| **Identity** | Registration, login, JWT/refresh tokens, password reset, 2FA, roles, profiles |
| **Catalog** | Titles, seasons, episodes, genres, people, cast, TMDB sync, search |
| **Playback** | Sources (ThirdPartyEmbed, UploadedAsset), media assets, source health, reports |
| **Library** | Watchlist, watch history, continue watching, progress tracking, ratings |
| **Discovery** | Home page aggregation, trending, now playing, search suggestions |
| **Notifications** | In-app inbox, email dispatch, web push, notification settings, new-episode jobs |
| **Analytics** | Event ingestion, daily stats aggregation, admin dashboard queries |
| **Admin** | Admin-specific orchestration, TMDB import, source management, audit logging |

---

## 4. Features: Guest vs Login

### Guest (no account required)
- Browse home page (hero, content rows)
- Browse movies and series with filters
- Search with instant suggestions
- Open title detail pages (backdrop, info, cast, trailer, seasons/episodes, where-to-watch)
- Watch videos (both source types)
- Report a broken source (rate-limited)
- Watchlist stored in browser localStorage
- Watch progress stored in browser localStorage
- Cookie notice (accept/reject)

### Login Required
- Watchlist synced across devices
- Watch history synced
- Continue Watching (synced)
- Rate titles (1–5 stars, one per user per title)
- Profile and account settings
- Notification inbox (bell icon with unread count)
- Notification settings (per type, per channel)
- Web push subscription (opt-in from settings)
- Import guest data on first login (merge localStorage into account)

### Admin Only (login + Admin role + 2FA)
- Search and import titles from TMDB
- Manage sources (add embeds, upload videos, reorder, enable/disable)
- View and act on broken-source reports
- Analytics dashboard (plays, signups, top titles, failing sources, empty searches)
- All actions logged in AuditLogs

### Guest-to-Login Flow
When a guest taps a login-required feature:
1. Show a compact login/register dialog (not a full-page redirect).
2. After login, return to the same page.
3. Auto-trigger `POST /api/v1/me/import-guest-data` to merge browser localStorage data into the account.
4. Clear the localStorage copies.

---

## 5. Architecture & Data Flow

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────┐
│                     Angular SSR App                      │
│  (standalone components, signals, lazy routes, OnPush)   │
│  Generated API client ← OpenAPI spec                     │
└────────────────────────┬────────────────────────────────┘
                         │ HTTPS (REST under /api/v1)
                         ▼
┌─────────────────────────────────────────────────────────┐
│                  ASP.NET Core 10 API                     │
│  Controllers → MediatR → Handlers → Domain/Infra        │
│  JWT auth │ Rate limiting │ CORS │ OpenAPI                │
├─────────────────────────────────────────────────────────┤
│  Identity │ Catalog │ Playback │ Library │ Discovery     │
│  Notifications │ Analytics │ Admin                       │
└───┬──────┬──────┬──────┬──────┬─────────────────────────┘
    │      │      │      │      │
    ▼      ▼      ▼      ▼      ▼
┌──────┐┌──────┐┌──────┐┌──────┐┌──────────────┐
│SQL   ││Redis ││TMDB  ││Post- ││Bunny Stream  │
│Server││      ││API   ││mark  ││(uploads)     │
└──────┘└──────┘└──────┘└──────┘└──────────────┘
                         │
                    Hangfire Jobs
              (TMDB sync, source health,
               notification dispatch)
```

### Request Flow
1. **Angular** calls the generated API client method.
2. **Controller** receives the request, runs auth/rate-limit middleware.
3. Controller dispatches a **MediatR** command or query.
4. **Handler** uses FluentValidation, then calls domain services and infrastructure.
5. **Infrastructure** talks to SQL Server (via EF Core), Redis (cache), TMDB API (via HTTP client with Polly), or other external services.
6. Response flows back as a DTO.

### TMDB Data Flow & SQL Persistence
- **Zero runtime TMDB API calls**: All public API requests (home feed, browse, search, title details) read strictly from local SQL Server (with Redis caching on top).
- TMDB API is accessed **only** by the background sync service (`ITmdbSyncService`).
- **Sync triggers**:
  1. **Admin Panel**: On-demand manual sync button (`POST /api/v1/admin/tmdb/sync`).
  2. **Hangfire Recurring Job**: Daily scheduled sync at a configurable time (default 3:00 AM UTC).
- **Data synced & persisted**:
  - Titles (movies and series) with full metadata, categories (trending, popular, top-rated, upcoming), vote counts, and status.
  - Genres and title-genre associations.
  - TV Seasons and Episodes.
  - Cast, crew, and People.
  - Trailers & videos.
  - Streaming watch providers.
  - Auto-generated `TitleSources` (VidSrc embeds).
- Sync history, run duration, items updated, and errors are tracked in the `TmdbSyncLogs` table.
- Poster and backdrop URLs still use TMDB's CDN directly (`https://image.tmdb.org/t/p/...`) without re-hosting files.
- TMDB attribution is displayed in the footer.

### Video Source Flow

**Third-party embed provider: VidSrc (`vidsrc.to`)**

Embed URLs are **auto-generated** from the TMDB ID using a URL template stored in the `SourceProviders` table:
- Movies: `https://vidsrc.to/embed/movie/{tmdbId}`
- TV episodes: `https://vidsrc.to/embed/tv/{tmdbId}/{season}/{episode}`

The admin can also **manually add, edit, or override** any source URL per title/episode.

**Flow:**
1. When a title is imported from TMDB, the system **auto-generates** a ThirdPartyEmbed source using the VidSrc URL template. The source is marked `IsAutoGenerated = true`.
2. Admin can manually add additional sources, change the auto-generated URL, or disable it. Manual sources have `IsAutoGenerated = false`.
3. For uploads: admin's browser uploads directly to Bunny Stream via a secure TUS upload URL. Our API never proxies the video file.
4. Bunny Stream webhook notifies our API when encoding is complete → marks the MediaAsset as ready.
5. Player page fetches sources ordered by `sort_order`, picks the first active one.
6. On error/timeout, player switches to the next source.
7. Users can report broken sources; a background job tracks fail counts and disables consistently failing sources.

**CSP allowlist:** `frame-src` includes `vidsrc.to` (configurable via `ALLOWED_FRAME_SOURCES` env var). Additional embed provider domains can be added later.

---

## 6. Database Schema

All tables use PascalCase C# entities mapped to snake_case SQL names. Table names are plural. Primary keys are `Id` (GUID or int as appropriate). Timestamps use `DateTimeOffset` (UTC).

### Identity Module

```
Users (ASP.NET Identity)
├── Id (GUID, PK)
├── UserName, Email, PasswordHash, ...
├── EmailConfirmed, TwoFactorEnabled
├── LockoutEnd, AccessFailedCount
├── CreatedAt, UpdatedAt
│
Roles (ASP.NET Identity)
├── Id (GUID, PK)
├── Name (User, Admin)
│
UserRoles (ASP.NET Identity)
├── UserId (FK → Users)
├── RoleId (FK → Roles)
│
Profiles
├── Id (GUID, PK)
├── UserId (FK → Users, unique)
├── DisplayName, AvatarUrl
├── PreferredLanguage
├── CreatedAt, UpdatedAt
│
RefreshTokens
├── Id (GUID, PK)
├── UserId (FK → Users)
├── Token (hashed), ExpiresAt
├── CreatedAt, RevokedAt, ReplacedByTokenId
```

### Catalog Module

```
Titles
├── Id (int, PK)
├── TmdbId (int, unique, indexed)
├── Type (enum: Movie, Series)
├── Title, OriginalTitle, OriginalLanguage
├── Overview, Tagline
├── PosterPath, BackdropPath
├── ReleaseDate (for movies), FirstAirDate (for series)
├── Runtime (minutes, nullable)
├── VoteAverage, VoteCount (from TMDB)
├── Status (Released, InProduction, etc.)
├── Popularity
├── IsActive
├── CreatedAt, UpdatedAt, LastTmdbSyncAt
│
Seasons
├── Id (int, PK)
├── TitleId (FK → Titles, indexed)
├── TmdbId (int)
├── SeasonNumber
├── Name, Overview, PosterPath
├── AirDate, EpisodeCount
│
Episodes
├── Id (int, PK)
├── SeasonId (FK → Seasons, indexed)
├── TmdbId (int)
├── EpisodeNumber
├── Name, Overview, StillPath
├── AirDate, Runtime
│
Genres
├── Id (int, PK)
├── TmdbId (int, unique)
├── Name
│
TitleGenres
├── TitleId (FK → Titles)
├── GenreId (FK → Genres)
├── PK (TitleId, GenreId)
│
People
├── Id (int, PK)
├── TmdbId (int, unique, indexed)
├── Name, ProfilePath
│
TitleCast
├── Id (int, PK)
├── TitleId (FK → Titles, indexed)
├── PersonId (FK → People)
├── Character
├── DisplayOrder
│
Videos (trailers from TMDB)
├── Id (int, PK)
├── TitleId (FK → Titles, indexed)
├── TmdbKey (YouTube key)
├── Name, Type (Trailer, Teaser, etc.)
├── Site (YouTube)
│
Providers (streaming platforms from TMDB)
├── Id (int, PK)
├── TmdbId (int, unique)
├── Name, LogoPath
│
TitleProviders (where-to-watch, display only)
├── Id (int, PK)
├── TitleId (FK → Titles, indexed)
├── ProviderId (FK → Providers)
├── Type (enum: Stream, Rent, Buy)
├── Country
```

### Playback Module

```
SourceProviders
├── Id (int, PK)
├── Name (e.g., "VidSrc", "Bunny Stream")
├── BaseUrl (nullable — e.g., "https://vidsrc.to")
├── EmbedUrlTemplate (nullable — e.g., "/embed/movie/{tmdbId}" or "/embed/tv/{tmdbId}/{season}/{episode}")
├── IsActive
├── Seeded: VidSrc (Id=1, BaseUrl="https://vidsrc.to", IsActive=true)
│
TitleSources
├── Id (int, PK)
├── TitleId (FK → Titles, indexed)
├── EpisodeId (FK → Episodes, nullable, indexed)
├── SourceProviderId (FK → SourceProviders)
├── Type (enum: ThirdPartyEmbed, UploadedAsset)
├── Reference (embed URL or external ref — e.g., "https://vidsrc.to/embed/movie/1003596")
├── IsAutoGenerated (bool — true if auto-created from URL template, false if admin added manually)
├── MediaAssetId (FK → MediaAssets, nullable)
├── Language
├── Quality (e.g., "1080p", "720p", "4K")
├── SortOrder
├── IsActive
├── FailCount
├── CreatedAt, UpdatedAt
│
MediaAssets
├── Id (GUID, PK)
├── BunnyVideoId (string)
├── Status (enum: Uploading, Encoding, Ready, Failed)
├── Duration (seconds)
├── ThumbnailUrl
├── SubtitlesJson (JSON array of subtitle tracks)
├── CreatedAt, UpdatedAt
│
SourceReports
├── Id (int, PK)
├── TitleSourceId (FK → TitleSources, indexed)
├── UserId (FK → Users, nullable — guests can report)
├── Reason (enum: NotPlaying, WrongContent, BadQuality, Other)
├── Note (optional text, max 500 chars)
├── Status (enum: Pending, Reviewed, Resolved)
├── IpAddress (for rate limiting, hashed)
├── CreatedAt, ReviewedAt
```

### Library Module

```
WatchlistItems
├── Id (int, PK)
├── UserId (FK → Users, indexed)
├── TitleId (FK → Titles)
├── AddedAt
├── Unique (UserId, TitleId)
│
WatchHistory
├── Id (int, PK)
├── UserId (FK → Users, indexed)
├── TitleId (FK → Titles)
├── EpisodeId (FK → Episodes, nullable)
├── TitleSourceId (FK → TitleSources, nullable)
├── ProgressSeconds (int)
├── DurationSeconds (int, nullable)
├── IsCompleted (bool, set at ~90%)
├── WatchedAt (last updated)
├── Unique (UserId, TitleId, EpisodeId)
│
Ratings
├── Id (int, PK)
├── UserId (FK → Users, indexed)
├── TitleId (FK → Titles)
├── Score (1–5)
├── CreatedAt, UpdatedAt
├── Unique (UserId, TitleId)
```

### Notifications Module

```
Notifications
├── Id (GUID, PK)
├── UserId (FK → Users, indexed)
├── Type (enum: NewEpisode, ContinueWatchingReminder)
├── Title (display text)
├── Body (display text)
├── Data (JSON — e.g., titleId, episodeId)
├── IsRead
├── CreatedAt
├── Index (UserId, IsRead, CreatedAt DESC)
│
NotificationSettings
├── Id (int, PK)
├── UserId (FK → Users, unique)
├── NewEpisodeInApp (bool, default true)
├── NewEpisodeEmail (bool, default true)
├── NewEpisodePush (bool, default false)
├── ContinueWatchingInApp (bool, default true)
├── ContinueWatchingEmail (bool, default false)
├── ContinueWatchingPush (bool, default false)
│
PushSubscriptions
├── Id (int, PK)
├── UserId (FK → Users, indexed)
├── Endpoint (string)
├── P256dh (string)
├── Auth (string)
├── UserAgent (string)
├── CreatedAt
```

### Analytics Module

```
AnalyticsDailyStats
├── Id (int, PK)
├── Date (date, indexed)
├── MetricName (string — e.g., "plays", "signups", "searches")
├── MetricValue (long)
├── Dimension (string, nullable — e.g., titleId, searchTerm)
├── Index (Date, MetricName)
```

### Admin Module

```
AuditLogs
├── Id (GUID, PK)
├── UserId (FK → Users)
├── Action (string — e.g., "SourceCreated", "TitleImported")
├── EntityType (string)
├── EntityId (string)
├── Details (JSON)
├── IpAddress
├── CreatedAt
├── Index (CreatedAt DESC)
```

### Key Indexes

| Table | Index | Purpose |
|---|---|---|
| Titles | TmdbId (unique) | TMDB lookups |
| Titles | Type, Popularity DESC | Browse by type |
| Titles | Type, ReleaseDate DESC | New releases |
| TitleSources | TitleId, EpisodeId, SortOrder | Source ordering |
| TitleSources | IsActive, FailCount | Health checks |
| WatchHistory | UserId, WatchedAt DESC | Continue watching |
| WatchlistItems | UserId, AddedAt DESC | User watchlist |
| Notifications | UserId, IsRead, CreatedAt DESC | Inbox queries |
| SourceReports | Status, CreatedAt | Admin report queue |

---

## 7. API Surface

Base path: `/api/v1`

### Public Endpoints (guest or authenticated)

```
GET    /home                                  → Hero + content rows
GET    /titles?type&genre&year&language&sort&page&pageSize  → Paginated browse
GET    /titles/{id}                           → Full title details
GET    /titles/{id}/seasons                   → Seasons + episodes for a series
GET    /titles/{id}/where-to-watch            → Streaming provider logos
GET    /titles/{id}/sources                   → Active sources (ordered)
GET    /search?q={query}&page&pageSize        → Full search results
GET    /search/suggest?q={query}              → Top 5–8 instant suggestions
POST   /sources/{id}/report                   → Report broken source (rate-limited)

POST   /auth/register                         → Create account
POST   /auth/login                            → Get JWT + set refresh cookie
POST   /auth/refresh                          → Refresh JWT using cookie
POST   /auth/logout                           → Revoke refresh token
POST   /auth/forgot-password                  → Send password reset email
POST   /auth/reset-password                   → Reset password with token
POST   /auth/verify-email                     → Verify email with token
```

### Login Required (`/me/*`)

```
GET    /me/profile                            → Current user profile
PUT    /me/profile                            → Update display name, avatar, language
PUT    /me/change-password                    → Change password

GET    /me/watchlist?page&pageSize            → Paginated watchlist
POST   /me/watchlist/{titleId}                → Add to watchlist
DELETE /me/watchlist/{titleId}                → Remove from watchlist

GET    /me/history?page&pageSize              → Watch history
GET    /me/continue-watching                  → Titles in progress
PUT    /me/progress                           → Save playback position (body: titleId, episodeId, seconds, duration)

POST   /me/import-guest-data                  → Merge localStorage watchlist + history into account

POST   /titles/{id}/rating                    → Rate a title (body: score 1–5)
DELETE /titles/{id}/rating                    → Remove rating
GET    /me/ratings?page&pageSize              → User's ratings

GET    /me/notifications?page&pageSize        → Paginated inbox
GET    /me/notifications/unread-count         → Unread count (for bell badge)
POST   /me/notifications/{id}/read            → Mark one as read
POST   /me/notifications/read-all             → Mark all as read

GET    /me/notification-settings              → Current settings
PUT    /me/notification-settings              → Update per-type per-channel toggles

POST   /me/push-subscriptions                 → Register push subscription
DELETE /me/push-subscriptions                 → Unregister push subscription
```

### Admin Only (`/admin/*`)

```
GET    /admin/titles?q&page&pageSize          → Search managed titles
POST   /admin/tmdb/search?q                   → Search TMDB for titles to import
POST   /admin/tmdb/import                     → Import a title from TMDB (body: tmdbId, type)
POST   /admin/tmdb/sync                       → Trigger daily TMDB sync job manually

GET    /admin/sources?titleId&status&page     → List sources with filters
POST   /admin/sources                         → Add a source (embed or upload ref)
PUT    /admin/sources/{id}                    → Update source (reorder, enable/disable)
DELETE /admin/sources/{id}                    → Remove a source

POST   /admin/uploads/create-url              → Get a secure direct-upload URL for Bunny Stream
GET    /admin/uploads/{assetId}/status         → Check encoding status

GET    /admin/reports?status&page&pageSize    → Broken source reports
PUT    /admin/reports/{id}                    → Update report status (reviewed, resolved)

GET    /admin/users?q&page&pageSize           → List users
GET    /admin/users/{id}                      → User detail

GET    /admin/analytics/overview              → Plays, signups, top titles, failing sources
GET    /admin/analytics/daily?metric&from&to  → Daily time series for a metric
GET    /admin/analytics/empty-searches        → Search terms with no results

GET    /admin/audit-logs?page&pageSize        → Admin action log
```

### Analytics Event Endpoint (public, batched, rate-limited)

```
POST   /analytics/events                      → Batch of anonymous events (only after consent)
```

---

## 8. Security

### Authentication & Authorization
- **ASP.NET Core Identity** for user management (password hashing, lockout, email confirmation).
- **JWT access token** (15-minute expiry) in Authorization header.
- **Rotating refresh token** in HTTP-only, Secure, SameSite=Strict cookie (7-day expiry). Old tokens are revoked on refresh.
- Roles: `User` (default on signup), `Admin` (seeded).
- All `/me/*` endpoints require `[Authorize]`.
- All `/admin/*` endpoints require `[Authorize(Roles = "Admin")]`.
- Admin login requires **2FA (TOTP)**. 2FA setup is required before any admin action.

### Admin Panel Access
- No link to admin in public UI. Route is a non-obvious path (e.g., `/panel/{random-segment}`).
- Lazy-loaded as a separate Angular bundle.
- Server-side role check on every admin endpoint — hidden URL is never the only protection.
- Lockout after 5 failed login attempts (15-minute lockout).
- Strict rate limiting on admin endpoints.
- Optional IP allowlist (configurable).

### OWASP Top 10 Compliance
- **Injection:** EF Core parameterized queries; no raw SQL.
- **Broken Auth:** Strong password policy, account lockout, JWT expiry, refresh token rotation.
- **Sensitive Data Exposure:** HTTPS only (HSTS), secure cookies, secrets in env vars never in code.
- **XXE / Deserialization:** System.Text.Json with strict settings.
- **Broken Access Control:** Server-side role checks on every endpoint, not just UI guards.
- **Security Misconfiguration:** CORS limited to our frontend origin(s), CSP headers, no debug in production.
- **XSS:** Angular's built-in sanitization, CSP headers, no innerHTML.
- **CSRF:** SameSite cookies, anti-forgery for cookie-based auth.
- **Vulnerable Dependencies:** Dependabot / `dotnet list package --vulnerable` in CI.
- **Logging & Monitoring:** Structured logging with Serilog, audit logs for admin actions.

### Rate Limiting (ASP.NET Core middleware + Redis)
- Public endpoints: 60 requests/minute per IP.
- Auth endpoints (login, register, forgot-password): 10 requests/minute per IP.
- Source report: 5 per hour per IP.
- Admin endpoints: 120 requests/minute per authenticated admin.
- Analytics event batch: 30 requests/minute per IP.

### Bot Protection
- **hCaptcha** on registration and login forms.
- Verified on the backend before processing.

### Content Security Policy
- `frame-src` restricted to an allowlist of embed provider domains (configurable in `appsettings.json`).
- Iframes use `sandbox` attribute and `referrerpolicy="no-referrer"`.

### Database
- Least-privilege SQL Server account for the application (no `sa` in production).
- Connection string from environment variables, never committed.

---

## 9. Development Process

1. **One feature at a time** per the Build Order.
2. Build → Test → Report → Wait for approval → Next feature.
3. Update this document after every feature.
4. No invented requirements — ask if unclear.
5. Keep modules independent; new modules can be added without changing existing ones.

### Git Workflow
- `main` branch is the stable branch.
- Feature branches: `feature/{step-number}-{short-name}` (e.g., `feature/01-project-setup`).
- Commit messages: conventional commits (`feat:`, `fix:`, `chore:`, `docs:`).

### Code Review Checklist (self-review before reporting)
- [ ] Builds without errors or warnings
- [ ] All tests pass
- [ ] No lint errors
- [ ] No hardcoded secrets or credentials
- [ ] API endpoints match the spec in this document
- [ ] Responsive design verified at all breakpoints
- [ ] Accessibility checked (focus, contrast, keyboard)
- [ ] PROJECT_GUIDE.md updated if anything changed

---

## 10. Build Order & Phase Plan

| Step | Feature | Status |
|---|---|---|
| 0 | Read prompt, ask questions | ✅ Complete |
| 1 | Reference site study | ✅ Complete |
| 2 | Project guide (this document) | ✅ Complete |
| 3 | Project setup: repo, design system, layout shell, CI, Docker | ✅ Complete |
| 4 | Home page: hero + content rows from TMDB | ✅ Complete |
| 5 | Movies & Series browse with filters and paging | ✅ Complete |
| 6 | Search with instant results | ✅ Complete |
| 7 | Title detail page | ✅ Complete |
| 8 | Video player page with dual-source model | ✅ Complete |
| 9 | Login and register + guest-to-account merge | ⬜ Next (Awaiting Approval) |
| 10 | Watchlist and Continue Watching | ⬜ Not started |
| 11 | Ratings (1–5 stars) | ⬜ Not started |
| 12 | Admin panel (TMDB import, sources, uploads, reports) | ⬜ Not started |
| 13 | Notifications (in-app, email, settings) | ⬜ Not started |
| 14 | PWA (installable, offline shell, update message, then web push) | ⬜ Not started |
| 15 | Analytics (consent-based events, admin dashboard) | ⬜ Not started |

### NOT NOW (ask before adding)
- Recommendations engine
- User reviews / comments
- Social login (Google, GitHub, etc.)
- Parental controls
- Billing and payments
- Top 10 row
- Full DevOps (Kubernetes, Terraform, etc.)
- Legal and compliance work

---

## 11. Coding Rules

### Backend (C# / ASP.NET Core)
- **C# 13** features (primary constructors, collection expressions, etc.).
- Nullable reference types enabled globally.
- `record` types for DTOs and commands/queries.
- One class/record per file (except small related DTOs).
- Async all the way — never `.Result` or `.Wait()`.
- Every external service behind an `I{Service}` interface registered in DI.
- Polly retry + timeout on all HTTP calls to external services (TMDB, Postmark, Bunny).
- FluentValidation for all command validation; validators auto-registered.
- MediatR pipeline behaviors for validation, logging, and performance monitoring.
- Structured logging with Serilog.
- XML doc comments on all public API endpoints (feeds into OpenAPI spec).
- No business logic in controllers — controllers only map HTTP to MediatR.

### Frontend (Angular / TypeScript)
- **Standalone components** only — no NgModules.
- **Signals** for reactive state; no RxJS Subjects for component state.
- **OnPush** change detection everywhere (Angular 22 default).
- **Lazy-loaded routes** for every feature module.
- `inject()` function instead of constructor injection.
- Strict TypeScript — no `any` types.
- One component per file; co-located `.ts`, `.html`, `.scss`, `.spec.ts`.
- Barrel exports (`index.ts`) per feature folder.
- All HTTP calls go through the generated API client (from OpenAPI spec).
- Environment config in `environment.ts` files.
- App name and global constants in `src/app/core/constants/app.constants.ts`.

### Shared
- All dates stored and transmitted as UTC ISO 8601.
- IDs: GUIDs for users, notifications, audit logs, media assets; auto-increment ints for catalog entities.
- Pagination: `page` (1-based) and `pageSize` (default 20, max 100).
- Error responses: consistent JSON shape `{ code, message, errors? }`.

---

## 12. Design Rules

### Color Tokens

| Token | Hex | Usage |
|---|---|---|
| `--bg-primary` | `#0E0E10` | Page background |
| `--bg-surface` | `#17171A` | Cards, panels, dropdowns |
| `--bg-raised` | `#222226` | Elevated elements, hover states |
| `--text-primary` | `#F2F2F2` | Headings, body text |
| `--text-secondary` | `#A1A1AA` | Labels, captions, metadata |
| `--border` | `#2E2E33` | Borders, dividers |
| `--accent` | `#D81F26` | Play button, active nav, progress bars, focus rings |
| `--accent-hover` | `#B91C22` | Accent hover/pressed state |
| `--rating` | `#F5B301` | Star rating highlight (used sparingly) |
| `--success` | `#22C55E` | Success states |
| `--error` | `#EF4444` | Error states |
| `--warning` | `#F59E0B` | Warning states |

### Typography

| Element | Font | Weight | Size |
|---|---|---|---|
| Logo / hero titles | Oswald | 700 | 32–48px |
| H1 (page titles) | Inter | 700 | 28–32px |
| H2 (section titles) | Inter | 600 | 20–24px |
| H3 (subsections) | Inter | 600 | 16–18px |
| Body | Inter | 400 | 14–16px |
| Caption / meta | Inter | 400 | 12–13px |
| Button | Inter | 600 | 14px |

### Spacing
- 8px grid system: `4, 8, 12, 16, 24, 32, 48, 64, 96`.
- Consistent gaps between cards: 12px mobile, 16px desktop.

### Component Rules
- Corner radius: 2–4px (sharp, not rounded).
- Borders: 1px solid `--border`. Almost no shadows.
- Buttons: rectangular, no rounded pills. Primary (filled `--accent`), secondary (outline), ghost (text only).
- **Restyle every Angular Material component** so nothing looks stock.
- No glassmorphism, no glow, no neon, no decorative gradients.
- Only gradient: dark fade over backdrop images (from left and bottom).
- Animations: subtle and functional only. `transform` and `opacity` transitions (200–300ms ease).
- `prefers-reduced-motion: reduce` → disable all animations.

### Icon Rules
- **Lucide** icons only. One stroke width throughout.
- Icons only where they carry function: play, bookmark, check, search, close, chevrons, volume, user, bell.
- Never in colored circles/boxes, never as decoration, no emoji.
- Prefer text labels in menus and headings; icons supplement, not replace.

### Card Design
- 2:3 aspect ratio (portrait poster).
- `object-fit: cover` on poster images.
- Desktop hover: `transform: scale(1.05)` with a semi-transparent bottom strip showing title + rating.
- Mobile: no hover effects.
- Skeleton loader: same 2:3 shape with `--bg-raised` pulsing animation.

### Responsive Breakpoints

| Name | Min Width | Posters per Row |
|---|---|---|
| xs (phone) | 320px | 3 |
| sm (phone+) | 375px | 3 |
| md (tablet) | 768px | 4–5 |
| lg (laptop) | 1024px | 5–6 |
| xl (desktop) | 1280px | 6–7 |
| 2xl (wide) | 1440px | 7–8 |
| 3xl (4K) | 1920px | 8 |

### Layout Specifics
- **Top nav:** Transparent over hero, solid `--bg-primary` after scroll. Links: Home, Movies, Series, Search, profile menu.
- **Mobile bottom tab bar:** Fixed, 5 tabs: Home, Search, Movies, Series, Profile. 56px height, `--bg-surface`.
- **Hero:** Full-width backdrop, `aspect-ratio: 16/9` (capped at 70vh). Left-aligned content with dark fade. Title in Oswald 700, synopsis truncated to 2 lines, genre/rating line, Play (accent filled) + More Info (outline) buttons.
- **Content rows:** Row title (Inter 600), "View All" link, horizontal scroll with arrow buttons on desktop hover, CSS scroll-snap on mobile.
- **Footer:** Compact. Logo, nav links, TMDB attribution with logo, copyright. Max 3 rows of content.

---

## 13. Testing Checklist

Required before reporting any feature as done:

### Backend
- [ ] Solution builds with zero errors and zero warnings
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Each new/changed endpoint tested as guest → correct result or 401
- [ ] Each new/changed endpoint tested as logged-in user → correct result
- [ ] Each new/changed endpoint tested as admin → correct result or 403
- [ ] Rate limiting verified on public endpoints

### Frontend
- [ ] `ng build` succeeds with zero errors
- [ ] `ng lint` passes with zero errors
- [ ] All unit tests pass (`ng test --watch=false`)
- [ ] SSR build succeeds (`ng build` with SSR config)

### Functionality
- [ ] Full feature tested end-to-end in the browser
- [ ] Loading state works (skeleton loaders)
- [ ] Empty state works (no data message)
- [ ] Error state works (API failure message)
- [ ] Guest flow works
- [ ] Logged-in flow works (where applicable)

### Design & Responsiveness
Test at these widths (portrait and landscape where relevant):

| Width | Device Class |
|---|---|
| 320px | Small phone |
| 375px | Standard phone |
| 414px | Large phone |
| 768px | Tablet portrait |
| 1024px | Tablet landscape / small laptop |
| 1280px | Laptop |
| 1440px | Desktop |
| 1920px | Full HD |

Check for: overflow, overlap, cut-off text, broken images, layout jumps, anything that looks generic/template-like.

### Feature-Specific
- **Video:** Test player on phone, tablet, desktop sizes. Fullscreen. Orientation changes. Both source types.
- **Notifications:** Test notification appears in inbox. Marks as read. Respects settings. No duplicates.
- **PWA:** Lighthouse PWA check. Install test on Android + desktop Chrome. Offline mode. Update message. Video and user data not cached.
- **Analytics:** No events before consent. Events fire after consent. No personal data in events.

### Security
- [ ] Protected routes redirect/reject unauthorized access
- [ ] Admin endpoints return 403 for non-admin users
- [ ] Admin endpoints return 401 for unauthenticated requests
- [ ] No secrets in source code or client bundles

---

## 14. Install, Run, Build & Test

### Prerequisites
- Node.js 22.x (`node -v`)
- npm 10.x+
- .NET SDK 10.0 (`dotnet --version`)
- Docker & Docker Compose (`docker compose version`)

### Initial Setup

```bash
# 1. Clone the repository
git clone <repo-url> movies-center
cd movies-center

# 2. Copy environment file
cp .env.example .env
# Fill in: TMDB_API_KEY, ADMIN_EMAIL, ADMIN_PASSWORD, JWT_SECRET, etc.

# 3. Start infrastructure (SQL Server + Redis)
docker compose up -d

# 4. Backend setup
cd backend
dotnet restore
dotnet ef database update --project src/MoviesCenter.Infrastructure --startup-project src/MoviesCenter.Api
cd ..

# 5. Frontend setup
cd frontend
npm install
npm run generate-api    # Generate API client from OpenAPI spec
cd ..
```

### Running Locally

```bash
# Terminal 1: Backend (runs on https://localhost:5001)
cd backend/src/MoviesCenter.Api
dotnet run

# Terminal 2: Frontend (runs on http://localhost:4200, proxies /api to backend)
cd frontend
npm run dev
```

### Building

```bash
# Backend
cd backend
dotnet build
dotnet publish -c Release -o ./publish

# Frontend
cd frontend
npm run build           # Production build with SSR
```

### Testing

```bash
# Backend tests
cd backend
dotnet test

# Frontend tests
cd frontend
npm run lint
npm run test -- --watch=false

# Frontend e2e (if applicable)
npm run e2e
```

### Docker Compose Services

```yaml
# docker-compose.yml provides:
# - sqlserver: SQL Server 2022 Developer on port 1433
# - redis: Redis 7 on port 6379
```

---

## 15. Environment Variables

Stored in `.env` (git-ignored). Template in `.env.example`.

| Variable | Description | Example |
|---|---|---|
| `DATABASE_CONNECTION_STRING` | SQL Server connection string | `Server=localhost,1433;Database=MoviesCenterDb;User Id=sa;Password=...;TrustServerCertificate=True` |
| `REDIS_CONNECTION_STRING` | Redis connection string | `localhost:6379` |
| `JWT_SECRET` | Secret key for JWT signing (min 32 chars) | `(generate a random 64-char string)` |
| `JWT_ISSUER` | JWT issuer | `https://localhost:5001` |
| `JWT_AUDIENCE` | JWT audience | `https://localhost:4200` |
| `TMDB_API_KEY` | TMDB API key (v3) | `4afe98c4e5ae2c05958c237fb4dca934` |
| `TMDB_ACCESS_TOKEN` | TMDB API Read Access Token (v4, optional) | `(bearer token)` |
| `ADMIN_EMAIL` | Initial admin account email | `admin.movies@gmail.com` |
| `ADMIN_PASSWORD` | Initial admin account password | `Admin#movies@123` |
| `POSTMARK_API_KEY` | Postmark server API token | `(from Postmark)` |
| `POSTMARK_FROM_EMAIL` | Sender email for transactional email | `noreply@moviescenter.com` |
| `VAPID_PUBLIC_KEY` | VAPID public key for web push | `(generated)` |
| `VAPID_PRIVATE_KEY` | VAPID private key for web push | `(generated)` |
| `VAPID_SUBJECT` | VAPID subject (email or URL) | `mailto:admin.movies@gmail.com` |
| `BUNNY_API_KEY` | Bunny Stream API key | `(from Bunny.net)` |
| `BUNNY_LIBRARY_ID` | Bunny Stream library ID | `(from Bunny.net)` |
| `BUNNY_WEBHOOK_SECRET` | Bunny webhook signing secret | `(from Bunny.net)` |
| `HCAPTCHA_SECRET_KEY` | hCaptcha server secret | `(from hCaptcha)` |
| `HCAPTCHA_SITE_KEY` | hCaptcha site key (also in frontend env) | `(from hCaptcha)` |
| `POSTHOG_API_KEY` | PostHog project API key (frontend) | `(from PostHog)` |
| `POSTHOG_HOST` | PostHog host URL | `https://app.posthog.com` |
| `CORS_ORIGINS` | Allowed frontend origins (comma-separated) | `http://localhost:4200,https://moviescenter.com` |
| `ALLOWED_FRAME_SOURCES` | Embed allowlist for CSP frame-src | `vidsrc.to` |

---

## 16. Decision Log

| # | Date | Decision | Rationale |
|---|---|---|---|
| 1 | 2026-10-24 | Angular 22 + .NET 10 LTS | Latest stable versions, long-term support |
| 2 | 2026-10-24 | TypeScript 6.0.x, Node.js 22.x | Required by Angular 22 |
| 3 | 2026-10-24 | SQL Server 2022 Developer in Docker | Free, full-featured, consistent local dev |
| 4 | 2026-10-24 | Redis 7.x in Docker | Cache + rate limit store, no extra infra |
| 5 | 2026-10-24 | Hangfire for background jobs | SQL Server storage, no extra infra needed |
| 6 | 2026-10-24 | Bunny Stream for admin uploads | Cheapest, free transcoding, TUS upload, HLS |
| 7 | 2026-10-24 | Postmark for email | Best deliverability, simple API, free dev tier |
| 8 | 2026-10-24 | VAPID web push (no Firebase) | No third-party dependency, works with Angular SW |
| 9 | 2026-10-24 | PostHog for analytics | Open-source, consent-friendly, custom events |
| 10 | 2026-10-24 | Lucide icons | Consistent stroke width, tree-shakable, Angular support |
| 11 | 2026-10-24 | Inter + Oswald fonts | Clean UI font + strong display font |
| 12 | 2026-10-24 | hCaptcha for bot protection | Privacy-friendly, free tier |
| 13 | 2026-10-24 | ng-openapi-gen for API client | Generates typed Angular services from OpenAPI |
| 14 | 2026-10-24 | snake_case SQL, PascalCase C# | EF Core convention, readable in both layers |
| 15 | 2026-10-24 | GitHub Actions CI | Simple, free for public repos |
| 16 | 2026-10-24 | VidSrc (`vidsrc.to`) for third-party embeds | Auto-generate embed URLs from TMDB ID; admin can manually override. Start with one provider, add more later |
| 17 | 2026-10-24 | Auto-generated sources with admin override | Sources auto-created on TMDB import using URL template; admin can edit/add/disable per title |
| 18 | 2026-10-25 | Local SQL persistence for TMDB data | Zero runtime TMDB calls. All data stored in SQL Server via Hangfire daily sync and Admin Sync Now button. Public endpoints query SQL Server |
| 19 | 2026-10-25 | Movies & Series Browse with Reactive Filters & SQL Paging | Implemented GET /api/v1/titles and GET /api/v1/genres querying SQL Server; Angular standalone browse views with FilterBar, Pagination, EmptyState, responsive grid, and query param synchronization |
| 20 | 2026-10-25 | Instant Search & Dedicated Search Page | Implemented /api/v1/search and /api/v1/search/suggest querying SQL Server with debouncing (300ms); global header search dropdown and dedicated /search view with type tabs, responsive grid, pagination, and empty state |
| 21 | 2026-10-25 | Title Detail View & Deep Metadata Surface | Implemented GET /api/v1/titles/{id}, /seasons, and /where-to-watch querying local SQL Server; created rich TitleDetailComponent with hero backdrop, metadata pills, season/episode selector, cast scroller, and YouTube trailer modal |
| 22 | 2026-10-25 | Video Player with Multi-Source Switcher & Broken Source Reporting | Implemented GET /api/v1/titles/{id}/sources and POST /api/v1/sources/{id}/report; built PlayerComponent (/watch/:type/:id) with dual-source toggle (VidSrc + Mirror fallback), TV episode stepper/scroller, theater and lights-off cinema dimmer, and problem reporting modal with feedback dialog |

---

## 17. Open Questions

| # | Question | Status |
|---|---|---|
| 1 | ~~Which third-party video/player service will be used?~~ | ✅ **Resolved** — VidSrc (`vidsrc.to`). Auto-generate from TMDB ID, admin can override. `frame-src: vidsrc.to` |
| 2 | PostHog: self-hosted (Docker) or PostHog Cloud? | To be decided before Build Step 15 |
| 3 | Production hosting environment (Azure, AWS, VPS, etc.)? | Not needed now, relevant for full DevOps later |
| 4 | Custom domain and SSL setup? | Not needed for local development |
| 5 | Bunny Stream account credentials? | Needed before Build Step 12 (admin uploads) |
| 6 | Postmark account credentials? | Needed before Build Step 13 (notifications with email) |
| 7 | hCaptcha site key and secret? | Needed before Build Step 9 (login/register) |

---

*This document is the single source of truth for Movies Center. It is updated after every feature completion.*
