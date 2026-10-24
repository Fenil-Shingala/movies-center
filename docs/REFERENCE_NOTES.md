# Reference Site Study — Layout & Usability Notes

> **Purpose:** This document records layout and usability observations from the reference site (cinehd.vc) as of October 2026. It is used **only** as a guide for page structure and flow. We do not copy its name, logo, text, images, code, exact design, or any information about its video sources.

---

## 1. Page List

| Page | URL Pattern | Purpose |
|---|---|---|
| Home | `/home` | Hero banner + content rows |
| Movies | `/search?type=movie` | Browse / filter movies |
| Series (TV) | `/search?type=tv` | Browse / filter TV shows |
| Anime | `/search/anime` | Anime-specific browse |
| Title Detail (Movie) | `/movie/{tmdb_id}` | Full movie info + player |
| Title Detail (TV) | `/tv/{tmdb_id}` | Full series info + episodes + player |
| Search | Search bar in nav | Instant search overlay |
| Watchlist | `/watchlist` | User's saved titles (called "Favorites") |
| Settings | `/settings` | Data export/import for watchlist |
| Providers | `/providers` | Grid of streaming provider logos |

---

## 2. Navigation

### Top Navigation (Desktop)
- Persistent horizontal bar at the top of every page.
- Links: **Home**, **Movies**, **Series**, **Anime**, **Settings**, **More** (dropdown).
- A **Search** icon/button opens an overlay or inline search field.
- The nav appears to be **solid dark** at all times (not transparent over the hero).
- No visible user profile or login button — the site operates without accounts (watchlist is browser-local).

### Bottom Tab Bar (Mobile)
- Fixed bottom bar with icons: **Home**, **Search**, **Movies**, **Series**, **Anime**, **Settings**, **More**.
- Provides quick access on small screens — good pattern.

### Footer
- Links: Terms of Service, Privacy Policy, FAQs, Contact.
- Quick links to Movies, TV Shows, Anime, Favorites.
- A disclaimer about not storing files.
- Copyright notice.
- No TMDB attribution visible.

---

## 3. Home Page — Content Rows

### Hero / Banner
- **Not a traditional hero banner.** The home page goes straight into content rows without a full-width backdrop hero.
- There is a "Providers" row at the very top showing streaming service logos (Netflix, Amazon, etc.) — this is unique but clutters the top of the page.

### Content Rows (in order)
1. **Now Playing** — horizontal scroll of ~20 movie cards, "View All" link
2. **Trending Movies** — similar horizontal row, ~20 cards
3. **Trending TV** — same format
4. **Netflix Originals** — series from Netflix
5. **Amazon Prime Shows**
6. **Apple TV+ Shows**
7. **Disney+ Shows** — ~20 cards visible
8. **Indian Movies** — regional category
9. **Peacock TV Shows**
10. **Max Shows** — ~20 cards visible

### Row Behavior
- Horizontal scroll with no visible navigation arrows (scroll only).
- "View All" link at the top-right of each row leads to the full browse page with that filter applied.
- Cards wrap from left to right; the row extends off-screen and is scrollable.

---

## 4. Card Design

### Standard Card
- **Aspect ratio:** 2:3 (portrait poster).
- **Content on card face:** Poster image only (no overlay text on the card itself in default state).
- **Below the card:** Title text, type label (`Movie` or `TV`), year, and language code (e.g., `EN`, `KO`, `FR`).
- **Rating:** Shown on some cards as a small number (e.g., `7.5`) — visible on the title detail page's "More Like This" section but inconsistently shown on home rows.

### Hover Effects
- Not determinable from HTML content alone, but typical for this kind of site: likely a slight scale/shadow effect on hover.

### Card Density
- Rows show approximately 6–8 cards visible at a time on desktop before scrolling.
- Cards appear tightly packed with small gaps.

---

## 5. Movies / Series Browse Page

