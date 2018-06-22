// angular components
import { Component } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

// models
import { supportedLanguages } from '../../models/supported-languages';
import { IUser } from '../../models/user';

// services
import { UserService } from 'services/user.service';
import { HttpService } from 'services/http.service';
import { ToastService } from 'services/toast.service';

@Component({
    selector: 'pca-profile',
    templateUrl: './profile.component.html'
})
export class ProfileComponent {
    public selectedLangShort: string;
    public supportedLanguages = supportedLanguages;
    public selectedLangFull: string;
    public showPassword = false;
    public newPassword = '';
    public confirmNewPassword = '';
    public submitted = false;

    public user: IUser;

    constructor(public _translateService: TranslateService,
                private _httpService: HttpService,
                private _toastService: ToastService,
                public _userService: UserService) {
        this._httpService.setRequestIsActive(true);
        const userFromStorage = localStorage.getItem('user');
        this._userService.getUser(userFromStorage ? userFromStorage : '').then(user => {
            this.user = user;
            this._translateService.setDefaultLang(user.language);
            this.selectedLangFull = this.getFullName(this._translateService.getDefaultLang());
            this._httpService.setRequestIsActive(false);
        });
    }

    toggleShowPassword() {
        this.showPassword = !this.showPassword;
      }

    getFullName(shortName: string) {
        const language = supportedLanguages.find(x => x.shortName === shortName);
        return language ? language.displayName : this.selectedLangFull;
    }

    selectLang(lang: any) {
        this._translateService.setDefaultLang(lang);
        this.selectedLangShort = lang;
        this.selectedLangFull = this.getFullName(lang);
        localStorage.setItem('language', lang);
        this.user.language = lang;
    }

    async SaveProfile() {
        this._httpService.setRequestIsActive(true);
        const result = await this._userService.editUser(this.user, this.newPassword === this.confirmNewPassword ? this.newPassword : '');
        if (result) {
            this._toastService.addToast('', this._translateService.instant('#Account.SaveProfileSucces'), 'success');
        } else {
            this._toastService.addToast('', this._translateService.instant('#Account.SaveProfileFailed'), 'error');
        }

        this._httpService.setRequestIsActive(false);
    }
}
