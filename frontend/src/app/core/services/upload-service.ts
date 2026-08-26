import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class UploadService {
  private http = inject(HttpClient);

  private readonly endPoint = 'upload';

  uploadImage(file: File, folderName: string = 'images'): Observable<{ url: string }> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('folderName', folderName);

    return this.http.post<{ url: string }>(this.endPoint, formData);
  }
}
