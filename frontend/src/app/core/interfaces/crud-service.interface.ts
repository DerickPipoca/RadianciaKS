import { Observable } from 'rxjs';

export interface ICrudService<TRequest, TResponse, TId = string> {
  getAll(): Observable<TResponse[]>;
  getById(id: TId): Observable<TResponse>;
  create(item: TRequest): Observable<TResponse>;
  update(id: TId, item: TRequest): Observable<TResponse>;
  delete(id: TId): Observable<any>;
}