### Layout
- Uses the same `/search` endpoint with query parameter `?type=movie` or `?type=tv`.
- The HTML content returned was **mostly empty** — the page content is rendered client-side (SPA pattern).
- Expected structure: a grid of poster cards with filter options.

### Filters (from URL analysis)
- **Type:** `movie` or `tv`
- **Trending:** `trending=true`
- **Now Playing:** `now_playing=true`
- **Watch Provider:** `watch_provider={id}` (Netflix=8, Amazon=9, etc.)
- **Country:** `country=IN` (for Indian movies)
- **Cast search:** `cast={name}`
- Likely also supports: genre, year, language, sort — but not visible in the scraped HTML.

---

## 6. Title Detail Page

### Movie Detail (example: Avengers: Doomsday)
- **Header area:** Title name, type label ("Movie"), year.
- **Synopsis:** Full plot description.
- **Cast section:** Scrollable cast list showing actor photo, name, and character name. Each is a link to search by cast.
- **Download button:** External link (not relevant to us).
- **"You may also like" row:** Poster cards of similar titles showing title, type, year, and rating.

### Series Detail (example: Lupin)
- **Header area:** Title name, "Tv" label, synopsis.
- **Cast section:** Same format as movie.
- **Episodes section:** Listed but details rendered client-side. Season/episode selector expected.
- **Trailer section:** Placeholder for embedded trailers (likely YouTube).
- **"You may also like" row:** Similar titles.

### Missing / Not Visible
- No backdrop image in the scraped content (likely loaded dynamically or as a background CSS image).
- No visible Play button in the scraped HTML — player is loaded dynamically.
- No rating widget or watchlist button in the scraped structure.
- No "Where to Watch" / streaming providers panel on the title page.
- No tabs for organizing content (Episodes, Trailers, Details, More Like This).

---

## 7. Search

- Search is triggered from the nav bar's search icon.
- Opens an overlay or navigates to a search results page.
- No instant suggestion / autocomplete visible in the HTML.
- Results appear to use the same browse grid.

---

## 8. Settings Page

- Very minimal: only watchlist data export/import.
- Export: copy JSON or download file.
- Import: paste JSON or upload file.
- No account management, no notification settings, no preferences.

---

## 9. What Works Well

| # | Aspect | Details |
|---|---|---|
| 1 | **Simple navigation** | Home, Movies, Series, Anime — clear and immediate. No deep nesting. |
| 2 | **Content-forward layout** | The home page is almost entirely content rows. Users see titles immediately. |
| 3 | **Provider-based rows** | Organizing by streaming provider (Netflix, Disney+, etc.) is a useful browsing paradigm. |
| 4 | **Bottom tab bar on mobile** | Essential links always accessible without scrolling up. |
| 5 | **Card information** | Showing type (Movie/TV), year, and language directly is useful for multilingual catalogs. |
| 6 | **Cast linking** | Clicking a cast member searches for their other titles — good for exploration. |
| 7 | **"View All" per row** | Easy way to dive deeper into any category. |
| 8 | **Watchlist export/import** | Smart way to handle persistence without accounts (browser-based). |
| 9 | **Low click depth** | Title → Detail → Play: 2 clicks max. |
| 10 | **Consistent card format** | Same 2:3 poster card used everywhere — predictable and scannable. |

---

## 10. What Feels Cluttered, Slow, or Confusing

