(function () {
    // Menu helpers: search, quantity steppers (− / +), and a live total in the order bar.
    var search = document.getElementById('menuSearch');
    var bar = document.getElementById('orderBar');
    if (!bar) return;

    var countEl = document.getElementById('orderCount');
    var totalEl = document.getElementById('orderTotal');
    var placeBtn = document.getElementById('placeOrder');
    var fee = parseFloat(bar.dataset.fee) || 0;
    var taxPercent = parseFloat(bar.dataset.tax) || 0;
    var taxType = bar.dataset.taxType || 'GST';
    function round2(n) { return Math.round(n * 100) / 100; }
    var inr = new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR' });

    function quantities() {
        return Array.prototype.map.call(document.querySelectorAll('.qty-input'), function (input) {
            var card = input.closest('.menu-card-col');
            return {
                qty: Math.max(0, parseInt(input.value, 10) || 0),
                price: parseFloat(card.dataset.price) || 0,
                label: card.dataset.label
            };
        });
    }

    function refresh() {
        var lines = quantities().filter(function (l) { return l.qty > 0; });
        var items = lines.reduce(function (sum, l) { return sum + l.qty; }, 0);
        var total = lines.reduce(function (sum, l) { return sum + l.qty * l.price; }, 0);

        // Same formula as BillCalculator: GST applies to items + system access fee
        var tax = round2((total + fee) * taxPercent / 100);
        var grand = round2(total + fee + tax);

        if (items === 0) {
            countEl.textContent = 'No dishes selected';
            totalEl.textContent = 'Choose quantities to see your total';
            placeBtn.disabled = true;
        } else {
            countEl.textContent = items + (items === 1 ? ' dish' : ' dishes') + ' · Total ' + inr.format(grand);
            totalEl.textContent = 'Items ' + inr.format(total) + ' + system fee ' + inr.format(fee)
                + ' + ' + taxType + ' ' + taxPercent + '% ' + inr.format(tax);
            placeBtn.disabled = false;
        }

        // Highlight the dishes in the order
        document.querySelectorAll('.qty-input').forEach(function (input) {
            input.closest('.dish-card').classList.toggle('in-order', parseInt(input.value, 10) > 0);
        });
    }

    // Add − / + buttons around every quantity box
    document.querySelectorAll('.qty-input').forEach(function (input) {
        var wrap = document.createElement('div');
        wrap.className = 'qty-stepper';

        var minus = document.createElement('button');
        minus.type = 'button';
        minus.className = 'qty-btn';
        minus.setAttribute('aria-label', 'Remove one ' + input.closest('.menu-card-col').dataset.label);
        minus.textContent = '−';

        var plus = document.createElement('button');
        plus.type = 'button';
        plus.className = 'qty-btn qty-btn--plus';
        plus.setAttribute('aria-label', 'Add one ' + input.closest('.menu-card-col').dataset.label);
        plus.textContent = '+';

        input.parentNode.insertBefore(wrap, input);
        wrap.appendChild(minus);
        wrap.appendChild(input);
        wrap.appendChild(plus);
        input.readOnly = true;
        input.tabIndex = -1;

        function change(delta) {
            var next = Math.min(20, Math.max(0, (parseInt(input.value, 10) || 0) + delta));
            input.value = next;
            refresh();
        }
        minus.addEventListener('click', function () { change(-1); });
        plus.addEventListener('click', function () { change(1); });
    });

    // Search filters dishes; hidden dishes keep their quantities
    if (search) {
        search.addEventListener('input', function () {
            var term = search.value.trim().toLowerCase();
            document.querySelectorAll('.menu-card-col').forEach(function (card) {
                card.classList.toggle('d-none', card.dataset.name.indexOf(term) === -1);
            });
            document.querySelectorAll('.menu-section').forEach(function (section) {
                section.classList.toggle('d-none', !section.querySelector('.menu-card-col:not(.d-none)'));
            });
        });
    }

    refresh();
})();
