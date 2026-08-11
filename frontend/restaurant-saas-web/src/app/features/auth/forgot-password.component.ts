import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'rsaas-forgot-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  templateUrl: './forgot-password.component.html',
})
export class ForgotPasswordComponent {
  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  readonly submitted = signal(false);

  constructor(private readonly auth: AuthService) {}

  submit(): void {
    if (this.form.invalid) return;

    const resetUrlTemplate = `${window.location.origin}/auth/reset-password?userId={userId}&token={token}`;
    this.auth.forgotPassword(this.form.getRawValue().email, resetUrlTemplate).subscribe({
      // Always show success — the API intentionally never reveals whether the email exists.
      next: () => this.submitted.set(true),
      error: () => this.submitted.set(true),
    });
  }
}
