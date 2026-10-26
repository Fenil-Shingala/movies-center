import { Component, inject, signal, ChangeDetectionStrategy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { 
  LucideX, 
  LucideMail, 
  LucideLock, 
  LucideUser, 
  LucideEye, 
  LucideEyeOff, 
  LucideLoader2, 
  LucideSparkles,
  LucideBookmark,
  LucideLogIn
} from '@lucide/angular';
import { AuthService } from '../../../core/services/auth.service';
import { GuestStorageService } from '../../../core/services/guest-storage.service';

@Component({
  selector: 'app-auth-modal',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    LucideX,
    LucideMail,
    LucideLock,
    LucideUser,
    LucideEye,
    LucideEyeOff,
    LucideLoader2,
    LucideSparkles,
    LucideBookmark,
    LucideLogIn
  ],
  templateUrl: './auth-modal.component.html',
  styleUrl: './auth-modal.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AuthModalComponent {
  readonly authService = inject(AuthService);
  readonly guestStorage = inject(GuestStorageService);
  private readonly fb = inject(FormBuilder);

  readonly isSubmitting = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly showPassword = signal<boolean>(false);

  readonly loginForm: FormGroup = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  readonly registerForm: FormGroup = this.fb.group({
    displayName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [
      Validators.required, 
      Validators.minLength(8),
      Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$/)
    ]],
    confirmPassword: ['', [Validators.required]]
  });

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.authService.isAuthModalOpen()) {
      this.close();
    }
  }

  setMode(mode: 'login' | 'register'): void {
    this.authService.authModalMode.set(mode);
    this.errorMessage.set(null);
  }

  togglePasswordVisibility(): void {
    this.showPassword.update(v => !v);
  }

  close(): void {
    this.errorMessage.set(null);
    this.authService.closeAuthModal();
  }

  onBackdropClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('modal-backdrop')) {
      this.close();
    }
  }

  submitLogin(): void {
    if (this.loginForm.invalid || this.isSubmitting()) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const { email, password } = this.loginForm.value;

    this.authService.login(email.trim(), password).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.loginForm.reset();
      },
      error: err => {
        this.isSubmitting.set(false);
        this.errorMessage.set(err?.error?.message ?? 'Invalid email or password. Please try again.');
      }
    });
  }

  submitRegister(): void {
    if (this.registerForm.invalid || this.isSubmitting()) {
      this.registerForm.markAllAsTouched();
      return;
    }

    const { displayName, email, password, confirmPassword } = this.registerForm.value;

    if (password !== confirmPassword) {
      this.errorMessage.set('Passwords do not match.');
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.authService.register(email.trim(), password, displayName.trim()).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.registerForm.reset();
      },
      error: err => {
        this.isSubmitting.set(false);
        this.errorMessage.set(err?.error?.message ?? 'Registration failed. Please check your details.');
      }
    });
  }
}
