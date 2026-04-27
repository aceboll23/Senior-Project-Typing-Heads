/**
 * Tests for escapeHtml in search.js
 * Guards against unsafe rendering of usernames in the user-search dropdown.
 */

global.$ = global.jQuery = require('jquery');

const { escapeHtml } = require('../src/BoredGamers/wwwroot/js/search.js');

describe('escapeHtml', () => {
    test('plain text passes through unchanged', () => {
        expect(escapeHtml('hello')).toBe('hello');
    });

    test('less-than and greater-than characters are escaped', () => {
        expect(escapeHtml('<script>alert(1)</script>'))
            .toBe('&lt;script&gt;alert(1)&lt;/script&gt;');
    });

    test('ampersands are escaped', () => {
        expect(escapeHtml('Tom & Jerry')).toBe('Tom &amp; Jerry');
    });

    test('empty string returns empty string', () => {
        expect(escapeHtml('')).toBe('');
    });
});