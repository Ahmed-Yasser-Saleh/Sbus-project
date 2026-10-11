(function () {
    function phoneIsValid(value) {
        let number = value.replace(/[٠-٩]/g, c => c.charCodeAt(0) - 0x660)
            .replace(/[۰-۹]/g, c => c.charCodeAt(0) - 0x6f0);
        if (!/^[0-9 +()\-]+$/.test(number) || number.length > 30) return false;
        number = number.replace(/[ +()\-]/g, '');
        if (number.startsWith('0020')) number = '0' + number.slice(4);
        else if (number.startsWith('20') && number.length === 12) number = '0' + number.slice(2);
        return /^01[0125][0-9]{8}$/.test(number);
    }

    function validatePhone(input) {
        input.setCustomValidity(phoneIsValid(input.value) ? '' : input.dataset.phoneError);
    }

    function showError(input, output, message) {
        input.setCustomValidity(message);
        input.setAttribute('aria-invalid', message ? 'true' : 'false');
        output.textContent = message;
        output.classList.toggle('field-validation-error', !!message);
        output.classList.toggle('field-validation-valid', !message);
    }

    function validatePassenger(card, showMessages = true) {
        const editing = card.querySelector('[data-passenger-editing]').value.toLowerCase() === 'true';
        const name = card.querySelector('[data-passenger-name]');
        const phone = card.querySelector('[data-traveler-phone]');
        const nameError = !name.value.trim() ? card.dataset.nameRequired :
            name.value.length > name.maxLength ? card.dataset.nameTooLong : '';
        const phoneError = phoneIsValid(phone.value) ? '' : phone.dataset.phoneError;
        if (showMessages) {
            showError(name, card.querySelector('[data-passenger-name-error]'), editing ? nameError : '');
            showError(phone, card.querySelector('[data-passenger-phone-error]'), editing ? phoneError : '');
        }
        const button = card.closest('form').querySelector('[data-book-seat]');
        const pending = card.closest('[data-partial-region]')?.getAttribute('aria-busy') === 'true';
        button.disabled = pending || (editing && !!(nameError || phoneError));
        return !editing || !(nameError || phoneError);
    }

    function initialize(root) {
        root.querySelectorAll('[data-traveler-phone]').forEach(validatePhone);
        root.querySelectorAll('[data-booking-passenger]').forEach(card => {
            // Keep server messages until the traveler changes a value.
            validatePassenger(card, false);
        });
    }

    document.addEventListener('click', event => {
        const edit = event.target.closest('[data-passenger-edit]');
        const card = edit?.closest('[data-booking-passenger]');
        if (!card || card.closest('[data-partial-region]')?.getAttribute('aria-busy') === 'true') return;
        const field = edit.dataset.passengerEdit;
        const input = card.querySelector(field === 'name' ? '[data-passenger-name]' : '[data-traveler-phone]');
        input.readOnly = false;
        card.querySelector(`[data-passenger-${field}-editing]`).value = 'true';
        card.querySelector('[data-passenger-editing]').value = 'true';
        card.querySelector('[data-passenger-account-option]').hidden = false;
        card.querySelector('[data-passenger-update-account]').disabled = false;
        validatePassenger(card);
        input.focus({ preventScroll: true });
    });

    document.addEventListener('input', event => {
        if (event.target.matches('[data-traveler-phone]')) validatePhone(event.target);
        const card = event.target.closest('[data-booking-passenger]');
        if (card) validatePassenger(card);
    });

    document.addEventListener('submit', event => {
        event.target.querySelectorAll('[data-traveler-phone]').forEach(validatePhone);
        const card = event.target.querySelector('[data-booking-passenger]');
        if ((card && !validatePassenger(card)) || !event.target.reportValidity()) event.preventDefault();
    }, true);

    document.addEventListener('sbus:updated', event => initialize(event.detail.region));
    initialize(document);
})();
