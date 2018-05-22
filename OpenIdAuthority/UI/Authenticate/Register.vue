<template>
  <div class="register">
    <form @submit.prevent="submitForm" class="form">
      <div class="field field-stacked form_row">
        <label class="field_label" for="email">Email</label>
        <input class="field_element field_element-fullWidth register_email"
          ref="email"
          id="email" 
          v-model="email" 
          type="text" 
          placeholder="you@example.com"
          >
        <span v-if="emailError" class="field_error">{{emailError}}</span>
      </div>
      <div class="field form_row">
        <button 
          :disabled="!emailIsValid"            
          type="submit"
          class="field_element field_element-fullWidth field_element-tall register_button"
          >Register
        </button>
      </div>
      <div class="register_message" v-if="message">
        {{message}}
      </div>
      <div class="register_footer">
        <a href="/signin" class="register_signInLink">Sign in</a>
      </div>
    </form>
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
      email: '',
      emailError: '',
      message: ''
    };
  },
  computed: {
    emailIsValid: function() {
      if(this.email.length == 0) {
        return false;
      }
      var testEl = document.createElement('input');
      testEl.type = 'email';
      testEl.value = this.email;
      return testEl.checkValidity();
    }
  },
  methods: {
    submitForm: function() {
      api.register('', this.email, this.nexturl ? this.nexturl : '/account/setpassword?nextUrl=/apps')
        .then(data => {
          this.message = 'Thanks for registering. Please check your email.';
        })
        .catch(error => {
          if(error.message) {
            this.message = error.message;
          }
          else {
            this.message = 'Something went wrong';
          }
        });
    }
  },
  mounted: function() {
    this.$nextTick(() => {
      this.$refs.email.focus();
    });
  }
};
</script>
