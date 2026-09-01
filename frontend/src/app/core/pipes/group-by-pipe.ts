import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'groupBy',
})
export class GroupByPipe implements PipeTransform {
  transform(collection: any[], property: string): { key: string; items: any[] }[] {
    if (!collection || collection.length === 0) return [];

    const groupedCollection = collection.reduce((previous, current) => {
      const groupKey = current[property] || 'Outros';

      if (!previous[groupKey]) {
        previous[groupKey] = [];
      }

      previous[groupKey].push(current);
      return previous;
    }, {});

    return Object.keys(groupedCollection).map((key) => ({
      key: key,
      items: groupedCollection[key],
    }));
  }
}
