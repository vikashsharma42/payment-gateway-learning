// Phone fields: a country picker with its flag, and the national number grouped the way that country writes it.
// A number typed with a leading + is left as typed; the server checks it against the chosen country.
(function () {
    'use strict';

    function digitsOf(text) {
        return (text || '').replace(/\D/g, '');
    }

    function groupsOf(option) {
        var raw = option.getAttribute('data-groups') || '';
        return raw ? raw.split(',').map(function (n) { return parseInt(n, 10); }) : [];
    }

    // 9876543210 with groups 5,5 -> "98765 43210"
    function group(digits, groups) {
        var parts = [];
        var index = 0;
        groups.forEach(function (size) {
            if (index < digits.length) {
                parts.push(digits.substr(index, size));
                index += size;
            }
        });
        if (index < digits.length) {
            parts.push(digits.substr(index));
        }
        return parts.join(' ');
    }

    function setup(field) {
        var select = field.querySelector('.phone-country');
        var input = field.querySelector('.phone-national');
        if (!select || !input) {
            return;
        }

        function reformat() {
            var option = select.options[select.selectedIndex];
            input.placeholder = option.getAttribute('data-example') || '';

            if (input.value.trim().charAt(0) === '+') {
                return;
            }

            var max = parseInt(option.getAttribute('data-max'), 10) || 15;
            input.value = group(digitsOf(input.value).slice(0, max), groupsOf(option));
        }

        select.addEventListener('change', reformat);
        input.addEventListener('input', reformat);
        reformat();
    }

    document.querySelectorAll('.phone-field').forEach(setup);
})();
