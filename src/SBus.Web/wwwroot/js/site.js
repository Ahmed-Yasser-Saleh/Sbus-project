(function () {
    const form = document.getElementById('booking-form');

    if (form) {
        const price = parseFloat(form.dataset.price || '0');
        const max = parseInt(form.dataset.max || '4', 10);
        const currency = form.dataset.currency || '';
        const currencyFirst = form.dataset.currencyFirst === 'true';
        const separator = document.documentElement.dir === 'rtl' ? '، ' : ', ';
        const boxes = Array.from(form.querySelectorAll('.seat-free input[type=checkbox]'));
        const seatsOut = form.querySelector('[data-summary-seats]');
        const totalOut = form.querySelector('[data-summary-total]');

        const refresh = () => {
            const picked = boxes.filter(b => b.checked).map(b => b.value);
            boxes.forEach(b => { b.disabled = !b.checked && picked.length >= max; });
            const amount = (picked.length * price).toLocaleString('en-US');
            if (seatsOut) seatsOut.textContent = picked.length ? picked.join(separator) : '—';
            if (totalOut) totalOut.textContent = currencyFirst ? `${currency} ${amount}` : `${amount} ${currency}`;
        };

        boxes.forEach(b => b.addEventListener('change', refresh));
        refresh();
    }

    document.querySelectorAll('[data-countdown]').forEach(el => {
        const deadline = parseInt(el.dataset.countdown, 10);
        const out = el.querySelector('[data-countdown-text]');

        const tick = () => {
            const left = Math.max(0, Math.floor((deadline - Date.now()) / 1000));
            const m = Math.floor(left / 60);
            const s = String(left % 60).padStart(2, '0');
            out.textContent = left > 0 ? `${m}:${s} ${el.dataset.minutesLabel}` : el.dataset.expiredLabel;
            el.classList.toggle('expired', left === 0);
            if (left > 0) setTimeout(tick, 1000);
        };

        tick();
    });

    document.querySelectorAll('[data-copy]').forEach(btn => {
        btn.addEventListener('click', async () => {
            const input = document.querySelector(btn.dataset.copy);
            try {
                await navigator.clipboard.writeText(input.value);
            } catch {
                input.select();
                document.execCommand('copy');
            }
            const original = btn.textContent;
            btn.textContent = btn.dataset.copiedLabel;
            setTimeout(() => { btn.textContent = original; }, 1500);
        });
    });

    document.querySelectorAll('form[data-confirm]').forEach(f => {
        f.addEventListener('submit', e => {
            if (!confirm(f.dataset.confirm)) e.preventDefault();
        });
    });

    document.querySelectorAll('[data-print]').forEach(btn => btn.addEventListener('click', () => window.print()));

    document.querySelectorAll('img[data-fallback]').forEach(img => {
        img.addEventListener('error', () => {
            const label = document.createElement('span');
            label.textContent = img.dataset.fallback;
            img.replaceWith(label);
        });
    });
})();
