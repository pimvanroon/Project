// angular
import { Component } from '@angular/core';
import { Router } from '@angular/router';

// services
import { AuthenticationService } from 'services/authentication.service';
import { ConfigurationService } from 'services/configuration.service';

@Component({
    selector: 'pca-navbar',
    templateUrl: './navbar.component.html'
})
export class NavbarComponent {

    constructor(private _config: ConfigurationService,
                private _router: Router,
                private _authenticationService: AuthenticationService) { }

    isRouteActive(instruction: string): boolean {
        return this._router.isActive(instruction, true);
    }

    get showLink() {
        const role = localStorage.getItem('role');
        return (role && role.toLowerCase() === 'admin') ? true : false;
    }

    logout() {
        this._authenticationService.logout();
    }
}
