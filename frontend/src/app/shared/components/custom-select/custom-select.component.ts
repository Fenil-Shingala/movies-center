import { 
  Component, 
  Input, 
  Output, 
  EventEmitter, 
  signal, 
  HostListener, 
  ElementRef, 
  inject,
  ChangeDetectionStrategy 
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideChevronDown, LucideCheck, LucideTv } from '@lucide/angular';

export interface SelectOption<T = any> {
  value: T;
  label: string;
  badge?: string | number;
  subtext?: string;
}

@Component({
  selector: 'app-custom-select',
  standalone: true,
  imports: [CommonModule, LucideChevronDown, LucideCheck, LucideTv],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './custom-select.component.html',
  styleUrls: ['./custom-select.component.scss']
})
export class CustomSelectComponent<T = any> {
  private readonly elementRef = inject(ElementRef);

  @Input() options: SelectOption<T>[] = [];
  @Input() value: T | null = null;
  @Input() placeholder = 'Select option';
  @Input() showIcon = true;

  @Output() valueChange = new EventEmitter<T>();

  readonly isOpen = signal<boolean>(false);

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.isOpen.set(false);
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.isOpen.set(false);
  }

  toggleDropdown(): void {
    this.isOpen.update(open => !open);
  }

  selectOption(opt: SelectOption<T>): void {
    this.value = opt.value;
    this.valueChange.emit(opt.value);
    this.isOpen.set(false);
  }

  getSelectedLabel(): string {
    const selected = this.options.find(o => o.value === this.value);
    return selected ? selected.label : this.placeholder;
  }

  getSelectedBadge(): string | number | undefined {
    const selected = this.options.find(o => o.value === this.value);
    return selected?.badge;
  }
}
