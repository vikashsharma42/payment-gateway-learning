(function () {
    // Stop double submits on the forms
    var form = document.querySelector('form[data-submit-once]');
    if (form) {
        form.addEventListener('submit', function () {
            var submit = form.querySelector('button[type="submit"]');
            if (submit) {
                submit.disabled = true;
                submit.textContent = 'Creating your payment…';
            }
        });
    }

    var btn = document.getElementById('payBtn');
    if (!btn) return;

    var status = document.getElementById('payStatus');
    var errorEl = document.getElementById('payError');

    // Back to the fallback button, with the reason in plain words
    function showFallback(message) {
        if (status) status.classList.add('d-none');
        btn.classList.remove('d-none');
        btn.disabled = false;
        if (errorEl) errorEl.textContent = message;
    }

    async function openPaymentPage() {
        if (errorEl) errorEl.textContent = '';
        if (typeof Cashfree === 'undefined') {
            showFallback('The payment library did not load. Check your connection and press the button to try again.');
            return;
        }

        btn.disabled = true;
        try {
            var cashfree = Cashfree({ mode: btn.dataset.mode });
            await cashfree.checkout({
                paymentSessionId: btn.dataset.session,
                redirectTarget: '_self'
            });
        } catch (e) {
            showFallback('We could not open the payment page. Press the button to try again.');
        }
    }

    btn.addEventListener('click', openPaymentPage);

    // One step for the customer: the secure payment page opens as soon as the page loads
    openPaymentPage();
})();
