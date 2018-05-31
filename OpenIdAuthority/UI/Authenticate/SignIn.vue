<template>
  <div class="signIn">
    <form @submit.prevent="submitForm" class="form">
      <section class="field field-stacked form_row">
        <label class="field_label" :for="getId('username')">{{usernameText}}</label>
        <input class="field_element field_element-fullWidth signIn_username"
          ref="username"
          :id="getId('username')"
          v-model="username" 
          type="text" 
          placeholder="you@example.com">
        <span v-if="usernameError" class="field_error">{{usernameError}}</span>
      </section>
      <section v-show="!showSavedUsernames">
        <div class="field field-stacked form_row">
          <label class="field_label" :for="getId('password')">{{passwordText}}</label>
          <input 
            class="field_element field_element-fullWidth signIn_password" 
            ref="password" 
            type="password" 
            :id="getId('password')"
            :placeholder="passwordPlaceholderText" 
            v-model="password">
          <span v-if="passwordError" class="field_error">{{passwordError}}</span>
        </div>

        <div class="field field-checkbox form_row" v-if="!(doNotRemember && doNotStaySignedIn)">
          <input 
            class="field_element"
            type="checkbox" 
            :id="getId('stay-signed-in')"
            v-model="staySignedIn">
          <label class="field_label" :for="getId('stay-signed-in')">{{staySignedInText}}</label>
        </div>

        <div class="fields fields-flexSpaceBetween form_row">
          <div v-if="acceptCode" class="field">
            <button 
              class="field_element field_element-tall signIn_oneTimeCodeButton"
              :type="password.length == 0 ? 'submit' : 'button'"
              :disabled="username.length == 0 || password.length > 0"
              >Get one time code
            </button>
          </div>
          <div class="field">
            <button 
              class="field_element field_element-tall signIn_signInButton"
              type="submit"
              :disabled="!signInEnabled"
              >Sign in
            </button>
          </div>
        </div>
        <div class="message message-notice signIn_message" v-if="message">
          {{message}}
        </div>
      </section>
      <div v-show="acceptPassword" class="minorNav signIn_footer">
        <a class="signIn_forgotPasswordLink" href="/forgotpassword">Forgot password?</a>
      </div>
    </form>
    <section class="savedUsernames" v-if="showSavedUsernames">
      <header class="savedUsernames_header">
        <span class="savedUsernames_title">Saved Usernames</span>
      </header>
      <div class="form">
        <div class="field form_row" v-for="name in savedUsernames" v-bind:key="name">
          <button 
            @click="selectUsername(name)" 
            class="savedUsernames_username field_element field_element-fullWidth field_element-tall">
            {{name}}
          </button>
        </div>
      </div>
      <div class="savedUsernames_footer">
        <a href="#" class="savedUsernames_clearUsernames" @click.prevent="clearSavedUsernames">Clear saved usernames</a>
      </div>
    </section>
  </div>
</template>

<script>
import Vue from 'vue';
import api from '../Shared/api.js';

var VueCookie = require('vue-cookie');

Vue.use(VueCookie);

