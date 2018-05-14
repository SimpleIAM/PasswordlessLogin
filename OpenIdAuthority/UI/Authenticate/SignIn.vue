<template>
  <div class="signin">
    <form @submit.prevent="submitForm">
      <div class="field">
        <label for="username">Email, phone, or username</label>
        <input 
          ref="username"
          id="username" 
          v-model="username" 
          type="text" 
          placeholder="you@example.com"
          >
        <span v-if="usernameError" class="field-validation-error">{{usernameError}}</span>
      </div>
      <div v-show="username.length && !showPasswordReset">
        <div class="field">
          <label for="password">Passphrase or one time code</label>
          <input ref="password" type="password" placeholder="******* or 000000" id="password" v-model="password">
          <span v-if="passwordError" class="field-validation-error">{{passwordError}}</span>
        </div>

        <div class="field">
          <div class="inline">
            <input 
              :disabled="password.length == 0"
              type="checkbox" 
              id="stay-signed-in" 
              v-model="staySignedIn">
            <label for="stay-signed-in">Remember username and stay signed in</label>
          </div>
        </div>

        <div class="field inline">
          <button 
            :type="password.length == 0 ? 'submit' : 'button'"
            :disabled="password.length > 0"
            >Get one time code
          </button>
          <button 
            type="submit"
            :disabled="!signInEnabled"
            >Sign in
          </button>
        </div>
        <p v-if="message" class="message">
          {{message}}
        </p>
        <div class="hint" v-if="!showPasswordReset">
          <a href="#" @click.prevent="showPasswordReset = true">Forgot password?</a>
        </div>
      </div>
      <section v-if="showPasswordReset"> 
        <div class="field" >
          <button             
            type="submit"
            class="full-width"
            >Get password reset email
          </button>
        </div>
        <div class="field" >
          <button type="button" @click.prevent="showPasswordReset = false"
            >Cancel
          </button>
        </div>
      </section>
    </form>
    <div v-if="savedUsernames.length && username.length == 0">
      <hr>
      <h3>Saved Usernames</h3>
      <div class="field" v-for="name in savedUsernames" v-bind:key="name">
        <button @click="selectUsername(name)" class="full-width">
          {{name}}
        </button>
      </div>
      <p class="hint">
        <a href="#" @click.prevent="clearSavedUsernames">Clear saved usernames</a>
      </p>
    </div>    
  </div>
</template>

<script>
import Vue from "vue";
var VueCookie = require('vue-cookie');

Vue.use(VueCookie);

export default {
  data: function() {
    return {
      savedUsernames: [],
      username: "",
      password: "",
      passwordError: "",
      message: "",
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
      console.log('submit');
      if(this.showPasswordReset) {
        this.getPasswordResetEmail();
      }
      else if(this.password.length == 0) {
        this.getOneTimeCode();
      }
      else {
        console.log('pre sign in');
        this.signIn();
      }
    },
    getOneTimeCode: function() {
      //todo: call api, and then set the message below
      this.message =
        "We sent sent a one time code to your email or phone.";
    },
    signIn: function() {
      console.log('sign in');
      if(this.signInEnabled) {
        this.message = "Signing in..."
        //before redirect, save cookie
        this.saveUsernames();
      }
    },
    getPasswordResetEmail: function() {
      this.message = "Check your email for password reset instructions";
      this.showPasswordReset = false;
      alert("not implemented");
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
