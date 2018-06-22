import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';


// Services
import { ConfigurationService } from 'services/configuration.service';
import { ToastService } from '../services/toast.service';

// Third party services
import { TranslateService } from '@ngx-translate/core';

// Models
import { IToken } from 'models/token';
import { HttpService } from 'services/http.service';

@Injectable()

export class LoginService {

    constructor(private _httpService: HttpService,
                private _toastService: ToastService,
                private _configService: ConfigurationService,
                private _translate: TranslateService) {
    }

    login(email: string, password: string) {
        const url = '/LoginService/LoginAndGetJwtToken';
        const payload = { email: email, password: password };
        return this._httpService.postRequestJson(url, payload);
    }

    resetPassword(email: string) {
        const url = '/UserService/ResetPassword';
        const payload = {email: email };
        return this._httpService.postRequest(url, payload);
    }
}

