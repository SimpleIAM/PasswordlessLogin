<template>
  <div class="signin">
    <h2>Sign In</h2>

    <div v-if="step==1">
      <form @submit.prevent="selectSignInName(newSignInName)" >
        <div class="field">
          <label for="sign-in-name">Email, phone, or username</label>
          <input 
            ref="signInName"
            id="sign-in-name" 
            v-model="newSignInName" 
            type="text" 
            placeholder="Email, phone, or username"
            >
          <span v-if="newSignInNameError" class="field-validation-error">{{newSignInNameError}}</span>
        </div>
        <div class="field inline">
          <div class="inline">
            <input 
              type="checkbox" 
              id="remember-sign-in-name" 
              v-model="rememberSignInName"
              :disabled="!newSignInNameIsValid" 
            >
            <label for="remember-sign-in-name">Remember</label>
          </div>
          <button             
            :disabled="!newSignInNameIsValid" 
            class="go-forward"
            >Next
            <svg class="icon icon-circle-right" width="20px" height="20px"><use xlink:href="#icon-circle-right"></use></svg>
          </button>
        </div>
      </form>
      <div v-if="savedSignInNames.length">
        <hr>
        <div class="field" v-for="name in savedSignInNames" v-bind:key="name">
          <button @click="selectSignInName(name)" class="full-width go-forward">
            {{name}}
            <svg class="icon icon-circle-right" width="20px" height="20px"><use xlink:href="#icon-circle-right"></use></svg>
          </button>
        </div>
        <p class="hint">
          <a href="#" @click.prevent="forgetSignInNames">Forget all</a>
        </p>
      </div>
    </div>

    <div v-if="step==2">
      <button @click="chooseDifferentSignInName()" class="full-width go-back">
        <svg class="icon icon-circle-left" width="20px" height="20px"><use xlink:href="#icon-circle-left"></use></svg>
        {{signInName}}
      </button>
      <form @submit.prevent="signIn">
        <div class="tab-container">
          <div
            :tabindex="signInOption=='passphrase' ? -1 : 0" 
            :class="['tab', signInOption=='passphrase' ? 'active' : '']" 
            @click="selectPassphraseTab"
            @keyup.enter="selectPassphraseTab"
            @keyup.space="selectPassphraseTab"
            >Password</div>
          <div
            :tabindex="signInOption=='code' ? -1 : 0" 
            v-if="signInName.includes('@')" 
            :class="['tab', signInOption=='code' ? 'active' : '']" 
            @click="selectOneTimeCodeTab"
            @keyup.enter="selectOneTimeCodeTab"
            @keyup.space="selectOneTimeCodeTab"
            >One Time Code</div>
        </div>
        <div v-show="signInOption=='passphrase'">
          <div class="field">
            <label for="passphrase">Password or passphrase</label>
            <input ref="passphrase" type="password" placeholder="password" id="passphrase" v-model="passphrase">
            <span v-if="passphraseError" class="field-validation-error">{{passphraseError}}</span>
          </div>
        </div>
        <div v-show="signInOption=='code'">
          <p v-show="oneTimeCodeMessage" class="message">
            {{oneTimeCodeMessage}}
            <a href="#" @click.prevent="getOneTimeCode">get new code</a>
          </p>
          <div class="field">
            <label for="one-time-code">One time code</label>
            <input 
              ref="oneTimeCode"
              type="text" 
              size="6" 
              placeholder="000000" 
              id="one-time-code" 
              v-model="oneTimeCode">
            <span v-if="oneTimeCodeError" class="error-message">{{oneTimeCodeError}}</span>
          </div>
        </div>
        <div class="field inline">
          <div class="inline">
            <input type="checkbox" id="stay-signed-in" v-model="staySignedIn">
            <label for="stay-signed-in">Stay signed in</label>
          </div>
          <button 
            class="go-forward"
            :disabled="!signInEnabled"
            >Sign in
            <svg class="icon icon-circle-right" width="20px" height="20px"><use xlink:href="#icon-circle-right"></use></svg>
          </button>
        </div>
      </form>      
    </div>

    <svg style="display:none;" version="1.1" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink">
      <defs>
        <symbol id="icon-circle-right" viewBox="0 0 32 32">
          <title>circle-right</title>
          <path d="M16 0c-8.837 0-16 7.163-16 16s7.163 16 16 16 16-7.163 16-16-7.163-16-16-16zM16 29c-7.18 0-13-5.82-13-13s5.82-13 13-13 13 5.82 13 13-5.82 13-13 13z"></path>
          <path d="M11.086 22.086l2.829 2.829 8.914-8.914-8.914-8.914-2.828 2.828 6.086 6.086z"></path>
        </symbol>
        <symbol id="icon-circle-left" viewBox="0 0 32 32">
          <title>circle-left</title>
          <path d="M16 32c8.837 0 16-7.163 16-16s-7.163-16-16-16-16 7.163-16 16 7.163 16 16 16zM16 3c7.18 0 13 5.82 13 13s-5.82 13-13 13-13-5.82-13-13 5.82-13 13-13z"></path>
          <path d="M20.914 9.914l-2.829-2.829-8.914 8.914 8.914 8.914 2.828-2.828-6.086-6.086z"></path>
        </symbol>
      </defs>
    </svg>
  </div>
