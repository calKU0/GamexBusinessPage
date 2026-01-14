# Gamex Olkusz - Corporate Website

A professional, high-performance web platform designed for **Gamex**, a Polish industry leader in road construction and heavy machinery rental. This project focuses on modern UX, local SEO optimization, and robust technical architecture using .NET technology.

## 🚀 Key Features

- **SEO-First Architecture**: Advanced JSON-LD Schema.org markup for `LocalBusiness`, `Project`, and `Service` types to dominate local search results.
- **Performance Optimized**: High Core Web Vitals scores through manual LCP optimization (`fetchpriority`, preloading) and WebP image formats.
- **Dynamic Portfolio**: Dedicated section showcasing completed road construction projects with categorized galleries.
- **Responsive Design**: Fully mobile-optimized interface built with a custom Bootstrap-based design system.
- **Business-Oriented UI**: Specialized service catalogs for roadworks and machinery rental.

## 🛠️ Tech Stack

- **Backend**: ASP.NET Core 10.0 (Razor Pages)
- **Frontend**: HTML5, CSS3 (Custom animations), JavaScript (ES6+)
- **Styling**: Bootstrap 5 + Custom SCSS/CSS
- **SEO/Metadata**: Structured Data (JSON-LD), Dynamic Meta Tags
- **Deployment Ready**: Optimized for Azure App Service or Windows-based IIS hosting

## 📊 SEO & Performance Highlights

- **Structured Data**: Integrated hierarchy: `CollectionPage` → `OfferCatalog` → `Product/Service`.
- **Image Optimization**: System-wide use of `.webp` and `loading="lazy"` for non-critical assets.
- **Asset Preloading**: Strategic use of `<link rel="preload">` for hero images to minimize LCP.

## 💻 Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2022 or VS Code

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/YourUsername/gamex-olkusz.git
   ```
2. Navigate to the project directory:
   ```bash
   cd gamex-olkusz
   ```
3. Restore dependencies and run:
   ```bash
   dotnet watch run
   ```

## 📜 License

This project is developed as a commercial website for Gamex Olkusz. All rights to the branding and images are reserved.
