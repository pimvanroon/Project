import { Pipe, PipeTransform, Injectable } from '@angular/core';
import { IRole } from 'models/role';

@Pipe({
    name: 'rolefilter'
})

@Injectable()
export class FilterRolePipe implements PipeTransform {
    transform(value: IRole[], searchString: string) {
        if (!searchString) {
            return value;
        }
        const lowerSeach = searchString.toLowerCase();
        const match = str => str.toLowerCase().includes(lowerSeach);
        return value.filter(it =>
            (match(it.name)));
    }
}
