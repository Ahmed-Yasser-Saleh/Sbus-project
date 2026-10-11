const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function fixture() {
    const events = {};
    const attributes = { 'aria-expanded': 'false' };
    let focused = false;
    const link = { addEventListener(type, handler) { events[`link:${type}`] = handler; } };
    const bar = {
        classList: { add() {} },
        addEventListener(type, handler) { events[`bar:${type}`] = handler; },
        querySelectorAll() { return [link]; }
    };
    const toggle = {
        hidden: true,
        closest() { return bar; },
        getAttribute(key) { return attributes[key]; },
        setAttribute(key, value) { attributes[key] = value; },
        addEventListener(type, handler) { events[`toggle:${type}`] = handler; },
        focus() { focused = true; }
    };
    const document = {
        querySelector() { return null; },
        querySelectorAll(selector) { return selector === '[data-nav-toggle]' ? [toggle] : []; },
        addEventListener() {}
    };
    vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../../src/SBus.Web/wwwroot/js/site.js'), 'utf8'), { document });
    return { events, toggle, attributes, hasFocus: () => focused };
}

test('Mobile navigation opens and closes with accurate expanded state', () => {
    const f = fixture();
    assert.equal(f.toggle.hidden, false);
    f.events['toggle:click']();
    assert.equal(f.attributes['aria-expanded'], 'true');
    f.events['toggle:click']();
    assert.equal(f.attributes['aria-expanded'], 'false');
});

test('Escape restores toggle focus and navigation links dismiss the menu', () => {
    const f = fixture();
    f.events['toggle:click']();
    f.events['bar:keydown']({ key: 'Escape' });
    assert.equal(f.attributes['aria-expanded'], 'false');
    assert.equal(f.hasFocus(), true);
    f.events['toggle:click']();
    f.events['link:click']();
    assert.equal(f.attributes['aria-expanded'], 'false');
});
