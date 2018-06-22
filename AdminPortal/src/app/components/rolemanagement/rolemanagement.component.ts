// angular components
import { Component } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

// models
import { supportedLanguages } from 'models/supported-languages';
import { IRole } from 'models/role';

// services
import { AuthenticationService } from 'services/authentication.service';
import { RoleService } from 'services/role.service';
import { HttpService } from 'services/http.service';
import { ToastService } from 'services/toast.service';

@Component({
    selector: 'pca-usermanagement',
    templateUrl: './rolemanagement.component.html'
})
export class RoleManagementComponent {
    public selectedLangFull: string;
    public supportedLanguages = supportedLanguages;
    public roles: IRole[];

    // table
    public filterValue: string;
    public rolesPerPage = 10;
    public pageSizes = [5, 10, 25, 50, 100, 200, 500];
    public pageNumber = 1;

    // add role modal
    public id: string;
    public name: string;

    // edit role modal
    public editId: string;
    public editName: string;
    constructor(public _roleService: RoleService,
                private _httpService: HttpService,
                private _translateService: TranslateService,
                private _toastService: ToastService,
                private _authenticationService: AuthenticationService) {
        this._httpService.setRequestIsActive(true);
        this._authenticationService.currentUserIsAdmin();
        this._roleService.getRoles().then(x => {
            this.roles = x;
            this._httpService.setRequestIsActive(false);
        });
        const language = localStorage.getItem('language');
        this._translateService.setDefaultLang(language ? language : 'en');
    }

    fillEditRoleModal(role: IRole) {
        this.editId = role.id;
        this.editName = role.name;
    }

    setPageSize(size) {
        this.rolesPerPage = size;
    }

    async editRole() {
        this._httpService.setRequestIsActive(true);
        const role = {
            id: this.editId,
            name: this.editName
        };
        const result = await this._roleService.editRole(role);
        if (result) {
            this._roleService.getRoles().then(x => this.roles = x);
            this._toastService.addToast('', this._translateService.instant('#UserRoleManagement.EditRoleSuccess'), 'success');
        } else {
            this._toastService.addToast('', this._translateService.instant('#UserRoleManagement.EditRoleFailed'), 'error');
        }
        this._httpService.setRequestIsActive(false);
    }

    async removeRole(role: IRole) {
        this._httpService.setRequestIsActive(true);
        const result = await this._roleService.removeRole(role);
        if (result) {
            this._roleService.getRoles().then(x => this.roles = x);
            this._toastService.addToast('', this._translateService.instant('#UserRoleManagement.RemoveRoleSuccess'), 'success');
        } else {
            this._toastService.addToast('', this._translateService.instant('#UserRoleManagement.RemoveRoleFailed'), 'error');
        }
        this._httpService.setRequestIsActive(false);
    }

    async submitNewRole() {
        this._httpService.setRequestIsActive(true);
        const role = {
            id: this.id,
            name: this.name
        };
        const result = await this._roleService.addRole(role);
        if (result) {
            this._roleService.getRoles().then(x => this.roles = x);
            this._toastService.addToast('', this._translateService.instant('#UserRoleManagement.AddRoleSuccess'), 'success');
        } else {
            this._toastService.addToast('', this._translateService.instant('#UserRoleManagement.AddRoleFailed'), 'error');
        }
        this._httpService.setRequestIsActive(false);
    }
}
