import { Observable } from 'rxjs';
import { PagedResponse } from '../models/paged-response.model';
import { ProductRequestDto, ProductResponseDto } from '../models/product.model';
import { ICrudService } from './crud-service.interface';

export interface IProductService
  extends Omit<ICrudService<ProductRequestDto, ProductResponseDto, string>, 'getAll'> {
  getAll(params: {
    pageNumber: number;
    pageSize: number;
    searchTerm?: string;
    sortBy?: string;
    isDescending?: boolean;
    categoryId?: string;
  }): Observable<PagedResponse<ProductResponseDto>>;
}
