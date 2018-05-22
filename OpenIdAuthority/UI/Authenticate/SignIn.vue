<template>
  <div class="signIn">
    <form @submit.prevent="submitForm" class="form">
      <section class="field field-stacked form_row">
        <label class="field_label" for="username">Email, phone, or username</label>
        <input class="field_element field_element-fullWidth signIn_username"
          ref="username"
          id="username" 
          v-model="username" 
          type="text" 
          placeholder="you@example.com"
          >
        <span v-if="usernameError" class="field_error">{{usernameError}}</span>
      </section>
      <section v-show="(savedUsernames.length == 0 || username.length) && !showPasswordReset">
        <div class="field field-stacked form_row">
          <label class="field_label" for="password">Password or one time code</label>
          <input class="field_element field_element-fullWidth signIn_password" ref="password" type="password" placeholder="**** / 123..." id="password" v-model="password">
          <span v-if="passwordError" class="field_error">{{passwordError}}</span>
        </div>

        <div class="field field-checkbox form_row">
          <input 
            class="field_element"
            :disabled="password.length == 0"
            type="checkbox" 
            id="stay-signed-in" 
            v-model="staySignedIn">
          <label class="field_label" for="stay-signed-in">Remember username and stay signed in</label>
        </div>

        <div class="fields fields-flexSpaceBetween form_row">
          <div class="field">
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
        <div class="signIn_message" v-if="message">
          {{message}}
        </div>
        <div v-if="!showPasswordReset" class="signIn_footer">
          <a class="signIn_forgotPasswordLink" href="#" @click.prevent="forgotPasswordLinkClicked">Forgot password?</a>
        </div>
      </section>
      <section v-if="showPasswordReset"> 
        <div class="field form_row">
          <button 
            :disabled="username.length == 0"            
            type="submit"
            class="field_element field_element-fullWidth field_element-tall signIn_passwordResetButton"
            >Get password reset email
          </button>
        </div>
        <div class="signIn_footer">
          <a 
            href="#"
            class="signIn_cancelButton"
            @click.prevent="showPasswordReset = false"
            >Sign in
          </a>
        </div>
      </section>
    </form>
    <section class="savedUsernames" v-if="savedUsernames.length && username.length == 0">
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
  props: ['nexturl'],
  data: function() {
    return {
      savedUsernames: [],
      username: '',
      password: '',
      passwordError: '',
      message: '',
      staySignedIn: true,
      showPasswordReset: false
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
    }
  },
  methods: {
    selectUsername: function(name) {
      this.username = name;
      this.$nextTick(() => {
        this.$refs.password.focus();
      });
    },
    submitForm: function() {
      if(this.showPasswordReset) {
        this.getPasswordResetEmail();
      }
      else if(this.password.length == 0) {
        this.getOneTimeCode();
      }
      else {
        this.signIn();
      }
    },
    getOneTimeCode: function() {
      api.sendOneTimeCode(this.username, this.nexturl)
        .then(data => {
          this.message = 'We sent sent a one time code to your email or phone.';
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
        this.message = "Signing in..."
        //before redirect, save cookie
        this.saveUsernames();
      }
    },
    forgotPasswordLinkClicked: function() {
      this.showPasswordReset = true;
      this.password = '';
    },
    getPasswordResetEmail: function() {
      //todo:implement
      this.message = "Check your email for password reset instructions";
      this.showPasswordReset = false;      
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
    this.$nextTick(() => {
      this.$refs.username.focus();
    });
  }
};
</script>
