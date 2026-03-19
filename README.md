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

## Technical Notes

- **Framework:** `ASP.NET Core Razor Pages` on `net10.0`.
- **Caching:** memory cache (`CatalogCache`) + output cache policies.
- **Data source:** JSON files in `Data/` (`machines.json`, `services.json`, `transport.json`).
- **Imaging:** `SixLabors.ImageSharp` and `SixLabors.ImageSharp.Web`.
- **Routing:** lowercase URL convention enabled globally.

## Project Structure

- `Pages/` - Razor Pages (public pages + admin pages).
- `Services/` - business services (catalog access, mail, form protection, images, schema helpers).
- `Models/` - catalog and form models.
- `Data/` - editable offer catalogs in JSON format.
- `wwwroot/` - static assets (CSS, JS, images).

## License

This project is developed as a commercial website for Gamex Olkusz. Branding assets and business content remain proprietary.
See the [LICENSE](LICENSE) file for more information.
