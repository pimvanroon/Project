// angular modules
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule, ReactiveFormsModule  } from '@angular/forms';
import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { HttpClientModule, HttpClient } from '@angular/common/http';

// third party modules
import { ToastyModule } from 'ng2-toasty';
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import { NgxPaginationModule } from 'ngx-pagination';


// components
import { AppComponent } from './app.component';
import { LoginComponent } from 'components/authentication/login.component';
import { NavbarComponent } from 'components/navbar/navbar.component';
import { ProfileComponent } from 'components/profile/profile.component';
import { UserManagementComponent } from 'components/usermanagement/usermanagement.component';
import { SpinnerComponent } from 'components/spinner/spinner.component';
import { RoleManagementComponent } from 'components/rolemanagement/rolemanagement.component';
import { AttendeeLogComponent } from 'components/attendeelog/attendeelog.component';

// pipes
import { SortDatePipe } from 'pipes/filter-date-table.pipe';
import { FilterRolePipe } from 'pipes/filter-role-table.pipe';
import { FilterUserPipe } from 'pipes/filter-user-table.pipe';

// services
import { AuthenticationService } from 'services/authentication.service';
import { LoginService } from 'services/login.service';
import { ConfigurationService } from 'services/configuration.service';
import { HttpService } from 'services/http.service';
import { JwtService } from 'services/jwt.service';
import { UserService } from 'services/user.service';
import { RoleService } from 'services/role.service';
import { ToastService } from 'services/toast.service';

export function HttpLoaderFactory(http: HttpClient) {
  return new TranslateHttpLoader(http, './assets/i18n/', '.json');
}

const appRoutes: Routes = [
  { path: '' , component: ProfileComponent},
  { path: 'usermanagement' , component: UserManagementComponent},
  { path: 'rolemanagement' , component: RoleManagementComponent},
  { path: 'attendancelog' , component: AttendeeLogComponent},
  { path: '**',  redirectTo: '/' }
];

@NgModule({
  declarations: [
    AppComponent,
    AttendeeLogComponent,
    LoginComponent,
    NavbarComponent,
    ProfileComponent,
    UserManagementComponent,
    SpinnerComponent,
    RoleManagementComponent,
    FilterRolePipe,
    FilterUserPipe,
    SortDatePipe
  ],
  imports: [
    BrowserModule,
    FormsModule,
    NgxPaginationModule,
    ReactiveFormsModule,
    RouterModule.forRoot(appRoutes),
    ToastyModule.forRoot(),
    HttpClientModule,
        TranslateModule.forRoot({
            loader: {
                provide: TranslateLoader,
                useFactory: HttpLoaderFactory,
                deps: [HttpClient]
            }
        })
  ],
  exports: [
    RouterModule
  ],
  providers: [
    AuthenticationService,
    ConfigurationService,
    HttpService,
    JwtService,
    LoginService,
    RoleService,
    ToastService,
    UserService
  ],
  bootstrap: [
    AppComponent
  ]
})
export class AppModule { }
