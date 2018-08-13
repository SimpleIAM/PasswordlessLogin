import axios from 'axios';

const url = axios.create({
  baseURL: window.location.origin + '/api/v1/'
});

const api = {
  register(applicationId, email, nextUrl) {
    return this.transformPromise(url.post('register', { applicationId, email, nextUrl }));
  },
  sendOneTimeCode(username, nextUrl) {
    return this.transformPromise(url.post('send-one-time-code', { username, nextUrl }));
  },
  authenticate(username, oneTimeCode, staySignedIn) {
    return this.transformPromise(url.post('authenticate', { username, oneTimeCode, staySignedIn }));
  },
  authenticatePassword(username, password, staySignedIn, nextUrl) {
    return this.transformPromise(url.post('authenticate-password', { username, password, staySignedIn, nextUrl }));
  },
  sendPasswordResetMessage(applicationId, username, nextUrl) {
    return this.transformPromise(url.post('send-password-reset-message', { applicationId, username, nextUrl }));
  },
  transformPromise(apiPromise) {
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
