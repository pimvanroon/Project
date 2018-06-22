import { Injectable } from '@angular/core';

// models
import { IRole } from 'models/role';

// services
import { HttpService } from 'services/http.service';


@Injectable()
export class RoleService {
    constructor(private _httpService: HttpService) { }


    getRoles(): Promise<IRole[]> {
        const url = '/RoleService/GetRoles';
        return this._httpService.getRequest(url)
            .map((roles: any) =>
                roles.map(role => ({
                    id: role.id,
                    name: role.name
                }))
            ).take(1).toPromise();
    }

    addRole(role: IRole): Promise<boolean> {
        const url = '/RoleService/AddRole';
        const payload = {
            name: role.name
        };
        return this._httpService.postRequest(url, payload);
    }

    editRole(role: IRole): Promise<boolean> {
        const url = '/RoleService/EditRole';
        const payload = {
            id: role.id,
            name: role.name
        };
        return this._httpService.postRequest(url, payload);
    }

    removeRole(role: IRole): Promise<boolean> {
        const url = '/RoleService/RemoveRole';
        const payload = {
            id: role.id
        };
        return this._httpService.postRequest(url, payload);
    }

}
