import Vue from 'vue'
import VueCustomElement from 'vue-custom-element'
import 'document-register-element'

Vue.use(VueCustomElement);

Vue.customElement('openidauthority-account', () => new Promise((resolve) => {
  require(['./Account/Account.vue'], (lazyComponent) => resolve(lazyComponent.default));
}));

Vue.customElement('openidauthority-sign-in', () => new Promise((resolve) => {
  require(['./Authenticate/SignIn.vue'], (lazyComponent) => resolve(lazyComponent.default));
}), { props: ['nexturl'] });

Vue.customElement('openidauthority-register', () => new Promise((resolve) => {
  require(['./Authenticate/Register.vue'], (lazyComponent) => resolve(lazyComponent.default));
}), { props: ['nexturl'] });