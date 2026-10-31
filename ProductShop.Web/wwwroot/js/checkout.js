(function () {
    // Stop double submits on the details form
    var form = document.querySelector('form[data-submit-once]');
    if (form) {
        form.addEventListener('submit', function () {
            var submit = form.querySelector('button[type="submit"]');
            if (submit) {
                submit.disabled = true;
                submit.textContent = 'Creating your order…';
            }
        });
    }

    // Open Cashfree checkout for the stored session
    var btn = document.getElementById('payBtn');
    if (!btn) return;

    var label = btn.textContent;

    btn.addEventListener('click', async function () {
        if (typeof Cashfree === 'undefined') {
            showToast('danger', 'The payment library did not load. Refresh the page and try again.');
            return;
        }

        btn.disabled = true;
        btn.textContent = 'Opening secure checkout…';

        try {
            var cashfree = Cashfree({ mode: btn.dataset.mode });
            await cashfree.checkout({
                paymentSessionId: btn.dataset.session,
                redirectTarget: '_self'
            });
        } catch (e) {
            btn.disabled = false;
            btn.textContent = label;
            showToast('danger', 'Could not open checkout. Please try again.');
        }
    });
})();