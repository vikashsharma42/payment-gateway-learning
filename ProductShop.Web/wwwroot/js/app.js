(function () {
    var MAX_VISIBLE = 4;
    var host = document.getElementById('toast-host');
    if (!host) return;

    // Keep toasts in the top-right corner, above page content, and announced to screen readers
    host.classList.add('toast-container', 'position-fixed', 'top-0', 'end-0', 'p-3');
    host.setAttribute('aria-live', 'polite');
    host.setAttribute('aria-atomic', 'false');

    var ICONS = {
        success: '✅',   // green check
        danger: '⚠️', // warning sign
        warning: '⚠️',
        info: 'ℹ️'    // info
    };

    // Errors stay a little longer so people can read them
    var DELAYS = { success: 4000, danger: 7000, warning: 6000, info: 4000 };

    // Hides the oldest toasts so that one more fits within MAX_VISIBLE
    function trimOldest() {
        var toasts = host.querySelectorAll('.toast');
        var excess = toasts.length - MAX_VISIBLE + 1;
        for (var i = 0; i < excess; i++) {
            var instance = bootstrap.Toast.getInstance(toasts[i]);
            if (instance) instance.hide();
            else toasts[i].remove();
        }
    }

    // Shows a toast. type: "success", "danger", "warning" or "info"
    window.showToast = function (type, message) {
        if (!window.bootstrap || !message) return;
        if (!ICONS[type]) type = 'info';

        // Skip an identical message that is already on screen
        var existing = host.querySelectorAll('.toast[data-message]');
        for (var i = 0; i < existing.length; i++) {
            if (existing[i].dataset.message === message && existing[i].classList.contains('show')) return;
        }

        trimOldest();

        var el = document.createElement('div');
        el.className = 'toast align-items-center border-0 shadow';
        el.classList.add(type === 'success' ? 'text-bg-success' : type === 'warning' ? 'text-bg-warning' : type === 'info' ? 'text-bg-info' : 'text-bg-danger');
        el.setAttribute('role', type === 'danger' ? 'alert' : 'status');
        el.setAttribute('aria-live', type === 'danger' ? 'assertive' : 'polite');
        el.dataset.message = message;

        var row = document.createElement('div');
        row.className = 'd-flex';

        var body = document.createElement('div');
        body.className = 'toast-body d-flex align-items-start gap-2 fw-semibold';

        var icon = document.createElement('span');
        icon.setAttribute('aria-hidden', 'true');
        icon.textContent = ICONS[type];

        var text = document.createElement('span');
        text.textContent = message;

        body.appendChild(icon);
        body.appendChild(text);

        var close = document.createElement('button');
        close.type = 'button';
        close.className = 'btn-close btn-close-white me-2 m-auto';
        close.setAttribute('data-bs-dismiss', 'toast');
        close.setAttribute('aria-label', 'Close');

        row.appendChild(body);
        row.appendChild(close);
        el.appendChild(row);

        host.appendChild(el);

        el.addEventListener('hidden.bs.toast', function () { el.remove(); });

        var instance = new bootstrap.Toast(el, { delay: DELAYS[type] || 5000, autohide: true });
        instance.show();
    };

    // Toast set by the server in TempData (see AppPageModel)
    if (host.dataset.toast) {
        try {
            var toast = JSON.parse(host.dataset.toast);
            window.showToast(toast.Type, toast.Message);
        } catch (e) {
            // Ignore malformed data
        }
    }
})();