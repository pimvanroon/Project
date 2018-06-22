import { Component } from '@angular/core';
import { Subscription } from 'rxjs/Subscription';
import 'rxjs/add/operator/filter';
import 'rxjs/add/operator/debounceTime';

// services
import { HttpService } from 'services/http.service';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'pca-root',
  templateUrl: './app.component.html'
})
export class AppComponent {

  constructor(private _httpService: HttpService,
              private _translateService: TranslateService) {
    const language = localStorage.getItem('language');
    this._translateService.setDefaultLang(language ? language : 'en');
  }
  get requestIsActive() {
    return this._httpService.requestIsActive;
  }
  get isLoggedIn() {
    return localStorage.getItem('user') !== null;
  }

}
