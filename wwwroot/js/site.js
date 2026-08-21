/* GAMEX - interface enhancements.
   Everything here is progressive enhancement: without JavaScript the page stays
   fully readable, and every animation drives transform/opacity only, so none of
   it causes layout shift. */
(function () {
    "use strict";

    var motionQuery = window.matchMedia("(prefers-reduced-motion: reduce)");
    var reduceMotion = motionQuery.matches;

    /* ----------------------------------------------------------------------
       1. Header scroll state

       The class lands on <header>, because that is the sticky element - the
       navbar is only its content and has nothing of its own to stick to.

       Two thresholds (hysteresis) are required. The header occupies layout
       space, so collapsing it shortens the document, the browser corrects the
       scroll position, that drops back below the threshold, the header expands
       again - several times per second. The gap between the thresholds is
       larger than the header's height difference, so the loop cannot close.
       ---------------------------------------------------------------------- */
    var header = document.getElementById("gamexHeader");
    var headerHeight = 0;

    // The header sits in normal flow, so collapsing it shortens the document and
    // moves every section up by the difference. Anything caching a section's
    // position has to be told to measure again once that has happened.
    var geometryListeners = [];

    var remeasureGeometry = function () {
        geometryListeners.forEach(function (listener) { listener(); });
    };

    var measureHeader = function () {
        headerHeight = header ? header.offsetHeight : 0;
    };

    var setHeaderState = function () { };

    if (header) {
        var SCROLL_ENTER = 200;
        var SCROLL_LEAVE = 90;
        var headerCompact = false;

        setHeaderState = function () {
            var y = window.scrollY;
            if (!headerCompact && y > SCROLL_ENTER) {
                headerCompact = true;
                header.classList.add("is-scrolled");
                measureHeader();
            } else if (headerCompact && y < SCROLL_LEAVE) {
                headerCompact = false;
                header.classList.remove("is-scrolled");
                measureHeader();
            }
        };
        measureHeader();
        setHeaderState();

        // The header animates between its tall and compact heights, so the read
        // taken the moment the class flips catches it mid-transition. Settle it
        // once the animation has finished.
        //
        // The transitions run on descendants - never on the header itself - so
        // this listens for the events bubbling up. Both filters matter: only two
        // properties change how tall the header is, the contact bar's grid row
        // and the logo's height, while colours, shadows, backgrounds and the
        // link underlines all bubble the same event. Unfiltered that was 68
        // events per toggle, each re-reading the header and every tracked
        // section - 340 forced layout reads on the home page and 1292 on the
        // machine listing. Coalescing into one frame takes it to a single pass.
        var settlePending = false;

        header.addEventListener("transitionend", function (event) {
            if (event.propertyName !== "grid-template-rows" && event.propertyName !== "height") {
                return;
            }
            if (settlePending) return;

            settlePending = true;
            window.requestAnimationFrame(function () {
                settlePending = false;
                measureHeader();
                remeasureGeometry();
            });
        });
    }

    /* ----------------------------------------------------------------------
       1b. Anchor highlighting in the navigation ("O nas", "FAQ")

       Both point at the same page, so the route alone cannot tell which one is
       active. We mark the section currently covering most of the viewport; when
       none is visible, both go dim.
       ---------------------------------------------------------------------- */
    var spyTargets = [];
    document.querySelectorAll("[data-spy-target]").forEach(function (link) {
        var id = link.getAttribute("data-spy-target");
        var section = document.getElementById(id);
        if (section) spyTargets.push({ id: id, link: link, section: section });
    });

    var updateSpy = function () { };

    // Set by the reveal block below; the batched scroll handler calls it so the
    // safety net can still notice a broken observer once the reader scrolls.
    var revealFallback = function () { };

    if (spyTargets.length) {
        // Section positions are measured once instead of on every scroll event.
        // Reading layout inside a scroll handler, right after the header has
        // just changed class, forces the browser to re-run layout synchronously
        // before it can answer - which showed up as ~150 ms of forced reflow.
        var measureSpy = function () {
            spyTargets.forEach(function (target) {
                var rect = target.section.getBoundingClientRect();
                target.top = rect.top + window.scrollY;
                target.height = rect.height;
            });
        };

        updateSpy = function () {
            // The visible area starts at the bottom edge of the sticky header,
            // because whatever sits under it is covered.
            var scrollY = window.scrollY;
            var top = headerHeight;
            var bottom = window.innerHeight;
            var minCover = (bottom - top) * 0.3;

            var best = null;
            spyTargets.forEach(function (target) {
                var sectionTop = target.top - scrollY;
                var sectionBottom = sectionTop + target.height;
                var cover = Math.min(sectionBottom, bottom) - Math.max(sectionTop, top);
                if (cover > minCover) {
                    minCover = cover;
                    best = target.id;
                }
            });

            spyTargets.forEach(function (target) {
                target.link.classList.toggle("is-active", target.id === best);
            });
        };

        var remeasure = function () {
            measureHeader();
            measureSpy();
            updateSpy();
        };

        remeasure();
        geometryListeners.push(remeasure);
        // Late-arriving fonts and images shift sections, so measure again once
        // the page has fully settled.
        window.addEventListener("load", remeasure);
        window.addEventListener("resize", remeasure, { passive: true });
    }

    /* ----------------------------------------------------------------------
       1c. Category sidebar on the catalog listings

       Each category has its own URL, because those pages carry their own title,
       description and canonical and are what rank for the category phrases.
       Only the "all" view lists every category at once, and there the sidebar
       tracks whichever section the reader has reached.
       ---------------------------------------------------------------------- */
    var categoryItems = [];

    var updateCategorySpy = function () {
        if (categoryItems.length < 2) return;

        // A heading counts as reached once it sits at or above the bottom edge
        // of the sticky header.
        var line = window.scrollY + headerHeight + 8;
        var active = categoryItems[0];

        categoryItems.forEach(function (item) {
            if (item.top <= line) active = item;
        });

        // At the very bottom the last section may never cross the line, so the
        // final category takes over once the page bottoms out.
        if (window.innerHeight + window.scrollY >= document.body.scrollHeight - 2) {
            active = categoryItems[categoryItems.length - 1];
        }

        categoryItems.forEach(function (item) {
            item.link.classList.toggle("is-active", item === active);
        });
    };

    var measureCategories = function () {
        categoryItems.forEach(function (item) {
            item.top = item.section.getBoundingClientRect().top + window.scrollY;
        });
    };

    // Rebuilt after a swap, because the sidebar and the sections are replaced.
    var setupCategorySpy = function () {
        categoryItems = [];

        var panel = document.querySelector(".machine-category-panel");
        if (!panel) return;

        // Spying only makes sense while every category is on the page. On a
        // filtered view the server already marks the selected one.
        var sections = document.querySelectorAll(
            ".machine-category, .services-category, .transport-category"
        );
        if (sections.length < 2) return;

        // Every link carries the id of its section, rendered from the same
        // category key as the section itself, so the two are paired exactly.
        // Matching on the visible label instead broke as soon as a heading was
        // worded or marked up differently from the sidebar.
        panel.querySelectorAll("a[data-section]").forEach(function (link) {
            var section = document.getElementById(link.getAttribute("data-section"));
            if (section) categoryItems.push({ link: link, section: section, top: 0 });
        });

        measureCategories();
        updateCategorySpy();
    };

    var remeasureCategories = function () {
        measureHeader();
        measureCategories();
        updateCategorySpy();
    };

    setupCategorySpy();
    geometryListeners.push(remeasureCategories);
    window.addEventListener("load", remeasureCategories);
    window.addEventListener("resize", remeasureCategories, { passive: true });

    /* ----------------------------------------------------------------------
       1e. Switching category without reloading the page

       The categories stay separate documents for search engines, but for a
       visitor a full reload between them is a white flash and a lost scroll
       position. The new page is fetched and its <main> swapped in, so the URL,
       title and markup end up exactly as the server rendered them - just
       without the reload. Anything unexpected falls back to normal navigation.
       ---------------------------------------------------------------------- */
    var catalogMain = document.querySelector("main");

    if (catalogMain && document.querySelector(".machine-category-panel")) {
        var swapping = false;
        var currentCatalogUrl = window.location.pathname + window.location.search;

        var applyDocument = function (doc) {
            var incoming = doc.querySelector("main");
            if (!incoming) return false;

            catalogMain.innerHTML = incoming.innerHTML;
            document.title = doc.title;

            var canonical = document.querySelector('link[rel="canonical"]');
            var incomingCanonical = doc.querySelector('link[rel="canonical"]');
            if (canonical && incomingCanonical) {
                canonical.setAttribute("href", incomingCanonical.getAttribute("href"));
            }

            // The reveal observer only watches elements present when it was
            // created, so freshly swapped sections would stay hidden.
            catalogMain.querySelectorAll(".reveal").forEach(function (el) {
                el.classList.add("is-visible");
            });

            setupCategorySpy();
            return true;
        };

        var loadCategory = function (href, addHistory) {
            if (swapping) return;
            swapping = true;
            document.body.classList.add("is-catalog-loading");

            window.fetch(href, { headers: { "X-Requested-With": "fetch" } })
                .then(function (response) {
                    if (!response.ok) throw new Error("HTTP " + response.status);
                    return response.text();
                })
                .then(function (html) {
                    var doc = new DOMParser().parseFromString(html, "text/html");

                    var commit = function () {
                        if (!applyDocument(doc)) throw new Error("no main element");
                        if (addHistory) window.history.pushState({ catalog: true }, "", href);
                        currentCatalogUrl = href;
                    };

                    // Runs once the new markup is in place. With a view
                    // transition the swap is asynchronous, so measuring here
                    // rather than straight after the call is what keeps this
                    // reading the new list instead of the old one.
                    var settle = function () {
                        var listing = document.querySelector(
                            ".machine-grid, .services-grid, .transport-grid, .machine-empty-state"
                        );
                        if (!listing) return;

                        var top = listing.getBoundingClientRect().top + window.scrollY - anchorOffset();
                        if (window.scrollY > top) {
                            window.scrollTo({
                                top: Math.max(top, 0),
                                behavior: reduceMotion ? "auto" : "smooth"
                            });
                        }
                    };

                    // A cross-fade where the browser offers one; elsewhere the
                    // swap is simply instant, which is still no worse than a
                    // reload.
                    if (!reduceMotion && document.startViewTransition) {
                        var transition = document.startViewTransition(commit);

                        // The browser skips the animation whenever the document
                        // cannot paint it - a hidden tab, a second click before
                        // the first settled. The DOM is still updated, so these
                        // rejections are expected and must not surface as
                        // uncaught errors.
                        var ignore = function () { };
                        if (transition.ready) transition.ready.catch(ignore);
                        if (transition.finished) transition.finished.catch(ignore);

                        if (transition.updateCallbackDone) {
                            // updateCallbackDone reports the swap itself, not the
                            // animation, so a rejection here means commit failed
                            // and the visitor still has to reach the page.
                            transition.updateCallbackDone.then(settle, function () {
                                window.location.href = href;
                            });
                        } else {
                            settle();
                        }
                    } else {
                        commit();
                        settle();
                    }
                })
                .catch(function () {
                    window.location.href = href;
                })
                .then(function () {
                    swapping = false;
                    document.body.classList.remove("is-catalog-loading");
                });
        };

        document.addEventListener("click", function (event) {
            if (event.defaultPrevented || event.button !== 0) return;
            if (event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;

            var link = event.target.closest ? event.target.closest(".machine-category-link") : null;
            if (!link || !link.getAttribute("href") || link.getAttribute("href").charAt(0) === "#") return;

            var url;
            try { url = new URL(link.href, window.location.href); } catch (e) { return; }
            if (url.origin !== window.location.origin) return;
            if (url.pathname !== window.location.pathname) return;

            event.preventDefault();
            loadCategory(url.pathname + url.search, true);
        });

        window.addEventListener("popstate", function () {
            if (!document.querySelector(".machine-category-panel")) return;

            // Anchor clicks push history entries too. Those differ only by hash
            // and the listing on screen is already the right one, so re-fetching
            // and re-swapping it would throw away the reader's position for
            // nothing.
            var target = window.location.pathname + window.location.search;
            if (target === currentCatalogUrl) return;

            currentCatalogUrl = target;
            loadCategory(target, false);
        });
    }

    // A single scroll listener, batched into one animation frame: the class
    // change and the geometry read no longer interleave on every event.
    var scrollPending = false;
    window.addEventListener("scroll", function () {
        if (scrollPending) return;
        scrollPending = true;
        window.requestAnimationFrame(function () {
            setHeaderState();
            updateSpy();
            updateCategorySpy();
            revealFallback();
            scrollPending = false;
        });
    }, { passive: true });

    /* ----------------------------------------------------------------------
       1d. Smooth scrolling for in-page anchors

       CSS scroll-behavior only covers navigation the browser performs itself,
       and it is switched off under prefers-reduced-motion. Handling the click
       here keeps the offset identical to scroll-padding-top, moves focus to the
       target for keyboard and screen reader users, and leaves a history entry
       so Back returns to where the reader was.
       ---------------------------------------------------------------------- */
    var anchorOffset = function () {
        var declared = parseFloat(
            window.getComputedStyle(document.documentElement).scrollPaddingTop
        );
        return isNaN(declared) ? headerHeight + 16 : declared;
    };

    document.addEventListener("click", function (event) {
        if (event.defaultPrevented || event.button !== 0) return;
        if (event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;

        var link = event.target.closest ? event.target.closest('a[href]') : null;
        if (!link || link.hasAttribute("download") || link.target === "_blank") return;

        var url;
        try { url = new URL(link.href, window.location.href); } catch (e) { return; }

        // Same document only. A link to another page keeps its normal
        // navigation, fragment and all.
        if (url.origin !== window.location.origin) return;
        if (url.pathname !== window.location.pathname) return;
        if (!url.hash || url.hash === "#") return;

        var id = decodeURIComponent(url.hash.slice(1));
        var target = document.getElementById(id);
        if (!target) return;

        event.preventDefault();

        var top = target.getBoundingClientRect().top + window.scrollY - anchorOffset();
        window.scrollTo({
            top: Math.max(top, 0),
            behavior: reduceMotion ? "auto" : "smooth"
        });

        if (window.history && window.history.pushState) {
            window.history.pushState(null, "", url.hash);
        }

        // Without this the next Tab press would resume from the link, not from
        // the section just jumped to. preventScroll keeps the smooth scroll.
        if (!target.hasAttribute("tabindex")) {
            target.setAttribute("tabindex", "-1");
        }
        target.focus({ preventScroll: true });
    });

    /* ----------------------------------------------------------------------
       2. Section reveal on scroll
       ---------------------------------------------------------------------- */
    var revealables = document.querySelectorAll(".reveal");

    var revealAll = function () {
        for (var i = 0; i < revealables.length; i++) {
            revealables[i].classList.add("is-visible");
        }
    };

    if (revealables.length) {
        if (reduceMotion || !("IntersectionObserver" in window)) {
            revealAll();
        } else {
            var observerReported = false;

            var observer = new IntersectionObserver(function (entries) {
                observerReported = true;
                entries.forEach(function (entry) {
                    if (!entry.isIntersecting) return;
                    entry.target.classList.add("is-visible");
                    observer.unobserve(entry.target);
                });
            }, { rootMargin: "0px 0px -8% 0px", threshold: 0.08 });

            revealables.forEach(function (el) { observer.observe(el); });

            // Safety net for the case where the observer never reports and the
            // page would stay blank.
            //
            // This used to be a plain revealAll on a three second timer, which
            // also revealed every section further down the page: anything the
            // reader reached after that was already visible and never animated.
            // It now waits for actual evidence that the observer is broken -
            // something inside the viewport that it should have reported and
            // did not - so sections below the fold keep their entrance.
            revealFallback = function () {
                if (observerReported) {
                    revealFallback = function () { };
                    return;
                }

                var viewportHeight = window.innerHeight;
                for (var i = 0; i < revealables.length; i++) {
                    var el = revealables[i];
                    if (el.classList.contains("is-visible")) continue;

                    var rect = el.getBoundingClientRect();
                    if (rect.top < viewportHeight && rect.bottom > 0) {
                        revealAll();
                        revealFallback = function () { };
                        return;
                    }
                }
            };

            window.setTimeout(function () { revealFallback(); }, 3000);
        }
    }

    /* ----------------------------------------------------------------------
       3. Counters in the trust bar
       The values are already in the HTML - only the count-up is animated.
       ---------------------------------------------------------------------- */
    var counters = document.querySelectorAll("[data-count-to]");
    if (counters.length && !reduceMotion && "IntersectionObserver" in window) {
        var runCounter = function (el) {
            var target = parseInt(el.getAttribute("data-count-to"), 10);
            var suffix = el.getAttribute("data-count-suffix") || "";
            if (isNaN(target)) return;

            var duration = 1400;
            var started = null;

            var step = function (now) {
                if (started === null) started = now;
                var progress = Math.min((now - started) / duration, 1);
                // Eases towards the target, but gently: a cubic curve put 87% of
                // the count into the first half and then crawled, which read as
                // the number arriving rather than counting.
                var eased = 1 - Math.pow(1 - progress, 2);
                el.textContent = Math.round(target * eased) + suffix;
                if (progress < 1) window.requestAnimationFrame(step);
            };

            window.requestAnimationFrame(step);
        };

        // The trust bar is in view the moment the page loads, but it fades in on
        // a delay. Counting immediately meant the numbers had almost landed
        // before the bar appeared; waiting for the fade to finish left visible
        // zeros sitting there instead. So it starts when the fade starts - the
        // numbers climb while the bar is coming in.
        var whenEntranceStarts = function (el, run) {
            var animated = el.closest(".anim-fade-up, .anim-fade-down, .anim-hero-title");
            if (!animated) {
                run();
                return;
            }

            var styles = window.getComputedStyle(animated);
            if (!styles.animationName || styles.animationName === "none") {
                run();
                return;
            }

            // If the fade is already under way we have missed its start event.
            var running = animated.getAnimations ? animated.getAnimations() : [];
            for (var i = 0; i < running.length; i++) {
                if (running[i].currentTime > 0) {
                    run();
                    return;
                }
            }

            var done = false;
            var go = function () {
                if (done) return;
                done = true;
                run();
            };

            animated.addEventListener("animationstart", function (event) {
                // Animations on children bubble up here too.
                if (event.target === animated) go();
            });

            // Nothing may report back - an interrupted animation, or an
            // environment that never runs it - and the numbers still have to
            // reach their target.
            var delay = (parseFloat(styles.animationDelay) || 0) * 1000;
            window.setTimeout(go, delay + 250);
        };

        // The markup carries the final number, so that a reader without
        // JavaScript still sees it. Reset to the starting value now, while the
        // bar is still faded out, or the count would visibly snap back from the
        // final figure to zero the moment it appears.
        var primeCounter = function (el) {
            var target = parseInt(el.getAttribute("data-count-to"), 10);
            if (isNaN(target)) return;
            el.textContent = "0" + (el.getAttribute("data-count-suffix") || "");
        };

        var counterObserver = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (!entry.isIntersecting) return;
                counterObserver.unobserve(entry.target);
                primeCounter(entry.target);
                whenEntranceStarts(entry.target, function () { runCounter(entry.target); });
            });
        }, { threshold: 0.6 });

        counters.forEach(function (el) { counterObserver.observe(el); });
    }

    /* ----------------------------------------------------------------------
       4. Subtle hero background parallax
       Runs on the "translate" property, because "transform" is taken by the
       gxHeroPan animation.
       ---------------------------------------------------------------------- */
    var heroBg = document.querySelector(".home-hero-bg");
    if (heroBg && !reduceMotion && window.matchMedia("(min-width: 992px)").matches) {
        var ticking = false;
        window.addEventListener("scroll", function () {
            if (ticking) return;
            ticking = true;
            window.requestAnimationFrame(function () {
                var offset = Math.min(window.scrollY, 700) * 0.15;
                heroBg.style.setProperty("--parallax", offset.toFixed(1) + "px");
                ticking = false;
            });
        }, { passive: true });
    }

    /* ----------------------------------------------------------------------
       5. Mobile menu

       The only feature Bootstrap's JavaScript bundle (80 KB) was still needed
       for. Collapsing comes down to toggling a class, so we do it ourselves.
       ---------------------------------------------------------------------- */
    var collapse = document.getElementById("gamexNavbar");
    var toggler = document.querySelector(".navbar-toggler");

    if (collapse && toggler) {
        var setMenu = function (open) {
            collapse.classList.toggle("show", open);
            toggler.setAttribute("aria-expanded", open ? "true" : "false");
        };

        // "Oferta" submenu - a separate button next to the link. Collapsed by
        // default so the whole navigation fits on a phone screen.
        var submenuToggles = collapse.querySelectorAll(".submenu-toggle");

        var collapseSubmenus = function () {
            submenuToggles.forEach(function (button) {
                button.setAttribute("aria-expanded", "false");
                var wrapper = button.closest(".dropdown-hover-wrapper");
                if (wrapper) wrapper.classList.remove("is-open");
            });
        };

        submenuToggles.forEach(function (button) {
            button.addEventListener("click", function () {
                var wrapper = button.closest(".dropdown-hover-wrapper");
                if (!wrapper) return;
                var open = !wrapper.classList.contains("is-open");
                wrapper.classList.toggle("is-open", open);
                button.setAttribute("aria-expanded", open ? "true" : "false");
            });
        });

        toggler.addEventListener("click", function () {
            var open = !collapse.classList.contains("show");
            setMenu(open);
            // Closing the whole menu collapses the submenus too, so the next
            // open starts from the same short view as the first one.
            if (!open) collapseSubmenus();
        });

        // Clicking a link closes the menu - otherwise, after jumping to an
        // anchor, the list would cover the content the user just moved to.
        collapse.addEventListener("click", function (event) {
            if (event.target.closest("a") && collapse.classList.contains("show")) {
                setMenu(false);
                collapseSubmenus();
            }
        });

        // Escape closes the menu, and returning to the horizontal layout
        // clears the state.
        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape" && collapse.classList.contains("show")) {
                setMenu(false);
                collapseSubmenus();
                toggler.focus();
            }
        });

        window.matchMedia("(min-width: 1200px)").addEventListener("change", function (event) {
            if (event.matches) {
                setMenu(false);
                collapseSubmenus();
            }
        });
    }

    /* ----------------------------------------------------------------------
       6. React to the reduced-motion setting changing mid-session
       ---------------------------------------------------------------------- */
    var onMotionChange = function (event) {
        reduceMotion = event.matches;
        if (reduceMotion) revealAll();
    };

    if (typeof motionQuery.addEventListener === "function") {
        motionQuery.addEventListener("change", onMotionChange);
    } else if (typeof motionQuery.addListener === "function") {
        motionQuery.addListener(onMotionChange);
    }
})();
