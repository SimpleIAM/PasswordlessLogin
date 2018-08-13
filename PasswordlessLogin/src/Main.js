import Vue from 'vue'
import VueCustomElement from 'vue-custom-element'
import 'document-register-element'
import 'promise-polyfill/src/polyfill'

Vue.use(VueCustomElement);

Vue.customElement('passwordless-account', () => new Promise((resolve) => {
  require(['./components/account/Account.vue'], (lazyComponent) => resolve(lazyComponent.default));
}));

Vue.customElement('passwordless-sign-in', () => new Promise((resolve) => {
  require(['./components/authenticate/SignIn.vue'], (lazyComponent) => resolve(lazyComponent.default));
}), { props: ['nextUrl', 'signInType', 'idPrefix', 'doNotRemember', 'doNotStaySignedIn', 'loginHint'] });

Vue.customElement('passwordless-register', () => new Promise((resolve) => {
  require(['./components/authenticate/Register.vue'], (lazyComponent) => resolve(lazyComponent.default));
}), { props: ['nextUrl'] });