# Movies Center 🎬

A modern, fast, and responsive movie and series streaming web platform built with **Angular 22** (SSR, Standalone, Signals) and **ASP.NET Core 10** (Clean Architecture, EF Core, SQL Server).

---

## 🚀 Technology Stack

- **Frontend**: Angular 22, SSR (Server-Side Rendering), SCSS Design System, Angular Material 22, Lucide Icons, TypeScript 6.
- **Backend**: ASP.NET Core 10, Clean Architecture (Domain, Application, Infrastructure, Api), EF Core 10, SQL Server, Redis, Hangfire.
- **Data & Streaming**: The Movie Database (TMDB) API, VidSrc third-party embeds, Bunny Stream self-hosted video.

---

## 📁 Repository Structure

```
movies-center/
├── .github/workflows/       # GitHub Actions CI pipelines
├── backend/                 # ASP.NET Core 10 backend solution
│   ├── src/
│   │   ├── MoviesCenter.Domain/
│   │   ├── MoviesCenter.Application/
│   │   ├── MoviesCenter.Infrastructure/
│   │   └── MoviesCenter.Api/
│   └── tests/
│       ├── MoviesCenter.UnitTests/
│       └── MoviesCenter.IntegrationTests/
├── frontend/                # Angular 22 SSR application
│   ├── src/
│   │   ├── app/
│   │   ├── assets/
│   │   └── styles/          # Design tokens & dark theme
│   └── package.json
├── docs/                    # Architectural guides & design specs
├── docker-compose.yml       # SQL Server 2022 + Redis 7
├── .env.example             # Environment variables template
└── README.md
```

---

## 🛠️ Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Node.js 22 LTS](https://nodejs.org/) & npm 10+
- [Docker](https://www.docker.com/) (or local SQL Server / LocalDB)

### Quick Start

1. **Clone repository & prepare environment:**
   ```bash
   cp .env.example .env
   ```

2. **Start Infrastructure (optional if using local SQL Server):**
   ```bash
   docker compose up -d
   ```

3. **Backend:**
   ```bash
   cd backend
   dotnet restore
   dotnet build
   cd src/MoviesCenter.Api
   dotnet run
   ```

4. **Frontend:**
   ```bash
   cd frontend
   npm install
   npm start
   ```
   Open `http://localhost:4200` in your browser.

---

## 📜 Documentation

- [Project Guide & Architecture](docs/PROJECT_GUIDE.md)
- [Reference Notes & UX Analysis](docs/REFERENCE_NOTES.md)
