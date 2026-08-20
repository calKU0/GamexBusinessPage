(() => {
    const storageKey = "gamex-a11y-settings";
    const minScale = 0.85;
    const maxScale = 1.35;
    const step = 0.05;

    const widget = document.getElementById("a11yWidget");
    const toggleButton = document.getElementById("a11yWidgetToggle");
    const panel = document.getElementById("a11yWidgetPanel");
    const fontSizeValue = document.getElementById("a11yFontSizeValue");

    if (!widget || !toggleButton || !panel || !fontSizeValue) {
        return;
    }

    const state = {
        fontScale: 1,
        highContrast: false,
        lightTheme: false
    };

    const savedState = localStorage.getItem(storageKey);
    if (savedState) {
        try {
            const parsed = JSON.parse(savedState);
            if (typeof parsed.fontScale === "number") {
                state.fontScale = Math.min(maxScale, Math.max(minScale, parsed.fontScale));
            }
            state.highContrast = Boolean(parsed.highContrast);
            state.lightTheme = Boolean(parsed.lightTheme);

            // Settings saved before the two became exclusive can hold both. The
            // light theme is what such a visitor has been seeing, because its
            // rules come later in the stylesheet, so keep that and drop the
            // other rather than changing the page under them.
            if (state.highContrast && state.lightTheme) {
                state.highContrast = false;
            }
        } catch {
        }
    }

    const saveState = () => {
        localStorage.setItem(storageKey, JSON.stringify(state));
    };

    const updateButtonState = (action, isActive) => {
        const button = panel.querySelector(`[data-a11y-action="${action}"]`);
        if (!button) {
            return;
        }

        button.classList.toggle("active", isActive);
        button.setAttribute("aria-pressed", isActive ? "true" : "false");
    };

    const applyState = () => {
        document.documentElement.style.setProperty("--a11y-font-scale", state.fontScale.toFixed(2));

        // Set on <html> as well as <body>. The themes work by redefining the
        // design tokens, and the root element paints the background behind the
        // page, so it has to inherit the same values or it stays dark.
        [document.documentElement, document.body].forEach((element) => {
            element.classList.toggle("a11y-high-contrast", state.highContrast);
            element.classList.toggle("a11y-light-theme", state.lightTheme);
        });

        fontSizeValue.textContent = `${Math.round(state.fontScale * 100)}%`;
        updateButtonState("toggle-contrast", state.highContrast);
        updateButtonState("toggle-light", state.lightTheme);

        saveState();
    };

    const setOpen = (isOpen) => {
        widget.classList.toggle("is-open", isOpen);
        toggleButton.setAttribute("aria-expanded", isOpen ? "true" : "false");
    };

    toggleButton.addEventListener("click", () => {
        setOpen(!widget.classList.contains("is-open"));
    });

    panel.addEventListener("click", (event) => {
        const target = event.target;
        if (!(target instanceof HTMLElement)) {
            return;
        }

        const action = target.getAttribute("data-a11y-action");
        if (!action) {
            return;
        }

        switch (action) {
            case "increase-font":
                state.fontScale = Math.min(maxScale, +(state.fontScale + step).toFixed(2));
                break;
            case "decrease-font":
                state.fontScale = Math.max(minScale, +(state.fontScale - step).toFixed(2));
                break;
            // The two are whole-page themes and cannot both apply: with both
            // classes set the light rules win purely by coming later in the
            // stylesheet. That made switching from light to high contrast look
            // like nothing happened, while the reverse appeared to work and
            // silently left high contrast on. Turning one on clears the other.
            case "toggle-contrast":
                state.highContrast = !state.highContrast;
                if (state.highContrast) {
                    state.lightTheme = false;
                }
                break;
            case "toggle-light":
                state.lightTheme = !state.lightTheme;
                if (state.lightTheme) {
                    state.highContrast = false;
                }
                break;
            case "reset":
                state.fontScale = 1;
                state.highContrast = false;
                state.lightTheme = false;
                break;
            default:
                return;
        }

        applyState();
    });

    document.addEventListener("click", (event) => {
        const target = event.target;
        if (!(target instanceof Node)) {
            return;
        }

        if (!widget.contains(target)) {
            setOpen(false);
        }
    });

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape") {
            setOpen(false);
        }
    });

    applyState();
})();