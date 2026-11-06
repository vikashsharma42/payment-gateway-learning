(function () {
    // Marks an email input as taken when an account already uses it.
    // Inputs opt in with data-check-url="<handler url>". The server still checks on submit.
    function clearFeedback(input) {
        input.classList.remove('is-invalid');
        var old = input.parentElement.querySelector('[data-email-feedback]');
        if (old) old.remove();
    }

    function showTaken(input) {
        input.classList.add('is-invalid');
        var fb = document.createElement('div');
        fb.className = 'invalid-feedback d-block';
        fb.setAttribute('data-email-feedback', '');
        fb.textContent = 'An account with this email already exists.';
        input.insertAdjacentElement('afterend', fb);
    }

    function check(input) {
        var value = input.value.trim();
        clearFeedback(input);
        if (!value || input.validity.typeMismatch) return;

        fetch(input.dataset.checkUrl + '&email=' + encodeURIComponent(value), {
            credentials: 'same-origin',
            headers: { 'Accept': 'application/json' }
        })
            .then(function (r) { return r.ok ? r.json() : null; })
            .then(function (data) {
                // Ignore a reply for an email the user has already changed
                if (!data || data.available || input.value.trim() !== value) return;
                showTaken(input);
            })
            .catch(function () { /* the server checks again on submit */ });
    }

    document.querySelectorAll('input[data-check-url]').forEach(function (input) {
        input.addEventListener('blur', function () { check(input); });
        input.addEventListener('input', function () { clearFeedback(input); });
    });
})();
