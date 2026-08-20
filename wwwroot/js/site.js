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
    if (header) {
        var SCROLL_ENTER = 200;
        var SCROLL_LEAVE = 90;
        var headerCompact = false;

        var setHeaderState = function () {
            var y = window.scrollY;
            if (!headerCompact && y > SCROLL_ENTER) {
                headerCompact = true;
                header.classList.add("is-scrolled");
            } else if (headerCompact && y < SCROLL_LEAVE) {
                headerCompact = false;
                header.classList.remove("is-scrolled");
            }
        };
        setHeaderState();
        window.addEventListener("scroll", setHeaderState, { passive: true });
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

    if (spyTargets.length) {
        var updateSpy = function () {
            // The visible area starts at the bottom edge of the sticky header,
            // because whatever sits under it is covered.
            var top = header ? header.getBoundingClientRect().bottom : 0;
            var bottom = window.innerHeight;
            var minCover = (bottom - top) * 0.3;

            var best = null;
            spyTargets.forEach(function (target) {
                var rect = target.section.getBoundingClientRect();
                var cover = Math.min(rect.bottom, bottom) - Math.max(rect.top, top);
                if (cover > minCover) {
                    minCover = cover;
                    best = target.id;
                }
            });

            spyTargets.forEach(function (target) {
                target.link.classList.toggle("is-active", target.id === best);
            });
        };

        updateSpy();
        window.addEventListener("scroll", updateSpy, { passive: true });
        window.addEventListener("resize", updateSpy, { passive: true });
    }

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
            var observer = new IntersectionObserver(function (entries) {
                entries.forEach(function (entry) {
                    if (!entry.isIntersecting) return;
                    entry.target.classList.add("is-visible");
                    observer.unobserve(entry.target);
                });
            }, { rootMargin: "0px 0px -8% 0px", threshold: 0.08 });

            revealables.forEach(function (el) { observer.observe(el); });

            // Safety net: should the observer fail for any reason, the content
            // still has to appear. Losing the animation beats losing a section.
            window.setTimeout(revealAll, 3000);
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

            var duration = 1100;
            var started = null;

            var step = function (now) {
                if (started === null) started = now;
                var progress = Math.min((now - started) / duration, 1);
                // fast start, soft landing on the target value
                var eased = 1 - Math.pow(1 - progress, 3);
                el.textContent = Math.round(target * eased) + suffix;
                if (progress < 1) window.requestAnimationFrame(step);
            };

            window.requestAnimationFrame(step);
        };

        var counterObserver = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (!entry.isIntersecting) return;
                runCounter(entry.target);
                counterObserver.unobserve(entry.target);
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
