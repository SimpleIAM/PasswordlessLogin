import axios from 'axios';

const url = axios.create({
  baseURL: window.location.origin + '/passwordless-api/v1/'
});

const api = {
  getMyAccount() {
    return this.transformPromise(url.get('my-account'));
  },
  getDatePasswordSet() {
    return this.transformPromise(url.get('my-account/date-password-set'));
  },
  setPassword(newPassword) {
    return this.transformPromise(url.post('my-account/set-password', { newPassword }));
  },
  removePassword(newPassword) {
    return this.transformPromise(url.post('my-account/remove-password', {}));
  },
  changeEmail(newEmail) {
    return this.transformPromise(url.post('my-account/change-email', { newEmail }), false);
  },
  transformPromise(apiPromise, redirectUnauthenticated = true) {
    return new Promise((resolve, reject) => {
      apiPromise
        .then(apiResponse => {
          resolve(apiResponse.data);
        })
        .catch(error => {
          error.unauthorized = false;
          error.message = 'An error occured';
          error.errors = null;
          if (error.response) {
            if (typeof error.response.data.message === 'string') {
              error.message = error.response.data.message;
            }
            if (typeof error.response.data.errors === 'object') {
              error.errors = error.response.data.errors;
            }
            switch (error.response.status) {
              case 401:
                if (redirectUnauthenticated) {
                  // redirect to sign in
                  window.location.href = window.location.origin + '/signin?nextUrl=' + window.location.pathname;
                }
              case 403:
                error.unauthorized = true;
                break;
            }
          }
          reject(error);
        });
    })
  }
};

export default api;
