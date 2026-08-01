import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DistrictDetailPane } from './district-detail-pane';
import { DistrictDetail } from './district.models';

describe('DistrictDetailPane', () => {
  function setup(): ComponentFixture<DistrictDetailPane> {
    TestBed.configureTestingModule({ imports: [DistrictDetailPane] });
    return TestBed.createComponent(DistrictDetailPane);
  }

  function text(fixture: ComponentFixture<DistrictDetailPane>): string {
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  const copenhagen: DistrictDetail = {
    id: 3,
    name: 'Copenhagen',
    primary: { id: 3, name: 'Camilla' },
    secondaries: [{ id: 2, name: 'Bjørn' }],
    stores: [{ id: 9, name: 'Nørrebro' }],
    concurrencyToken: 'AAAAAAAAB9E=',
  };

  it('prompts to select a district when nothing is loaded', () => {
    const fixture = setup();
    fixture.componentRef.setInput('detail', null);
    fixture.detectChanges();

    expect(text(fixture)).toContain('Select a district');
  });

  it('shows a loading state', () => {
    const fixture = setup();
    fixture.componentRef.setInput('detail', null);
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();

    expect(text(fixture)).toContain('Loading');
  });

  it('renders the primary, secondaries and stores', () => {
    const fixture = setup();
    fixture.componentRef.setInput('detail', copenhagen);
    fixture.detectChanges();

    const rendered = text(fixture);
    expect(rendered).toContain('Copenhagen');
    expect(rendered).toContain('Camilla');
    expect(rendered).toContain('Bjørn');
    expect(rendered).toContain('Nørrebro');
  });

  it('shows empty states for a district with no secondaries or stores', () => {
    const fixture = setup();
    fixture.componentRef.setInput('detail', { ...copenhagen, secondaries: [], stores: [] });
    fixture.detectChanges();

    const rendered = text(fixture);
    expect(rendered).toContain('No secondaries');
    expect(rendered).toContain('No stores');
  });
});