</template>

<script>
import Vue from "vue";
var VueCookie = require('vue-cookie');

Vue.use(VueCookie);

export default {
  data: function() {
    return {
      step: 1,
      savedSignInNames: [],
      newSignInName: "",
      rememberSignInName: true,
      signInName: "",
      signInOption: "passphrase",
      passphrase: "",
      passphraseError: "",
      oneTimeCodeSent: false,
      oneTimeCodeMessage: "",
      oneTimeCode: "",
      oneTimeCodeError: "",
      staySignedIn: true
    };
  },
  computed: {
    newSignInNameError: function() {
      if(this.newSignInName.length == 0) {
        return '';
      }
      if(this.newSignInName.includes(' ')) {
        return 'No spaces allowed'
      }
      return '';
    },
    newSignInNameIsValid: function() {
      return this.newSignInName.length > 0 && !this.newSignInNameError;
    },
    signInEnabled: function() {
      if(this.step != 2) {
        return false;
      }
      if(this.signInOption=='passphrase' && this.passphrase.length > 0) {
        return true;
      }
      if(this.signInOption=='code' && this.oneTimeCode.length > 0) {
        return true;
      }
      return false;
    }
  },
  methods: {
    selectSignInName: function(name) {
      //todo: validate
      this.signInName = name;
      this.step = 2;
      this.setFocus();
    },
    chooseDifferentSignInName: function() {
      this.step = 1;
      this.signInOption = 'passphrase';
      this.passphrase = '';
      this.oneTimeCode = '';
      this.setFocus();
    },
    selectOneTimeCodeTab: function() {
      this.signInOption = 'code';
      this.passphrase = '';
      this.setFocus();
      if(!this.oneTimeCodeSent) {
        this.getOneTimeCode();
        this.oneTimeCodeSent = true;
      }
    },
    getOneTimeCode: function() {
      //todo: call api, and then set the message below
      this.oneTimeCodeMessage =
        "A one time code was sent to your email address.";
    },
    selectPassphraseTab: function() {
      this.signInOption = 'passphrase'
      this.setFocus();
    },    
    signIn: function() {
      if(this.signInEnabled) {
        alert("not implemented");
        //before redirect, save cookie
        this.saveCookieSettings();
      }
    },
    setFocus: function() {
      this.$nextTick(() => {
        if (this.step == 1) {
          this.$refs.signInName.focus();
        } else if (this.signInOption == 'passphrase') {
          this.$refs.passphrase.focus();
        } else if (this.signInOption == 'code') {
          this.$refs.oneTimeCode.focus();
        }
      });
    },
    readCookieSettings() {
      const signInNamesString = this.$cookie.get('SavedSignInNames');
      if(typeof signInNamesString == 'string') {
        if(signInNamesString.length > 300) {
          this.$cookie.delete('SavedSignInNames');
        }
        else {
          let count = 0;
          signInNamesString.split(' ').forEach(name => {
            count++;
            if(count <= 3 && name.length > 1 && name.length < 100) {
              this.savedSignInNames.push(name);
            }
          });
        }
      }
    },
    saveCookieSettings() {
      if(this.rememberSignInName) {
        let signInNames = this.savedSignInNames;
        signInNames.unshift(this.signInName);
        this.$cookie.set('SavedSignInNames', signInNames.join(' '), { expires: '1Y' });
        this.newSignInName = '';
      }
    },
    forgetSignInNames() {
      this.$cookie.delete('SavedSignInNames');
      this.savedSignInNames = [];
    }
  },
  mounted: function() {
    this.readCookieSettings();
    this.setFocus();
  }
};
</script>
