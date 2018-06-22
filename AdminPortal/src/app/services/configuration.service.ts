import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import 'rxjs/add/operator/map';
import 'rxjs/add/operator/shareReplay';

// models
import { IUrls } from 'models/IUrls';

@Injectable()
export class ConfigurationService {

    public urls$ = this._http.get('/assets/config/urls.json')
        .map(v => v as IUrls)
        .shareReplay(1);

    constructor(private _http: HttpClient) {
    }

}
