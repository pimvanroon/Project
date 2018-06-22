import { Injectable } from '@angular/core';
import { ToastOptions, ToastyConfig, ToastyService } from 'ng2-toasty';

@Injectable()
export class ToastService {

    constructor(private toastyService: ToastyService, private toastyConfig: ToastyConfig) {
        this.toastyConfig.limit = 5;
    }

    addToast(title: string, msg: string, flag: 'info' | 'success' | 'wait' | 'error' | 'warning') {

        const toastOptions: ToastOptions = {title, msg, showClose: true, timeout: 5000, theme: 'bootstrap'};

        switch (flag) {
            case ('info'):
                this.toastyService.info(toastOptions);
                break;
            case ('success'):
                this.toastyService.success(toastOptions);
                break;
            case ('wait'):
                this.toastyService.wait(toastOptions);
                break;
            case ('error'):
                this.toastyService.error(toastOptions);
                break;
            case ('warning'):
                this.toastyService.warning(toastOptions);
                break;
            default:
                break;
        }
    }

    clearToast() {
        this.toastyService.clearAll();
    }
}
