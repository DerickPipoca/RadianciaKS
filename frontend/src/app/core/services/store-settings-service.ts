import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { StoreSettingsRequestDto, StoreSettingsResponseDto } from '../models/store-settings.model';

@Injectable({
  providedIn: 'root',
})
export class StoreSettingsService {
  private http = inject(HttpClient);
  private readonly endPoint = 'StoreSettings';

  getSettings(): Observable<StoreSettingsResponseDto> {
    return this.http.get<StoreSettingsResponseDto>(this.endPoint);
  }

  updateSettings(dto: StoreSettingsRequestDto): Observable<StoreSettingsResponseDto> {
    return this.http.put<StoreSettingsResponseDto>(this.endPoint, dto);
  }

  uploadLogo(file: File, isBig: boolean): Observable<{ url: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ url: string }>(`${this.endPoint}/upload-logo?isBig=${isBig}`, formData);
  }
}
