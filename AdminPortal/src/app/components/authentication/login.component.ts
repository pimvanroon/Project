import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { FormControl, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import 'rxjs/add/operator/pairwise';
import 'rxjs/add/operator/bufferCount';
import 'rxjs/add/operator/filter';
import 'rxjs/add/operator/take';
import { TranslateService } from '@ngx-translate/core';
import { ToastService } from 'services/toast.service';
import { HttpService } from 'services/http.service';
import { LoginService } from 'services/login.service';



@Component({
  selector: 'pca-login',
  templateUrl: 'login.component.html'
})
export class LoginComponent implements OnInit {
  public emailAddress = '';
  public password = '';
  public resetPassword = false;
  public passwordHasChanged = false;
  public missingPassword = false;
  public missingUsername = false;
  private showPassword = false;
  myform: FormGroup;
  emailForm: FormControl;
  passwordForm: FormControl;


  constructor(private _httpService: HttpService,
              private _loginService: LoginService,
              private _translateService: TranslateService,
              private _route: ActivatedRoute,
              private _toastService: ToastService) {
  }

  ngOnInit() {
    this.createFormControls();
    this.createForm();
  }

  get isLoading() {
    return this._httpService.requestIsActive;
  }

  keyPressed(event) {
    if (event.key === 'Enter') {
      this.login();
    }
  }

  toggleShowPassword() {
    this.showPassword = !this.showPassword;
  }

  createFormControls() {
    this.emailForm = new FormControl('', [Validators.required, Validators.pattern('\\b[\\w.%-]+@[-.\\w]+\\.[A-Za-z]{2,4}\\b')]);
    this.passwordForm = new FormControl('', [Validators.required, Validators.minLength(8)]);
  }

  createForm() {
    this.myform = new FormGroup({ emailForm: this.emailForm, passwordForm: this.passwordForm });
  }

  login() {
    if (this.emailAddress && this.password) {
      this._loginService.login(this.emailAddress, this.password);
    }
  }

  forgottenPassword(bool: boolean) {
    this.resetPassword = bool;
  }

  async reset() {
    if (this.emailAddress) {
      const result = await this._loginService.resetPassword(this.emailAddress);
      this.forgottenPassword(false);
      if (result) {
        this._toastService.addToast('', this._translateService.instant('#Reset.PasswordHasBeenReset'), 'success');
      } else {
        this._toastService.addToast('', this._translateService.instant('#Reset.PasswordFailedToReset'), 'error');
      }
      this.passwordHasChanged = true;
    } else {
      if (this.emailAddress) {
        this._toastService.addToast('', this._translateService.instant('#Errors.RequiredEmail'), 'error');
      }
    }
  }
}
