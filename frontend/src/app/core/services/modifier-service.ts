import {
  ModifierGroupRequest,
  ModifierOptionRequest,
  ModifierOptionResponse,
} from './../models/modifier.model';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ModifierGroupResponse } from '../models/modifier.model';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root',
})
export class ModifierService {
  private http = inject(HttpClient);
  private readonly endPoint = 'modifier';

  createGroup(group: ModifierGroupRequest): Observable<ModifierGroupResponse> {
    return this.http.post<ModifierGroupResponse>(this.endPoint, group);
  }

  addOptionToGroup(id: string, option: ModifierOptionRequest): Observable<ModifierOptionResponse> {
    const urlEndPoint = `${this.endPoint}/groups/${id}/options`;
    return this.http.post<ModifierOptionResponse>(urlEndPoint, option);
  }

  getGroupsByProduct(id: string): Observable<ModifierGroupResponse[]> {
    const urlEndPoint = `${this.endPoint}/products/${id}`;
    return this.http.get<ModifierGroupResponse[]>(id);
  }

  deleteGroup(id: string): Observable<void> {
    const urlEndPoint = `${this.endPoint}/groups/${id}`;
    return this.http.delete<void>(urlEndPoint);
  }

  deleteOption(id: string): Observable<void> {
    const urlEndPoint = `${this.endPoint}/options/${id}`;
    return this.http.delete<void>(urlEndPoint);
  }
}
