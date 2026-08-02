import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';

const STORAGE_KEY = 'district-sale-theme';

describe('ThemeService', () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
  });

  /** Inject the (root) service and flush its reflect effect so the DOM/storage are up to date. */
  function create(): ThemeService {
    const service = TestBed.inject(ThemeService);
    TestBed.inject(ApplicationRef).tick();
    return service;
  }

  it('starts from the saved theme when one is stored', () => {
    localStorage.setItem(STORAGE_KEY, 'dark');
    expect(create().theme()).toBe('dark');
  });

  it('falls back to light when nothing is stored (jsdom has no OS preference)', () => {
    expect(create().theme()).toBe('light');
  });

  it('ignores a malformed stored value and falls back to light', () => {
    localStorage.setItem(STORAGE_KEY, 'chartreuse');
    expect(create().theme()).toBe('light');
  });

  it('toggle() flips between light and dark', () => {
    const service = create();
    expect(service.theme()).toBe('light');

    service.toggle();
    expect(service.theme()).toBe('dark');

    service.toggle();
    expect(service.theme()).toBe('light');
  });

  it('reflects the theme onto <html data-theme> and persists the choice', () => {
    const service = create();

    service.toggle(); // -> dark
    TestBed.inject(ApplicationRef).tick();

    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
    expect(localStorage.getItem(STORAGE_KEY)).toBe('dark');
  });
});
