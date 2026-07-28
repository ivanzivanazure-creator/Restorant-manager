import { HttpClient } from '@angular/common/http';
import { Component, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { OnboardingStatus } from '../../core/models/domain.models';

const DISMISS_KEY = 'rsaas.onboarding.dismissed';

@Component({
  selector: 'rsaas-onboarding-checklist',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatTooltipModule],
  templateUrl: './onboarding-checklist.component.html',
  styleUrl: './onboarding-checklist.component.scss',
})
export class OnboardingChecklistComponent implements OnInit {
  readonly status = signal<OnboardingStatus | null>(null);
  readonly dismissed = signal(localStorage.getItem(DISMISS_KEY) === 'true');

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.http.get<OnboardingStatus>(`${environment.apiBaseUrl}/onboarding/status`).subscribe({
      next: (status) => this.status.set(status),
      error: () => void 0,
    });
  }

  goTo(route: string): void {
    this.router.navigateByUrl(route);
  }

  dismiss(): void {
    localStorage.setItem(DISMISS_KEY, 'true');
    this.dismissed.set(true);
  }
}
