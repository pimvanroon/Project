import { Pipe, PipeTransform, Injectable } from '@angular/core';
import { IUser } from 'models/user';

@Pipe({
    name: 'userfilter'
})

@Injectable()
export class FilterUserPipe implements PipeTransform {
    transform(value: IUser[], searchString: string) {
        if (!searchString) {
            return value;
        }
        const lowerSeach = searchString.toLowerCase();
        const match = str => str.toLowerCase().includes(lowerSeach);
        return value.filter(it =>
            match(it.name) ||
            match(it.email) ||
            match(it.primaryPhone) ||
            match(it.mobilePhone) ||
            ( it.role.length > 0 && match(it.role[0]) ) ||
            match(it.language));
    }
}
