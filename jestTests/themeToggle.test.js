/**
 * Tests for theme toggle logic (TYP-210)
 * Covers: toggleTheme() and applyStoredTheme()
 */

const { toggleTheme, applyStoredTheme } = require('../src/BoredGamers/wwwroot/js/theme.js');

describe('toggleTheme', () => {
  beforeEach(() => {
    document.documentElement.removeAttribute('data-theme');
    localStorage.clear();
  });

  test('switches to light theme when currently in dark mode', () => {
    toggleTheme();
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
    expect(localStorage.getItem('theme')).toBe('light');
  });

  test('switches back to dark theme when currently in light mode', () => {
    document.documentElement.setAttribute('data-theme', 'light');
    localStorage.setItem('theme', 'light');

    toggleTheme();

    expect(document.documentElement.getAttribute('data-theme')).toBeNull();
    expect(localStorage.getItem('theme')).toBeNull();
  });
});

describe('applyStoredTheme', () => {
  beforeEach(() => {
    document.documentElement.removeAttribute('data-theme');
    localStorage.clear();
  });

  test('applies light theme if localStorage has "light"', () => {
    localStorage.setItem('theme', 'light');
    applyStoredTheme();
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
  });

  test('leaves dark theme if localStorage is empty', () => {
    applyStoredTheme();
    expect(document.documentElement.getAttribute('data-theme')).toBeNull();
  });
});
