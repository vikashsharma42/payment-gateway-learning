// Small usability helpers shared by every page
(function () {
    // 1. Stop double submits on every form marked data-submit-once.
    // The buttons are disabled on a short timer, so the submit button's own name and value still reach the server.
    // checkout.js handles its own form and is left alone (data-busy-skip).
    document.addEventListener('submit', function (event) {
        var form = event.target;
        if (!(form instanceof HTMLFormElement)) return;
        if (!form.hasAttribute('data-submit-once') || form.hasAttribute('data-busy-skip')) return;
        if (form.getAttribute('aria-busy') === 'true') {
            event.preventDefault();
            return;
        }

        form.setAttribute('aria-busy', 'true');
        setTimeout(function () {
            form.querySelectorAll('button[type="submit"], input[type="submit"]').forEach(function (button) {
                button.disabled = true;
                button.classList.add('is-busy');
            });
        }, 0);
    });

    // Back/forward cache: a page restored from history must accept input again
    window.addEventListener('pageshow', function () {
        document.querySelectorAll('form[aria-busy="true"]').forEach(function (form) {
            form.removeAttribute('aria-busy');
            form.querySelectorAll('.is-busy').forEach(function (button) {
                button.disabled = false;
                button.classList.remove('is-busy');
            });
        });
    });

    // 2. Server-side field errors: outline the field and move focus to the first one.
    // ASP.NET renders <span data-valmsg-for="Input.Email"> with the message inside.
    function fieldsFor(name) {
        return Array.prototype.slice.call(document.querySelectorAll('[name="' + name + '"]'))
            .filter(function (el) { return el.type !== 'hidden'; });
    }

    var firstInvalid = null;
    document.querySelectorAll('[data-valmsg-for]').forEach(function (span) {
        if (!span.textContent.trim()) return;
        fieldsFor(span.getAttribute('data-valmsg-for')).forEach(function (field) {
            field.classList.add('is-invalid');
            field.setAttribute('aria-invalid', 'true');
            if (!firstInvalid) firstInvalid = field;

            // Clear the highlight as soon as the person starts to correct the field
            var clear = function () {
                field.classList.remove('is-invalid');
                field.removeAttribute('aria-invalid');
                field.removeEventListener('input', clear);
            };
            field.addEventListener('input', clear);
        });
    });
    if (firstInvalid && typeof firstInvalid.focus === 'function') {
        firstInvalid.focus({ preventScroll: false });
    }

    // 3. Progress bar while the next page loads (links and forms on this site only)
    var bar = document.getElementById('nav-progress');
    function startProgress() {
        if (!bar) return;
        bar.classList.remove('is-done');
        bar.classList.remove('is-running');
        void bar.offsetWidth; // restart the animation
        bar.classList.add('is-running');
    }
    function finishProgress() {
        if (!bar) return;
        bar.classList.remove('is-running');
        bar.classList.add('is-done');
    }

    document.addEventListener('click', function (event) {
        var link = event.target.closest && event.target.closest('a[href]');
        if (!link || link.target || link.hasAttribute('download')) return;
        var href = link.getAttribute('href');
        if (!href || href.charAt(0) === '#') return;
        if (link.origin && link.origin !== window.location.origin) return;
        if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey) return;
        startProgress();
    });

    document.addEventListener('submit', function () { startProgress(); });
    window.addEventListener('pageshow', finishProgress);
    window.addEventListener('load', finishProgress);
    // 4. Table booking: Confirm stays grey until a time is chosen (with JavaScript on)
    document.querySelectorAll('form[data-slot-form]').forEach(function (form) {
        var confirm = form.querySelector('[data-confirm]');
        if (!confirm) return;
        var update = function () {
            confirm.disabled = !form.querySelector('input[name="Input.StartsAt"]:checked');
        };
        form.addEventListener('change', update);
        update();
    });
})();
