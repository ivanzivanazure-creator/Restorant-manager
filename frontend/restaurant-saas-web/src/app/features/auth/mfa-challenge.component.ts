import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'rsaas-mfa-challenge',
  standalone: true,
  imports: [ReactiveFormsModule, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  templateUrl: './mfa-challenge.component.html',
})
export class MfaChallengeComponent {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);

  readonly form = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(6)]],
  });

  readonly errorMessage = signal<string | null>(null);
  private readonly challengeToken = this.route.snapshot.queryParamMap.get('challenge') ?? '';

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
  ) {}

  submit(): void {
    if (this.form.invalid || !this.challengeToken) return;

    this.auth.verifyMfa(this.challengeToken, this.form.getRawValue().code).subscribe({
      next: () => this.router.navigate(['/']),
      error: () => this.errorMessage.set('Invalid or expired code. Please try again.'),
    });
  }
}
