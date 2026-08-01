import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DistrictDetailPane } from './district-detail-pane';
import { DistrictDetail, Salesperson } from './district.models';

describe('DistrictDetailPane', () => {
  const pool: Salesperson[] = [
    { id: 3, name: 'Camilla' },
    { id: 2, name: 'Bjørn' },
    { id: 4, name: 'David' },
    { id: 6, name: 'Freja' },
  ];

  const copenhagen: DistrictDetail = {
    id: 3,
    name: 'Copenhagen',
    primary: { id: 3, name: 'Camilla' },
    secondaries: [{ id: 2, name: 'Bjørn' }],
    stores: [{ id: 9, name: 'Nørrebro' }],
    concurrencyToken: 'AAAAAAAAB9E=',
  };

  function setup(detail: DistrictDetail | null = copenhagen): ComponentFixture<DistrictDetailPane> {
    TestBed.configureTestingModule({ imports: [DistrictDetailPane] });
    const fixture = TestBed.createComponent(DistrictDetailPane);
    fixture.componentRef.setInput('detail', detail);
    fixture.componentRef.setInput('salespersons', pool);
    fixture.detectChanges();
    return fixture;
  }

  function el(fixture: ComponentFixture<DistrictDetailPane>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  it('prompts to select a district when nothing is loaded', () => {
    const fixture = setup(null);
    expect(el(fixture).textContent).toContain('Select a district');
  });

  it('shows a loading state', () => {
    const fixture = setup(null);
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();
    expect(el(fixture).textContent).toContain('Loading');
  });

  it('renders the primary, secondaries and stores', () => {
    const rendered = el(setup()).textContent ?? '';
    expect(rendered).toContain('Copenhagen');
    expect(rendered).toContain('Camilla');
    expect(rendered).toContain('Bjørn');
    expect(rendered).toContain('Nørrebro');
  });

  it('shows empty states for a district with no secondaries or stores', () => {
    const rendered = el(setup({ ...copenhagen, secondaries: [], stores: [] })).textContent ?? '';
    expect(rendered).toContain('No secondaries');
    expect(rendered).toContain('No stores');
  });

  it('offers only salespersons not already assigned in the add list', () => {
    const options = Array.from(el(setup()).querySelectorAll('option')).map((o) => o.textContent?.trim());
    expect(options).toContain('David'); // free
    expect(options).toContain('Freja'); // free
    expect(options).not.toContain('Camilla'); // primary
    expect(options).not.toContain('Bjørn'); // already a secondary
  });

  it('emits addSecondary for the chosen salesperson', () => {
    const fixture = setup();
    let added: number | undefined;
    fixture.componentInstance.addSecondary.subscribe((id) => (added = id));

    const select = el(fixture).querySelector('select') as HTMLSelectElement;
    select.value = '4';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    el(fixture).querySelector<HTMLButtonElement>('.add-btn')!.click();

    expect(added).toBe(4);
  });

  it('emits removeSecondary when a secondary is removed', () => {
    const fixture = setup();
    let removed: number | undefined;
    fixture.componentInstance.removeSecondary.subscribe((id) => (removed = id));

    el(fixture).querySelector<HTMLButtonElement>('.remove-btn')!.click();

    expect(removed).toBe(2);
  });

  it('confirms a change of primary (explaining the current primary steps down) before emitting', () => {
    const fixture = setup();
    let promoted: number | undefined;
    fixture.componentInstance.makePrimary.subscribe((id) => (promoted = id));

    el(fixture).querySelector<HTMLButtonElement>('.make-primary-btn')!.click();
    fixture.detectChanges();

    const confirm = el(fixture).querySelector('.confirm');
    expect(confirm?.textContent).toContain('step down'); // explains what happens to Camilla
    expect(confirm?.textContent).toContain('Camilla');
    expect(promoted).toBeUndefined(); // not yet — awaiting confirmation

    el(fixture).querySelector<HTMLButtonElement>('.confirm-btn')!.click();
    expect(promoted).toBe(2);
  });

  it('does not offer to remove the primary (blocked in the UI)', () => {
    const dom = el(setup());
    // One secondary => exactly one remove control; the primary has none.
    expect(dom.querySelectorAll('.remove-btn').length).toBe(1);
    expect(dom.textContent).toContain('be removed'); // the "primary can't be removed" hint
  });
});
