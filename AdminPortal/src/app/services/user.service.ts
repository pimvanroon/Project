import { Injectable } from '@angular/core';

// models
import { IUser } from 'models/user';

// services
import { HttpService } from 'services/http.service';

@Injectable()
export class UserService {

    constructor(private _httpService: HttpService) { }

    getUsers(): Promise<IUser[]> {
        const url = '/UserService/GetUsers';
        return this._httpService.getRequest(url)
            .map((users: any) =>
                users.map(usr => ({
                    id: usr.id,
                    name: usr.name || '',
                    email: usr.email,
                    primaryPhone: usr.primaryPhone || '',
                    mobilePhone: usr.mobilePhone || '',
                    role: usr.rolesList || [],
                    language: usr.language || ''
                }))
            ).take(1).toPromise();
    }

    getUser(email: string): Promise<IUser> {
        const url = '/UserService/GetUser?email=' + email;
        return this._httpService.getRequest(url)
            .map((usr: any) => ({
                id: usr.id,
                name: usr.name || '',
                email: usr.email,
                primaryPhone: usr.primaryPhone || '',
                mobilePhone: usr.mobilePhone || '',
                role: usr.rolesList || [],
                language: usr.language || ''
            }))
            .take(1).toPromise();
    }

    addUser(user: IUser): Promise<boolean> {
        const url = '/UserService/AddUser';
        const payload = {
            name: user.name, email: user.email, primaryphone: user.primaryPhone,
            mobilephone: user.mobilePhone, role: user.role[0], language: user.language
        };
        return this._httpService.postRequest(url, payload);
    }

    editUser(user: IUser, password: string): Promise<boolean> {
        const url = '/UserService/EditUser';
        const payload = {
            id: user.id, name: user.name, email: user.email, primaryphone: user.primaryPhone,
            mobilephone: user.mobilePhone, role: user.role[0], language: user.language, password: password
        };
        return this._httpService.postRequest(url, payload);
    }

    removeUser(user: IUser): Promise<boolean> {
        const url = '/UserService/RemoveUser';
        const payload = {
            id: user.id
        };
        return this._httpService.postRequest(url, payload);
    }
}