export default {
  props: {
    nextUrl: String,
    loginHint: String,
    signInType: String,
    idPrefix: String,
    doNotRemember: Boolean,
    doNotStaySignedIn: Boolean
  },
  data: function() {
    return {
      savedUsernames: [],
      username: '',
      password: '',
      passwordError: '',
      message: '',
      staySignedIn: true
    };
  },
  watch: {
    username: function(newUsername, oldUsername) {
      this.message = '';
      if(newUsername.length == 0) {
        this.password = '';
        this.loadSavedUsernames();
      }
    },
    password: function() {
      this.message = '';
    }
  },
  computed: {
    showSavedUsernames: function() {
      return !this.doNotRemember && this.savedUsernames.length && this.username.length == 0;
    },
    usernameError: function() {
      if(this.username.length == 0) {
        return '';
      }
      if(this.username.includes(' ')) {
        return 'No spaces allowed'
      }
      return '';
    },
    usernameIsValid: function() {
      return this.username.length > 0 && !this.usernameError;
    },
    signInEnabled: function() {
      return this.usernameIsValid && this.password.length > 0;
    },
    acceptPassword: function() {
      return this.signInType !== 'code';
    },
    acceptCode: function() {
      return this.signInType !== 'password';
    },
    usernameText: function() {
      return 'Email';
    },
    passwordText: function() {
      switch(this.signInType) {
        case 'code':
          return 'One time code';
        case 'password':
          return 'Password';
        default:
          return 'Password or one time code';
      }
    },
    passwordPlaceholderText: function() {
      switch(this.signInType) {
        case 'code':
          return '123...';
        case 'password':
          return 'password';
        default:
          return '****** / 123...';
      }
    },
    staySignedInText: function() {
      if(this.doNotRemember) {
        return 'Stay signed in';
      }
      else if(this.doNotStaySignedIn) {
        return 'Remember username';
      }
      else {
        return 'Remember username and stay signed in';
      }
    }
  },
  methods: {
    getId: function(name) {
      let prefix = '';
      if(typeof this.signInType === 'string') {
        prefix = this.signInType + '-';
      }
      if(typeof this.idPrefix === 'string') {
        prefix = this.idPrefix + '-';
      }
      return prefix + name;
    },
    selectUsername: function(name) {
      this.username = name;
      this.$nextTick(() => {
        this.$refs.password.focus();
      });
    },
    submitForm: function() {
      this.message = "Please wait...";
      if(this.password.length > 0) {
        this.signIn();
      }
      else if(this.signInType !== 'password') {
        this.getOneTimeCode();
      }
      else {
        this.message = '';
      }
    },
    getOneTimeCode: function() {
      api.sendOneTimeCode(this.username, this.nextUrl)
        .then(data => {
          this.message = 'We sent sent a one time code to your email or phone';
          this.$nextTick(() => {
            this.$refs.password.focus();
          });
        })
        .catch(error => {
          if(error.message) {
            this.message = error.message;
          }
          else {
            this.message = 'Something went wrong';
          }
        });
    },
    signIn: function() {
      if(this.signInEnabled) {
        let oneTimeCode = this.password.replace(' ', '');
        if (this.signInType == 'code' || /^[0-9]{6}$/.test(oneTimeCode)) {
          api.authenticate(this.username, oneTimeCode, this.staySignedIn)
            .then(data => this.signInDone(data))
            .catch(error => this.signInFailed(error));
        }
        else {
          api.authenticatePassword(this.username, this.password, this.staySignedIn, this.nextUrl)
            .then(data => this.signInDone(data))
            .catch(error => this.signInFailed(error));
        }
      }
    },
    signInDone: function(data) {
      if(!this.doNotRemember) {
        this.saveUsernames();
      }
      if (typeof data.nextUrl === 'string') {
        window.location = data.nextUrl;
      }
      else {
        window.reload();
      }
    },
    signInFailed: function(error) {
      if(error.response.status == 401) {
        this.password = '';
      }
      this.$nextTick(() => {
        this.message = error.message ? error.message : 'Something went wrong';
      });
    },
    loadSavedUsernames() {
      this.savedUsernames = [];
      const usernames = this.$cookie.get('SavedUsernames');
      if(typeof usernames == 'string') {
        let count = 0;
        usernames.split(' ').forEach(name => {
          count++;
          if(count <= 3 && name.length > 1 && name.length <= 100) {
            this.savedUsernames.push(name);
          }
        });
      }
    },
    saveUsernames() {
      if(this.staySignedIn) {
        let usernames = this.savedUsernames.filter(name => name.toLowerCase() !== this.username.toLowerCase());
        if(this.username.length <= 100) {
          usernames.unshift(this.username);
        }
        this.$cookie.set('SavedUsernames', usernames.slice(0, 3).join(' '), { expires: '1Y' });        
      }
    },
    clearSavedUsernames() {
      this.$cookie.delete('SavedUsernames');
      this.savedUsernames = [];
    }
  },
  mounted: function() {
    this.loadSavedUsernames();
    if(this.loginHint) {
      this.username = this.loginHint;
      this.$nextTick(() => {
        this.$refs.password.focus();
      });
    }
    else {
      this.$nextTick(() => {
        this.$refs.username.focus();
      });      
    }
  }
};
</script>
