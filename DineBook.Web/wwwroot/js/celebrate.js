(function () {
    var COLORS = ['#a5b4fc', '#c4b5fd', '#f9a8d4', '#86efac', '#fde68a', '#7dd3fc'];
    var reduceMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // Rises a burst of soft bubbles over the page. Used after an order or payment is complete.
    window.celebrate = function (count) {
        if (reduceMotion) return;

        var total = count || 30;
        var layer = document.createElement('div');
        layer.className = 'bubble-layer';
        layer.setAttribute('aria-hidden', 'true');

        for (var i = 0; i < total; i++) {
            var bubble = document.createElement('span');
            bubble.className = 'bubble';

            var size = 14 + Math.random() * 40;
            bubble.style.width = size + 'px';
            bubble.style.height = size + 'px';
            bubble.style.left = (Math.random() * 100) + '%';
            bubble.style.background = 'radial-gradient(circle at 30% 30%, #ffffff 0%, ' +
                COLORS[i % COLORS.length] + ' 45%, ' + COLORS[(i + 2) % COLORS.length] + ' 100%)';
            bubble.style.setProperty('--drift', ((Math.random() * 60) - 30) + 'px');
            bubble.style.animationDuration = (2.6 + Math.random() * 2.2) + 's';
            bubble.style.animationDelay = (Math.random() * 0.9) + 's';

            layer.appendChild(bubble);
        }

        document.body.appendChild(layer);

        // Remove the layer once every bubble has floated away
        setTimeout(function () { layer.remove(); }, 5600);
    };

    // The server sets data-celebrate="true" on <body> after a completed order or payment
    if (document.body && document.body.dataset.celebrate === 'true') {
        window.celebrate();
    }
})();
