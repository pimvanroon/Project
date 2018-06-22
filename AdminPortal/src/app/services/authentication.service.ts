// Angular and rx
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';

// Services
import { ConfigurationService } from 'services/configuration.service';
import { JwtService } from 'services/jwt.service';
import { TranslateService } from '@ngx-translate/core';
import { UserService } from 'services/user.service';

@Injectable()
export class AuthenticationService {

    constructor(private _router: Router,
        public _userService: UserService) { }

    async logout() {
        localStorage.removeItem('user');
        localStorage.removeItem('role');
        localStorage.removeItem('jwt_token');
        localStorage.removeItem('language');
    }

    get isLoggedIn() {
        return localStorage.getItem('user') !== null;
    }

    get loggedInUser() {
        return localStorage.getItem('user');
    }

    currentUserIsAdmin() {
        const role = localStorage.getItem('role');
        if (role && role.toLowerCase() !== 'admin') {
            this._router.navigate(['']);
        }
    }

}
