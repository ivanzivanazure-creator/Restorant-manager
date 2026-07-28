import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PublicStatus } from '../../core/models/domain.models';

@Injectable({ providedIn: 'root' })
export class StatusService {
  constructor(private readonly http: HttpClient) {}

  getStatus(): Observable<PublicStatus> {
    return this.http.get<PublicStatus>(`${environment.apiBaseUrl}/status`);
  }
}
