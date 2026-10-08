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

    const mirrors = document.querySelectorAll('[data-countdown-mirror]');

    document.querySelectorAll('[data-countdown]').forEach(el => {
        const deadline = parseInt(el.dataset.countdown, 10);
        const out = el.querySelector('[data-countdown-text]');

        const tick = () => {
            const left = Math.max(0, Math.floor((deadline - Date.now()) / 1000));
            const m = Math.floor(left / 60);
            const s = String(left % 60).padStart(2, '0');
            const text = left > 0 ? `${m}:${s} ${el.dataset.minutesLabel}` : el.dataset.expiredLabel;
            out.textContent = text;
            mirrors.forEach(mirror => { mirror.textContent = text; });
            el.classList.toggle('expired', left === 0);
            if (left > 0) setTimeout(tick, 1000);
        };

        tick();
    });

    const flashCopied = btn => {
        const label = btn.querySelector('span') || btn;
        const original = label.textContent;
        label.textContent = btn.dataset.copiedLabel;
        setTimeout(() => { label.textContent = original; }, 1500);
    };

    const copy = async (text, fallbackInput) => {
        try {
            await navigator.clipboard.writeText(text);
        } catch {
            if (fallbackInput) {
                fallbackInput.select();
                document.execCommand('copy');
            }
        }
    };

    document.querySelectorAll('[data-copy]').forEach(btn => {
        btn.addEventListener('click', async () => {
            const input = document.querySelector(btn.dataset.copy);
            await copy(input.value, input);
            flashCopied(btn);
        });
    });

    document.querySelectorAll('[data-copy-text]').forEach(btn => {
        btn.addEventListener('click', async () => {
            await copy(document.querySelector(btn.dataset.copyText).textContent.trim());
            flashCopied(btn);
        });
    });

    document.querySelectorAll('input[type=file][data-file-label]').forEach(input => {
        input.addEventListener('change', () => {
            const label = document.querySelector(input.dataset.fileLabel);
            const file = input.files && input.files[0];
            if (file && label) label.textContent = file.name;
            input.closest('.dropzone')?.classList.toggle('has-file', !!file);
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
