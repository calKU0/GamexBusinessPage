# Gamex Olkusz - Business Website

> 💼 **Commercial Project**

Corporate website for **Gamex Olkusz**, built with **ASP.NET Core Razor Pages (.NET 10)**.  
The project combines a public offer website with an authenticated admin area for catalog maintenance.

## Overview

The application presents:

- machine rental offer,
- road and construction services,
- transport offer,
- realizations and company information,
- a protected contact form with anti-spam safeguards,
- SEO-oriented metadata and structured data.

## Main Capabilities

### Public website

- SEO-friendly, lowercase routes (e.g. `/oferta/wypozyczenie-maszyn/{slug}`, `/kontakt`).
- Dynamic offer pages based on JSON catalogs in `Data/`.
- Machine details pages with per-machine schema data and contact preselection.
- Dedicated pages for `Oferta`, `Transport`, `Usługi`, `Realizacje`, and `Dotacja`.
- Generated `sitemap.xml` endpoint including static and dynamic machine URLs.

### Contact flow

- Contact form endpoint in `Pages/Contact.cshtml.cs`.
- Server-side message sending through SMTP (`ContactEmailService`).
- Form hardening via `ContactFormProtectionService`:
  - honeypot field,
  - protected form token with expiration,
  - per-IP rate limiting,
  - per-email submission cooldown.

### Admin panel

- Cookie-based admin authentication (`/Admin/Login`).
- Authorization policy protecting the whole `/Admin` area.
- Catalog management for:
  - machines,
  - services,
  - transport.
- Image processing pipeline (`ImageService`) with:
  - upload validation,
  - resize,
  - WebP conversion,
  - main image selection,
  - physical file deletion.

## Front-end

The public pages load **no third-party CSS or JavaScript**. Bootstrap and Font
Awesome were replaced with project-owned equivalents, so the browser opens no
connection to an external origin before it can paint.

- **Styles** are authored in `Styles/`, split into 17 topic files (tokens, base,
  primitives, buttons, layout, navigation, per-page sections, animations,
  accessibility, responsive). They are concatenated into a single
  `wwwroot/css/site.css` at build time.
- **Grid and utilities** (`Styles/00-grid.css`) are a hand-picked subset of the
  Bootstrap classes the markup actually uses. Full Bootstrap is still loaded by
  `_AdminLayout` only, which is excluded from search engines.
- **Icons** come from an inline SVG sprite (`Pages/Shared/_IconSprite.cshtml`,
  22 symbols) used through `<svg class="ico"><use href="#i-name"></use></svg>`.
  They inherit `currentColor` and scale with the font size.
- **Typefaces** (Barlow, Barlow Condensed) are self-hosted from `wwwroot/fonts`
  as 12 subsetted `woff2` files. See [Styles/FONTS.md](Styles/FONTS.md) for the
  licence terms and for how to add a weight.
- **JavaScript** (`wwwroot/js/site.js`) is progressive enhancement only: header
  scroll state, scrollspy, scroll reveal, counters, hero parallax and the mobile
  menu. Without it every page stays fully readable and navigable.
- **Motion** is limited to `transform` and `opacity` so it cannot cause layout
  shift, and it honours `prefers-reduced-motion`.

### Stylesheet build

`wwwroot/css/site.css` is **generated** and is not tracked in git. The
`BundleGamexStyles` target in `GamexBusinessPage.csproj` concatenates
`Styles/*.css` in filename order and writes the result on every build.

```bash
dotnet build
```

Notes:

- The target is attached through `InitialTargets`, so it runs before the static
  asset pipeline computes fingerprints. It also runs on a fresh clone.
- Editing anything in `Styles/` or `wwwroot/` requires a rebuild and an app
  restart before the change is served.
- Static asset compression is disabled in `Debug` and enabled in `Release`.

## SEO

- Per-page `title`, `description`, `keywords`, canonical URL and Open Graph tags.
- JSON-LD structured data assembled by `Services/SchemaBuilder.cs` and
  `Services/SchemaFactory.cs`: `LocalBusiness` + `GeneralContractor`, `WebSite`,
  `WebPage`, `BreadcrumbList`, `FAQPage`, `ItemList`, `Product`, `Service`,
  `OfferCatalog`, `PostalAddress`, `GeoCoordinates`, `OpeningHoursSpecification`.
- Consistent NAP (name, address, phone) across the contact bar, footer, contact
  page and structured data.
- `areaServed` declared at both voivodeship and city level (małopolskie,
  śląskie, świętokrzyskie).
- Breadcrumbs rendered from `Pages/Shared/_Breadcrumb.cshtml` and mirrored into
  `BreadcrumbList`.
- Unique per-machine descriptions in `Data/machines.json`; the first sentence
  becomes that page's meta description.
- `wwwroot/robots.txt` keeps `/admin` and `returnUrl` variants out of the index
  while leaving category views crawlable.

## Accessibility

- Skip link to `#tresc` as the first focusable element (WCAG 2.4.1).
- Visible keyboard focus styles (WCAG 2.4.7).
- Touch targets of at least 44x44 px on interactive controls (WCAG 2.5.8).
- Accessibility widget (`wwwroot/js/accessibility-widget.js`) offering font
  scaling, a high-contrast mode and a light-background mode, persisted in
  `localStorage`.
- `prefers-reduced-motion` support throughout `Styles/12-animations.css`.

## Technical Notes

- **Framework:** `ASP.NET Core Razor Pages` on `net10.0`.
- **Caching:** memory cache (`CatalogCache`) plus per-page `[OutputCache]`
  attributes. There is deliberately **no base output-cache policy** - it also
  covered static files and the contact page's antiforgery token.
- **Data source:** JSON files in `Data/` (`machines.json`, `services.json`, `transport.json`).
- **Imaging:** `SixLabors.ImageSharp` and `SixLabors.ImageSharp.Web`.
- **Routing:** lowercase URL convention enabled globally.
- **Static assets:** served through `MapStaticAssets` with build-time fingerprinting.

## Project Structure

- `Pages/` - Razor Pages (public pages + admin pages).
- `Pages/Shared/` - layout, breadcrumb, contact form and icon sprite partials.
- `Styles/` - CSS source files, bundled into `wwwroot/css/site.css` at build time.
- `Services/` - business services (catalog access, mail, form protection, images, schema builders).
- `Models/` - catalog and form models.
- `Data/` - editable offer catalogs in JSON format.
- `wwwroot/` - static assets (generated CSS, JS, images, self-hosted fonts).

## Configuration

`appsettings.json` and `appsettings.Development.json` are not tracked. The
application expects these sections:

| Section | Purpose |
|---|---|
| `Admin` | admin panel credentials |
| `Smtp` | outgoing mail server settings |
| `ContactForm` | recipient address and sender details |
| `ContactFormProtection` | rate limits, cooldowns and token lifetime |

## License

This project is developed as a commercial website for Gamex Olkusz. Branding assets and business content remain proprietary.
See the [LICENSE](LICENSE) file for more information.
