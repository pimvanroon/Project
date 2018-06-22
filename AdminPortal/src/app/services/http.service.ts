import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Router } from '@angular/router';

import 'rxjs/add/operator/take';
import 'rxjs/add/operator/switchMap';

// services
import { ConfigurationService } from 'services/configuration.service';
import { IToken } from 'models/token';
import { TranslateService } from '@ngx-translate/core';
import { JwtService } from 'services/jwt.service';


@Injectable()
export class HttpService {
    public requestIsActive = false;
    constructor(private _http: HttpClient,
                private _config: ConfigurationService,
                private _translateService: TranslateService,
                private _jwtService: JwtService,
                private _router: Router) { }

    public setRequestIsActive(val: boolean) {
        this.requestIsActive = val;
    }

    public getRequest(appendUrl: string) {
        return this._config.urls$.switchMap(urls => this._http.get(urls.identityService + appendUrl));
    }

    public postRequestJson(appendUrl: string, payload: object) {
        const options = {
            headers: new HttpHeaders()
                .set('Content-Type', 'application/json')
                .append('ServiceVersion', '1')
        };
        return this._config.urls$.switchMap(urls => this._http.post(urls.identityService + appendUrl, payload, options))
        .subscribe(
            data => {
                const result = data as IToken;
                this.SetCredentials(result.auth_token);
                return result.auth_token;
            });
    }

    public postRequest(appendUrl: string, payload: object): Promise<boolean> {
        const options = {
            headers: new HttpHeaders()
                .set('Content-Type', 'application/json')
                .append('ServiceVersion', '2')
                .append('Authorization', 'Bearer ' + localStorage.getItem('jwt_token')),
            // If responsetype is not supplied, default is JSON. But when the result 'true' is parsed as JSON, HttpClient
            // will throw a parse exception. Hence state the response type is text.
            responseType: 'text' as 'text',
        };
        return this._config.urls$.switchMap(urls => this._http.post(urls.identityService + appendUrl, payload, options))
            .take(1).toPromise().then(v => v === 'True');
    }

    SetCredentials(data: string) {
        const parsedToken = this._jwtService.decodeToken(data);

            localStorage.setItem('role', 'admin');
            localStorage.setItem('language', 'en');
            this._translateService.setDefaultLang('en');
            localStorage.setItem('jwt_token', JSON.stringify(data));
            localStorage.setItem('user', parsedToken.sub);
            this._router.navigate(['']);
        
    }

}
