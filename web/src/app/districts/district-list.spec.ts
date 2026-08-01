import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DistrictList } from './district-list';
import { DistrictSummary } from './district.models';

describe('DistrictList', () => {
  function setup(): ComponentFixture<DistrictList> {
    TestBed.configureTestingModule({ imports: [DistrictList] });
    return TestBed.createComponent(DistrictList);
  }

  const copenhagen: DistrictSummary = {
    id: 3,
    name: 'Copenhagen',
    primary: { id: 3, name: 'Camilla' },
    storeCount: 5,
  };

  function text(fixture: ComponentFixture<DistrictList>): string {
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  it('shows a loading state', () => {
    const fixture = setup();
    fixture.componentRef.setInput('districts', []);
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();

    expect(text(fixture)).toContain('Loading');
  });

  it('shows an empty state when there are no districts', () => {
    const fixture = setup();
    fixture.componentRef.setInput('districts', []);
    fixture.componentRef.setInput('loading', false);
    fixture.detectChanges();

    expect(text(fixture)).toContain('No districts');
  });

  it('renders name, primary and store count for each district', () => {
    const fixture = setup();
    fixture.componentRef.setInput('districts', [copenhagen]);
    fixture.componentRef.setInput('loading', false);
    fixture.detectChanges();

    const rendered = text(fixture);
    expect(rendered).toContain('Copenhagen');
    expect(rendered).toContain('Camilla');
    expect(rendered).toContain('5 stores');
  });

  it('emits select with the district id when a row is clicked', () => {
    const fixture = setup();
    fixture.componentRef.setInput('districts', [copenhagen]);
    fixture.componentRef.setInput('loading', false);
    fixture.detectChanges();

    let emitted: number | undefined;
    fixture.componentInstance.select.subscribe((id) => (emitted = id));
    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('button.district')!.click();

    expect(emitted).toBe(3);
  });
});
