const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function fixture(editing = false) {
    const listeners = {};
    const element = extra => ({
        value: '', readOnly: !editing, hidden: false, disabled: false, checked: false,
        dataset: {}, classList: { toggle() {} }, setAttribute(key, value) { this[key] = value; },
        setCustomValidity(message) { this.error = message; }, focus() {},
        ...extra
    });
    const name = element({ value: 'Profile Name', maxLength: 100 });
    const phone = element({ value: '+201012345678', dataset: { phoneError: 'Invalid phone' } });
    const inputs = {
        '[data-passenger-editing]': element({ value: String(editing) }),
        '[data-passenger-name]': name,
        '[data-traveler-phone]': phone,
        '[data-passenger-edit-name]': element({ dataset: { passengerEdit: 'name' } }),
        '[data-passenger-edit-phone]': element({ dataset: { passengerEdit: 'phone' } }),
        '[data-passenger-name-editing]': element({ value: String(editing) }),
        '[data-passenger-phone-editing]': element({ value: String(editing) }),
        '[data-passenger-account-option]': element({ hidden: !editing }),
        '[data-passenger-update-account]': element({ disabled: !editing }),
        '[data-passenger-name-error]': element(),
        '[data-passenger-phone-error]': element(),
        '[data-book-seat]': element()
    };
    const selections = { seats: [1, 2], pickup: 'Cairo', dropoff: 'Suez', date: '2030-01-01' };
    const region = { getAttribute() { return null; } };
    const form = {
        querySelector(selector) { return selector === '[data-booking-passenger]' ? card : inputs[selector]; },
        querySelectorAll() { return [phone]; }, reportValidity() { return true; }, selections
    };
    const card = {
        dataset: { profileName: name.value, profilePhone: phone.value, nameRequired: 'Name required', nameTooLong: 'Name too long' },
        querySelector(selector) { return inputs[selector]; },
        closest(selector) { return selector === 'form' ? form : region; }
    };
    for (const [selector, input] of Object.entries(inputs)) {
        input.closest = target => target === '[data-booking-passenger]' ? card :
            target === '[data-passenger-edit]' && input.dataset.passengerEdit ? input : target === selector ? input : null;
        input.matches = target => target === selector;
    }
    const document = {
        addEventListener(event, action) { (listeners[event] ||= []).push(action); },
        querySelectorAll(selector) { return selector === '[data-booking-passenger]' ? [card] : [phone]; }
    };
    vm.runInNewContext(fs.readFileSync(path.resolve(__dirname, '../../src/SBus.Web/wwwroot/js/traveler-profile.js'), 'utf8'), { document });
    const dispatch = (event, target) => {
        const request = { target, preventDefault() { this.prevented = true; } };
        listeners[event].forEach(action => action(request));
        return request;
    };
    return { inputs, form, dispatch, selections, refresh() { listeners['sbus:updated'].forEach(action => action({ detail: { region: document } })); } };
}

test('Each pencil unlocks only its own field and preserves booking selections and account opt-in', () => {
    const { inputs, dispatch, selections } = fixture();
    const original = JSON.stringify(selections);
    dispatch('click', inputs['[data-passenger-edit-name]']);
    assert.equal(inputs['[data-passenger-name]'].readOnly, false);
    assert.equal(inputs['[data-traveler-phone]'].readOnly, true);
    assert.equal(inputs['[data-passenger-name-editing]'].value, 'true');
    assert.equal(inputs['[data-passenger-update-account]'].checked, false);
    assert.equal(inputs['[data-passenger-account-option]'].hidden, false);
    inputs['[data-passenger-name]'].value = 'Family Member';
    inputs['[data-passenger-update-account]'].checked = true;
    dispatch('click', inputs['[data-passenger-edit-phone]']);
    assert.equal(inputs['[data-traveler-phone]'].readOnly, false);
    assert.equal(inputs['[data-passenger-phone-editing]'].value, 'true');
    assert.equal(inputs['[data-passenger-name]'].value, 'Family Member');
    assert.equal(inputs['[data-passenger-update-account]'].checked, true);
    assert.equal(inputs['[data-passenger-update-account]'].disabled, false);
    assert.equal(JSON.stringify(selections), original);
});

test('Invalid edit values show inline errors, disable booking, and block submission', () => {
    const { inputs, form, dispatch } = fixture(true);
    inputs['[data-passenger-name]'].value = ' ';
    inputs['[data-traveler-phone]'].value = '12345';
    dispatch('input', inputs['[data-traveler-phone]']);
    assert.equal(inputs['[data-book-seat]'].disabled, true);
    assert.equal(inputs['[data-passenger-name-error]'].textContent, 'Name required');
    assert.equal(inputs['[data-passenger-phone-error]'].textContent, 'Invalid phone');
    assert.equal(dispatch('submit', form).prevented, true);
    inputs['[data-passenger-name]'].value = 'Valid Name';
    inputs['[data-traveler-phone]'].value = '01012345678';
    dispatch('input', inputs['[data-traveler-phone]']);
    assert.equal(inputs['[data-book-seat]'].disabled, false);
    assert.equal(inputs['[data-passenger-phone-error]'].textContent, '');
});

for (const phone of ['01012345678', '+20 101-234-5678', '00201012345678', '٠١٠١٢٣٤٥٦٧٨', '۰۱۰۱۲۳۴۵۶۷۸']) {
    test(`Valid Egyptian phone ${phone} allows booking`, () => {
        const { inputs, dispatch } = fixture(true);
        inputs['[data-traveler-phone]'].value = phone;
        dispatch('input', inputs['[data-traveler-phone]']);
        assert.equal(inputs['[data-book-seat]'].disabled, false);
        assert.equal(inputs['[data-passenger-phone-error]'].textContent, '');
    });
}

test('Fragment initialization keeps edit mode and validation active', () => {
    const { inputs, dispatch, refresh } = fixture(true);
    assert.equal(inputs['[data-passenger-editing]'].value, 'true');
    inputs['[data-passenger-name]'].value = 'x'.repeat(101);
    refresh();
    assert.equal(inputs['[data-book-seat]'].disabled, true);
    dispatch('input', inputs['[data-passenger-name]']);
    assert.equal(inputs['[data-book-seat]'].disabled, true);
    assert.equal(inputs['[data-passenger-name-error]'].textContent, 'Name too long');
});
