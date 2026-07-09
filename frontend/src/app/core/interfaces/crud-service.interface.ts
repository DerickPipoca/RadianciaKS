import { Observable } from 'rxjs';

export interface ICrudService<TRequest, TResponse, TId = string> {
  getAll(params?: any): Observable<any>;
  getById(id: TId): Observable<TResponse>;
  create(item: TRequest): Observable<TResponse>;
  update(id: TId, item: TRequest): Observable<TResponse>;
  delete(id: TId): Observable<any>;
}
