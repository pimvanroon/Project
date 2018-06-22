import { TestBed, async } from '@angular/core/testing';
import { AppComponent } from './app.component';

// third party modules
import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
import { TranslateHttpLoader } from '@ngx-translate/http-loader';
import { NgxPaginationModule } from 'ngx-pagination';
import { Routes, RouterModule } from '@angular/router';
import { ToastyModule } from 'ng2-toasty';

// components
import { NavbarComponent } from 'components/navbar/navbar.component';
import { ProfileComponent } from 'components/profile/profile.component';
import { SpinnerComponent } from 'components/spinner/spinner.component';
import { UserManagementComponent } from 'components/usermanagement/usermanagement.component';
import { RoleManagementComponent } from 'components/rolemanagement/rolemanagement.component';

// pipes
import { FilterUserPipe } from 'pipes/filter-user-table.pipe';
import { FilterRolePipe } from 'pipes/filter-role-table.pipe';

// services
import { AuthenticationService } from 'services/authentication.service';
import { JwtService } from 'services/jwt.service';
import { HttpService } from 'services/http.service';
import { ConfigurationService } from 'services/configuration.service';
import { UserService } from 'services/user.service';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule, HttpClient } from '@angular/common/http';
import { APP_BASE_HREF } from '@angular/common';
import { ToastService } from 'services/toast.service';


export function HttpLoaderFactory(http: HttpClient) {
  return new TranslateHttpLoader(http, './assets/i18n/', '.json');
}


const appRoutes: Routes = [
  { path: '', component: ProfileComponent },
  { path: 'usermanagement', component: UserManagementComponent },
  { path: 'rolemanagement', component: RoleManagementComponent },
  { path: '**', redirectTo: '/' }
];

describe('AppComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      declarations: [
        AppComponent,
        NavbarComponent,
        ProfileComponent,
        SpinnerComponent,
        UserManagementComponent,
        RoleManagementComponent,
        FilterUserPipe,
        FilterRolePipe
      ],
      imports: [
        BrowserModule,
        FormsModule,
        NgxPaginationModule,
        ReactiveFormsModule,
        ToastyModule.forRoot(),
        RouterModule.forRoot(appRoutes),
        HttpClientModule,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useFactory: HttpLoaderFactory,
            deps: [HttpClient]
          }
        })
      ],
      providers: [
        AuthenticationService,
        ConfigurationService,
        JwtService,
        HttpService,
        UserService,
        ToastService,
        { provide: APP_BASE_HREF, useValue : '/' }
      ]
    });
    TestBed.compileComponents();
  });

  it('should create the app', async(() => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.debugElement.componentInstance;
    expect(app).toBeTruthy();
  }));

});