| # | Problem | How We Do It Better |
|---|---|---|
| 1 | **No hero banner.** The home page jumps straight into rows — no focal point, no "what to watch tonight" moment. | Full-width hero with backdrop, left-aligned title and synopsis, genre/rating line, primary Play and secondary More Info buttons. Rotates through 4–5 trending titles. |
| 2 | **Providers row at the top** is confusing — it's just logos with no context. | Move provider content into the browse filters or a "Where to Watch" section on title pages. |
| 3 | **Too many rows.** 10+ rows of 20 cards each makes the home page extremely long and repetitive. | Limit to 5–6 well-curated rows (Trending, Now Playing, Popular Movies, Popular Series, and after login, Continue Watching). |
| 4 | **No navigation arrows on rows.** Horizontal scrolling without visible controls is not discoverable on desktop. | Arrow buttons appear on hover (desktop); swipe on mobile. |
| 5 | **Cards have no hover effect or quick info.** Desktop users must click into every title to learn anything beyond the poster. | Subtle zoom (1.05×) on hover with a title + rating strip overlay at the bottom. |
| 6 | **Browse page renders empty without JS.** The movies/series browse page is completely blank without client-side rendering. | SSR renders the initial grid; filters and pagination work with or without JS. |
| 7 | **No search suggestions.** Typing a query gives no instant feedback. | Debounced instant suggestions as you type (top 5–8 results with posters). |
| 8 | **Title page lacks structure.** Everything is flat — no tabs, no clear separation between episodes, trailers, details, and similar titles. | Tabs: Episodes (for series), Trailers, More Like This, Details. Clear visual sections. |
| 9 | **No visible Play button or player area** in the title page HTML. Users must figure out how to watch. | Prominent Play button in the hero area; clear player page with source switching. |
| 10 | **No rating or watchlist action on the title page.** Users can't rate or save directly. | Star rating widget (1–5, logged-in users), Watchlist toggle button with clear states. |
| 11 | **No empty states or error messages.** If content doesn't load, nothing tells the user what happened. | Skeleton loaders while loading, friendly messages for empty/error states. |
| 12 | **No TMDB credit.** The site uses TMDB data without visible attribution. | TMDB logo and attribution in the footer (required by TMDB terms). |
| 13 | **Footer is text-heavy and promotional.** The disclaimer text is awkward. | Clean, minimal footer: logo, nav links, TMDB credit, copyright. No promotional text. |
| 14 | **Settings page is bare.** Only export/import with no user preferences. | Full settings: profile, notification preferences (per channel), display preferences, data management. |
| 15 | **Mobile experience unclear.** Client-rendered content may cause layout issues on slower connections. | Mobile-first responsive design; SSR for fast first paint; skeleton loaders shaped like posters. |
| 16 | **No accessibility features visible.** No visible focus indicators, alt text, or keyboard navigation support. | Full accessibility: visible focus rings, proper alt text, ARIA labels, keyboard nav, reduced-motion support. |

---

## 11. Structural Takeaways for Movies Center

### Home Page
1. **Hero banner** — full-width backdrop, left-aligned title and synopsis, genre/rating line, primary Play and secondary More Info buttons. Dark fade overlay from left and bottom. Auto-rotates through 4–5 trending titles.
2. **Content rows** (5–6 max): Trending, Now Playing, Popular Movies, Popular Series, and (after login) Continue Watching.
3. Each row: bold title, "View All" link, 2:3 poster cards with arrow navigation on desktop, swipe on mobile.

### Browse Pages (Movies / Series)
- Grid of poster cards with server-rendered initial content.
- Sticky filter bar: genre dropdown, year range, language, sort by (popularity, rating, release date).
- Infinite scroll or paginated (with page numbers for SEO).

### Title Detail Page
- Large backdrop, title, meta line (year, runtime, rating, genres).
- Play button (primary) and Watchlist button (outline/toggle).
- Tabs: Episodes (series only), Trailers, More Like This, Details (cast, crew, synopsis).
- "Where to Watch" panel showing streaming provider logos.
- Star rating widget (1–5, logged-in only).

### Player Page
- Fluid 16:9 player area.
- Source selector (if multiple sources).
- Report broken source button.
- Next episode button (series).
- Touch-friendly controls, fullscreen support.

### Search
- Instant search overlay/dropdown with debounced suggestions (posters + title + year + type).
- Full results page for enter/submit.

### Mobile
- Bottom tab bar: Home, Search, Movies, Series, Profile.
- All content rows swipeable.
- Poster grid: ~3 per row on phones, ~5 on tablets.
- 44px minimum touch targets.

---

*This document is for internal reference only. It guides our layout decisions but we build our own original design and brand.*
