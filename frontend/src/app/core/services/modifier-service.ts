import {
  ModifierGroupRequestDto,
  ModifierOptionRequestDto,
  ModifierOptionResponseDto,
} from './../models/modifier.model';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ModifierGroupResponseDto } from '../models/modifier.model';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root',
})
export class ModifierService {
  private http = inject(HttpClient);
  private readonly endPoint = 'modifier';

  createGroup(group: ModifierGroupRequestDto): Observable<ModifierGroupResponseDto> {
    const urlEndPoint = `${this.endPoint}/groups`;
    return this.http.post<ModifierGroupResponseDto>(urlEndPoint, group);
  }

  addOptionToGroup(
    id: string,
    option: ModifierOptionRequestDto,
  ): Observable<ModifierOptionResponseDto> {
    const urlEndPoint = `${this.endPoint}/groups/${id}/options`;
    return this.http.post<ModifierOptionResponseDto>(urlEndPoint, option);
  }

  getGroupsByProduct(id: string): Observable<ModifierGroupResponseDto[]> {
    const urlEndPoint = `${this.endPoint}/products/${id}`;
    return this.http.get<ModifierGroupResponseDto[]>(urlEndPoint);
  }

  deleteGroup(id: string): Observable<void> {
    const urlEndPoint = `${this.endPoint}/groups/${id}`;
    return this.http.delete<void>(urlEndPoint);
  }

  deleteOption(id: string): Observable<void> {
    const urlEndPoint = `${this.endPoint}/options/${id}`;
    return this.http.delete<void>(urlEndPoint);
  }

  updateGroup(
    groupId: string,
    data: ModifierGroupRequestDto,
  ): Observable<ModifierGroupResponseDto> {
    const urlEndPoint = `${this.endPoint}/groups/${groupId}`;
    return this.http.put<ModifierGroupResponseDto>(urlEndPoint, data);
  }

  updateOption(
    optionId: string,
    data: ModifierOptionRequestDto,
  ): Observable<ModifierOptionResponseDto> {
    const urlEndPoint = `${this.endPoint}/options/${optionId}`;
    return this.http.put<ModifierOptionResponseDto>(urlEndPoint, data);
  }
}
