(function () {
    var host = document.getElementById('toast-host');

    // Shows a Bootstrap toast. type: "success", "danger", "warning" or "info"
    window.showToast = function (type, message) {
        if (!host || !window.bootstrap) return;

        var el = document.createElement('div');
        el.className = 'toast align-items-center text-bg-' + type + ' border-0';
        el.setAttribute('role', type === 'danger' ? 'alert' : 'status');
        el.innerHTML =
            '<div class="d-flex">' +
                '<div class="toast-body"></div>' +
                '<button type="button" class="btn-close btn-close-white me-2 m-auto" ' +
                        'data-bs-dismiss="toast" aria-label="Close"></button>' +
            '</div>';
        el.querySelector('.toast-body').textContent = message;

        host.appendChild(el);
        el.addEventListener('hidden.bs.toast', function () { el.remove(); });
        new bootstrap.Toast(el, { delay: 5000 }).show();
    };

    // Show a toast that the server set in TempData
    if (host && host.dataset.toast) {
        try {
            var toast = JSON.parse(host.dataset.toast);
            window.showToast(toast.Type, toast.Message);
        } catch (e) {
            // Ignore malformed data
        }
    }
})();