(function () {
    if (!window.fetch || !window.AbortController || !window.history.pushState) return;

    const states = new Map();
    let pagePath = location.pathname;
    const regionFor = id => document.getElementById(id);
    const feedbackFor = id => document.querySelector(`[data-partial-error="${id}"]`);
    const statusFor = id => document.querySelector(`[data-partial-status="${id}"]`);
    const samePage = url => url.origin === location.origin && url.pathname === pagePath;

    function syncControls(id, url) {
        document.querySelectorAll(`[data-partial-controls="${id}"] a[data-partial-param]`).forEach(link => {
            const param = link.dataset.partialParam;
            const selected = url.searchParams.get(param) === link.dataset.partialValue;
            const destination = new URL(url);
            destination.searchParams.set(param, link.dataset.partialValue);
            link.href = destination.href;
            link.classList.toggle('active', selected);
            link.setAttribute('aria-selected', String(selected));
            if (param === 'date' && selected) link.setAttribute('aria-current', 'date');
            else link.removeAttribute('aria-current');
            link.tabIndex = selected ? 0 : -1;
        });
        document.querySelectorAll(`[data-partial-text="${id}"]`).forEach(el => {
            const values = JSON.parse(el.dataset.partialValues);
            const value = values[url.searchParams.get(el.dataset.partialParam)];
            if (value !== undefined) el.textContent = value;
        });
    }

    function remember(id, url, push) {
        const state = { ...history.state, sbusPartial: id };
        if (push && url.href !== location.href) history.pushState(state, '', url);
        else history.replaceState(state, '', url);
    }

    async function update(id, url, { method = 'GET', body, trigger, push = false } = {}) {
        const region = regionFor(id);
        if (!region || !samePage(url)) return;
        const previous = states.get(id);
        // A canceled POST can still commit on the server. Never interrupt or replay a write.
        if (previous?.pendingPost) return;
        previous?.controller.abort();
        const controller = new AbortController();
        const state = { controller, pendingPost: method === 'POST', retryUrl: url };
        states.set(id, state);
        const feedback = feedbackFor(id);
        const status = statusFor(id);
        const scroll = { x: scrollX, y: scrollY };
        const focused = document.activeElement;
        const focusId = focused?.id;
        const focusName = focused?.getAttribute('name');
        const buttons = method === 'POST' ? [...region.querySelectorAll('button[type="submit"], button:not([type]), input[type="submit"]')].filter(b => !b.disabled) : [];
        buttons.forEach(b => { b.disabled = true; });
        region.setAttribute('aria-busy', 'true');
        if (feedback) feedback.hidden = true;
        if (status) status.textContent = status.dataset.loadingLabel;
        try {
            if (method === 'GET') {
                syncControls(id, url);
                if (push) remember(id, url, true);
            }
            const response = await fetch(url, {
                method, body, signal: controller.signal, credentials: 'same-origin', cache: 'no-store',
                headers: { 'X-SBus-Region': id, 'X-Requested-With': 'XMLHttpRequest' }
            });
            if (states.get(id) !== state) return;
            const destination = new URL(response.url);
            if (response.status === 401 || response.status === 403 ||
                (response.redirected && (method !== 'POST' || !samePage(destination)))) {
                location.assign(destination.href);
                return;
            }
            if (!response.ok) throw new Error('Request failed');
            // Full HTML (including a login page after a challenge) must never enter a region.
            if (response.headers.get('X-SBus-Region') !== id) {
                if (response.redirected) {
                    location.assign(destination.href);
                    return;
                }
                throw new Error('Unexpected response');
            }
            const html = await response.text();
            if (states.get(id) !== state) return;
            const documentFragment = new DOMParser().parseFromString(html, 'text/html');
            const incoming = documentFragment.getElementById(id);
            if (!incoming || documentFragment.querySelector('script')) throw new Error('Invalid fragment');
            const canonical = new URL(incoming.dataset.partialUrl || destination.href, location.href);
            if (!samePage(canonical)) throw new Error('Invalid destination');
            // Keep the viewport stable when a shorter result would otherwise clamp scrollY.
            if (scroll.y > 0) region.style.minHeight = `${region.getBoundingClientRect().height}px`;
            region.dataset.partialUpdated = 'true';
            region.replaceChildren(...incoming.childNodes);
            region.dataset.partialUrl = canonical.href;
            syncControls(id, canonical);
            // A validation response keeps the current address; a same-page PRG uses its GET URL.
            if (method === 'GET' || response.redirected) remember(id, canonical, false);
            const errors = region.querySelector('.alert-danger, .validation-summary-errors, .field-validation-error');
            const success = region.querySelector('.alert-success');
            if (status) status.textContent = success?.textContent.trim() || incoming.dataset.partialAnnouncement || '';
            document.dispatchEvent(new CustomEvent('sbus:updated', { detail: { region } }));
            if (errors) {
                errors.tabIndex = -1;
                errors.focus({ preventScroll: true });
            } else if (!focused?.isConnected) {
                const replacement = (focusId && document.getElementById(focusId)) ||
                    (focusName && [...region.querySelectorAll('[name]')].find(el => el.name === focusName)) ||
                    (trigger?.tagName === 'A' && region.querySelector('form input:not([type="hidden"])')) || region;
                replacement.focus({ preventScroll: true });
            }
            window.scrollTo(scroll.x, scroll.y);
        } catch (error) {
            if (controller.signal.aborted || states.get(id) !== state) return;
            // A lost POST response has an unknown outcome: retry only a GET to inspect the data.
            state.retryUrl = method === 'POST' ? new URL(region.dataset.partialUrl || location.href, location.href) : url;
            if (feedback) {
                feedback.querySelector('[data-partial-error-text]').textContent = method === 'POST' ? feedback.dataset.postError : feedback.dataset.loadError;
                feedback.querySelector('[data-partial-retry]').textContent = method === 'POST' ? feedback.dataset.refreshLabel : feedback.dataset.retryLabel;
                feedback.hidden = false;
            }
            if (status) status.textContent = '';
        } finally {
            if (states.get(id) === state) {
                region.removeAttribute('aria-busy');
                state.pendingPost = false;
                buttons.forEach(b => { if (b.isConnected) b.disabled = false; });
            }
        }
    }

    document.addEventListener('click', event => {
        const retry = event.target.closest('[data-partial-retry]');
        if (retry) {
            const state = states.get(retry.dataset.partialRetry);
            if (state) update(retry.dataset.partialRetry, state.retryUrl, { trigger: retry });
            return;
        }
        const link = event.target.closest('a[data-partial]');
        if (!link || event.defaultPrevented || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey || link.hasAttribute('download') || (link.target && link.target !== '_self')) return;
        let url = new URL(link.href);
        if (!samePage(url) || !regionFor(link.dataset.partial)) return;
        event.preventDefault();
        if (link.dataset.partialParam) {
            url = new URL(location.href);
            url.searchParams.set(link.dataset.partialParam, link.dataset.partialValue);
        }
        update(link.dataset.partial, url, { trigger: link, push: true });
    });

    document.addEventListener('submit', event => {
        const form = event.target;
        if (!form.matches('form[data-partial]') || event.defaultPrevented || (form.target && form.target !== '_self')) return;
        const submitter = event.submitter;
        const method = (submitter?.getAttribute('formmethod') || form.method).toUpperCase();
        const url = new URL(submitter?.getAttribute('formaction') || form.action, location.href);
        if (!samePage(url) || !regionFor(form.dataset.partial) || !['GET', 'POST'].includes(method)) return;
        event.preventDefault();
        const data = new FormData(form);
        if (submitter?.name) data.append(submitter.name, submitter.value);
        if (method === 'GET') {
            const query = new URLSearchParams(data);
            url.search = query.toString();
        }
        update(form.dataset.partial, url, { method, body: method === 'POST' ? data : undefined, trigger: submitter, push: method === 'GET' });
    });

    document.addEventListener('keydown', event => {
        const tab = event.target.closest('[role="tab"][data-partial]');
        if (!tab || !['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return;
        const tabs = [...tab.closest('[role="tablist"]').querySelectorAll('[role="tab"]')];
        const step = (event.key === 'ArrowRight' ? 1 : -1) * (document.documentElement.dir === 'rtl' ? -1 : 1);
        const index = event.key === 'Home' ? 0 : event.key === 'End' ? tabs.length - 1 : (tabs.indexOf(tab) + step + tabs.length) % tabs.length;
        event.preventDefault();
        tabs[index].focus({ preventScroll: true });
        tabs[index].click();
    });

    const initialRegion = document.querySelector('[data-partial-region]');
    if (initialRegion) {
        history.scrollRestoration = 'manual';
        const initialUrl = new URL(initialRegion.dataset.partialUrl || location.href, location.href);
        remember(initialRegion.id, initialUrl, false);
        pagePath = initialUrl.pathname;
        syncControls(initialRegion.id, initialUrl);
        window.addEventListener('popstate', event => {
            const id = event.state?.sbusPartial || initialRegion.id;
            if (!samePage(new URL(location.href)) || !regionFor(id) || states.get(id)?.pendingPost) location.reload();
            else update(id, new URL(location.href));
        });
    }
})();
