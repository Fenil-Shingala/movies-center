import { describe, it, expect, beforeEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { CustomSelectComponent, SelectOption } from './custom-select.component';

describe('CustomSelectComponent', () => {
  let component: CustomSelectComponent<number>;

  const options: SelectOption<number>[] = [
    { value: 1, label: 'Season 1', badge: '7 Ep' },
    { value: 2, label: 'Season 2', badge: '13 Ep' }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CustomSelectComponent]
    }).compileComponents();

    const fixture = TestBed.createComponent(CustomSelectComponent<number>);
    component = fixture.componentInstance;
    component.options = options;
    component.value = 1;
    fixture.detectChanges();
  });

  it('should create and display selected option label', () => {
    expect(component).toBeTruthy();
    expect(component.getSelectedLabel()).toBe('Season 1');
    expect(component.getSelectedBadge()).toBe('7 Ep');
  });

  it('should toggle dropdown open and close', () => {
    expect(component.isOpen()).toBe(false);
    component.toggleDropdown();
    expect(component.isOpen()).toBe(true);
    component.toggleDropdown();
    expect(component.isOpen()).toBe(false);
  });

  it('should select option and emit value', () => {
    let emittedVal: number | null = null;
    component.valueChange.subscribe(val => (emittedVal = val));

    component.selectOption(options[1]);
    expect(component.value).toBe(2);
    expect(emittedVal).toBe(2);
    expect(component.isOpen()).toBe(false);
  });
});
