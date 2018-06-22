// angular components
import { Component } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

// models
import { IRole } from 'models/role';
import { supportedLanguages } from 'models/supported-languages';
import { IUser } from 'models/user';

// services
import { UserService } from 'services/user.service';
import { RoleService } from 'services/role.service';
import { AuthenticationService } from 'services/authentication.service';
import { HttpService } from 'services/http.service';
import { ToastService } from 'services/toast.service';

@Component({
    selector: 'pca-usermanagement',
    templateUrl: './usermanagement.component.html'
})
export class UserManagementComponent {
    public selectedLangFull: string;
    public supportedLanguages = supportedLanguages;
    public users: IUser[];
    public roles: IRole[];
    public currentUser: IUser;

    // table
    public filterValue: string;
    public usersPerPage = 10;
    public pageSizes = [5, 10, 25, 50, 100, 200, 500];
    public pageNumber = 1;

    // add user modal
    public id: string;
    public name: string;
    public email: string;
    public primaryPhone: string;
    public mobilePhone: string;
    public role: string;
    public language: string;

    // edit user modal
    public editId: string;
    public editName: string;
    public editEmail: string;
    public editPrimaryPhone: string;
    public editMobilePhone: string;
    public editRole: string;
    public editLanguage: string;
    public editSelectedLangFull: string;

    constructor(public _translateService: TranslateService,
        public _userService: UserService,
        public _roleService: RoleService,
        private _httpService: HttpService,
        private _toastService: ToastService,
        private _authenticationService: AuthenticationService) {
        this._authenticationService.currentUserIsAdmin();
        const language = localStorage.getItem('language');
        this._translateService.setDefaultLang(language ? language : 'en');
        this.selectedLangFull = this.getFullName(this._translateService.getDefaultLang());
        this._httpService.setRequestIsActive(true);
        this._userService.getUsers().then(x => {
            this.users = x;
            this._httpService.setRequestIsActive(false);
        });
    }

    getFullName(shortName: string) {
        const language = supportedLanguages.find(x => x.shortName === shortName);
        return language ? language.displayName : this.selectedLangFull;
    }

    setPageSize(size) {
        this.usersPerPage = size;
    }

    selectLang(lang: string) {
        this.language = lang;
        this.selectedLangFull = this.getFullName(lang);
    }

    selectEditLang(lang: string) {
        this.editLanguage = lang;
        this.editSelectedLangFull = this.getFullName(lang);
    }

    selectRole(role: string) {
        this.role = role;
    }

    selectEditRole(role: string) {
        this.editRole = role;
    }

    async fillAddUserModal() {
        this._httpService.setRequestIsActive(true);
        this.role = this._translateService.instant('#UserRoleManagement.SelectRole');
        await this._roleService.getRoles().then(x => this.roles = x);
        this._httpService.setRequestIsActive(false);
    }

    async fillUserEditModal(user: IUser) {
        this.currentUser = user;
        this._httpService.setRequestIsActive(true);
        this.editId = user.id;
        this.editName = user.name;
        this.editEmail = user.email;
        this.editPrimaryPhone = user.primaryPhone;
        this.editMobilePhone = user.mobilePhone;
        this.editRole = user.role.length > 0 && user.role[0]
            ? user.role[0]
            : this._translateService.instant('#UserRoleManagement.SelectRole');
        this.editLanguage = user.language ? user.language : this._translateService.getDefaultLang();
        this.editSelectedLangFull = this.getFullName(this.editLanguage);
        await this._roleService.getRoles().then(x => this.roles = x);
        this._httpService.setRequestIsActive(false);
    }

    async editUser() {
        this._httpService.setRequestIsActive(true);
        let user: IUser;
        const userFromStorage = localStorage.getItem('user');
        if (this.currentUser.email === userFromStorage && userFromStorage !== this.editEmail) {
            user = {
                id: this.editId,
                name: this.editName,
                email: this.currentUser.email,
                primaryPhone: this.editPrimaryPhone,
                mobilePhone: this.editMobilePhone,
                role: [this.editRole],
                language: this.editLanguage,
            };
            this._toastService.addToast('', this._translateService.instant('#UserRoleManagement.CannotChangeOwnEmail') + ' ' + this.currentUser.email, 'info');
        } else {
            user = {
                id: this.editId,
                name: this.editName,
                email: this.editEmail,
                primaryPhone: this.editPrimaryPhone,
                mobilePhone: this.editMobilePhone,
                role: [this.editRole],
                language: this.editLanguage,
            };
        }

        const result = await this._userService.editUser(user, '');
        if (result) {
            this._userService.getUsers().then(x => this.users = x);
            this._toastService.addToast('', this._translateService.instant('#UserRoleManagement.EditUserSuccess'), 'success');
        } else {
            this._toastService.addToast('', this._translateService.instant('#UserRoleManagement.EditUserFailt'), 'error');
        }
        this._httpService.setRequestIsActive(false);
    }

    async removeUser(user: IUser) {
        this._httpService.setRequestIsActive(true);
        const result = await this._userService.removeUser(user);
        if (result) {
            this._userService.getUsers().then(x => this.users = x);
            this._toastService.addToast('', this._translateService.instant('#UserRoleManagement.RemovedUserSuccess'), 'success');
        } else {
            this._toastService.addToast('', this._translateService.instant('#UserRoleManagement.RemovedUserFailed'), 'error');
        }
        this._httpService.setRequestIsActive(false);
    }

    async submitNewUser() {
        this._httpService.setRequestIsActive(true);
        const user = {
            id: this.id,
            name: this.name,
            email: this.email,
            primaryPhone: this.primaryPhone,
            mobilePhone: this.mobilePhone,
            role: [this.role],
            language: this.language ? this.language : this._translateService.getDefaultLang()
        };
        const result = await this._userService.addUser(user);
        if (result) {
            this._userService.getUsers().then(x => this.users = x);
            this._toastService.addToast('', this._translateService.instant('#UserRoleManagement.AddUserSuccess') + ' ' + user.email, 'success');
        } else {
            this._toastService.addToast('', this._translateService.instant('#UserRoleManagement.AddUserFailed'), 'error');
        }
        this._httpService.setRequestIsActive(false);
    }
}
