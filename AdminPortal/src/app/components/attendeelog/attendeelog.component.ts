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
    selector: 'pca-attendeelog',
    templateUrl: './attendeelog.component.html'
})
export class AttendeeLogComponent {
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
                private _userService: UserService,
                private _roleService: RoleService,
                private _httpService: HttpService,
                private _toastService: ToastService,
                private _authenticationService: AuthenticationService) {
        this._httpService.setRequestIsActive(true);
        this._userService.getUsers().then(x => {
            this.users = x;
            this._httpService.setRequestIsActive(false);
        });
    }

    setPageSize(size) {
        this.usersPerPage = size;
    }

}
